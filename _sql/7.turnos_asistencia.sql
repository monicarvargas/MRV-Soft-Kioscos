-- =====================================================================
-- TURNOS + ASISTENCIA (entrada / salida de empleados)
-- Correr TODO junto en Supabase > SQL Editor. Se puede correr más de una vez.
-- Zona horaria usada: America/Argentina/Buenos_Aires
-- =====================================================================

-- 1) Turnos fijos de cada negocio (ej: Mañana 07:00-15:00, Noche 23:00-07:00)
create table if not exists turnos (
    id          bigserial primary key,
    kiosco_id   bigint not null,
    nombre      text   not null,
    hora_inicio time   not null,
    hora_fin    time   not null,   -- si es menor que hora_inicio, el turno termina al día siguiente
    orden       int    not null default 0,
    activo      boolean not null default true
);
create index if not exists ix_turnos_kiosco on turnos (kiosco_id);

-- 2) Entradas y salidas (una fila por cada vez que un empleado entra al sistema)
create table if not exists asistencias (
    id          bigserial primary key,
    kiosco_id   bigint not null,
    usuario_id  uuid   not null,
    dispositivo text,                              -- qué celular / PC
    entrada     timestamptz not null default now(),
    salida      timestamptz,
    cierre      text,                              -- salio | otro_usuario | fin_turno | manual
    nota        text
);
create index if not exists ix_asistencias_kiosco_entrada on asistencias (kiosco_id, entrada);
create index if not exists ix_asistencias_abiertas on asistencias (kiosco_id, usuario_id) where salida is null;

-- 3) Cierra solas las entradas que quedaron abiertas (se olvidaron de salir):
--    la salida es el fin del turno (el primer fin de turno que esté al menos 1 hora después de la entrada).
--    Si el negocio no tiene turnos cargados, se cierra a las 12 horas.
create or replace function public.cerrar_asistencias_vencidas(p_kiosco bigint) returns void
language plpgsql as $$
declare
    tz text := 'America/Argentina/Buenos_Aires';
    r record;
    fin_ts timestamptz;
begin
    for r in select id, entrada from asistencias where kiosco_id = p_kiosco and salida is null loop
        select min(e) into fin_ts
        from (
            select (((r.entrada at time zone tz)::date + d) + t.hora_fin) at time zone tz as e
            from turnos t, generate_series(0, 1) d
            where t.kiosco_id = p_kiosco and t.activo
        ) x
        where e >= r.entrada + interval '1 hour';

        if fin_ts is null then fin_ts := r.entrada + interval '12 hours'; end if;

        if fin_ts <= now() then
            update asistencias set salida = fin_ts, cierre = 'fin_turno' where id = r.id;
        end if;
    end loop;
end $$;

-- 4) Al entrar al sistema: marca la entrada (y la salida del empleado anterior en ese mismo equipo)
create or replace function public.fichar_entrada(p_kiosco bigint, p_usuario uuid, p_dispositivo text) returns bigint
language plpgsql as $$
declare v_id bigint;
begin
    perform public.cerrar_asistencias_vencidas(p_kiosco);

    -- otro empleado estaba en este equipo: se le marca la salida ahora
    update asistencias set salida = now(), cierre = 'otro_usuario'
    where kiosco_id = p_kiosco and dispositivo = p_dispositivo and salida is null and usuario_id <> p_usuario;

    -- si ya estaba trabajando (volvió a abrir la app), sigue la misma entrada
    select id into v_id from asistencias
    where kiosco_id = p_kiosco and usuario_id = p_usuario and salida is null
    order by entrada desc limit 1;
    if v_id is not null then
        update asistencias set dispositivo = p_dispositivo where id = v_id;
        return v_id;
    end if;

    insert into asistencias (kiosco_id, usuario_id, dispositivo) values (p_kiosco, p_usuario, p_dispositivo)
    returning id into v_id;
    return v_id;
end $$;

-- 5) "Cambiar usuario": marca la salida
create or replace function public.fichar_salida(p_id bigint) returns void
language sql as $$
    update asistencias set salida = now(), cierre = 'salio' where id = p_id and salida is null;
$$;

-- 6) El dueño corrige una entrada/salida (horas locales "2026-10-12 07:05")
create or replace function public.editar_asistencia(p_id bigint, p_entrada text, p_salida text) returns void
language plpgsql as $$
declare tz text := 'America/Argentina/Buenos_Aires';
begin
    update asistencias
    set entrada = (p_entrada::timestamp at time zone tz),
        salida  = case when coalesce(p_salida, '') = '' then null else (p_salida::timestamp at time zone tz) end,
        cierre  = 'manual'
    where id = p_id;
end $$;

-- 7) Vista para la pantalla (horas ya en hora argentina, como texto)
drop view if exists vw_asistencias;
create view vw_asistencias as
select a.id,
       a.kiosco_id,
       a.usuario_id,
       u.username,
       u.rol,
       a.dispositivo,
       a.cierre,
       to_char(a.entrada at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD')       as dia,
       to_char(a.entrada at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD HH24:MI') as entrada_local,
       to_char(a.salida  at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD HH24:MI') as salida_local,
       (a.salida is null)                                                                    as abierta,
       round(extract(epoch from (coalesce(a.salida, now()) - a.entrada)) / 60)::int          as minutos
from asistencias a
left join usuarios u on u.id = a.usuario_id;
