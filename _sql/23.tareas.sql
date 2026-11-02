-- =====================================================================
-- TAREAS DEL PERSONAL: cada empleado carga, renglón por renglón, lo que hizo en el día.
-- (Etapa 1: carga y consulta. Más adelante se arma la tabla/resumen con esto.)
-- Correr en TEST y en PRODUCCIÓN (después del 15 y del 20). Se puede correr más de una vez.
-- =====================================================================

create table if not exists public.tareas (
    id             bigserial primary key,
    kiosco_id      bigint not null references public.kioscos(id) on delete cascade,
    usuario_id     uuid,                    -- quién la hizo
    usuario_nombre text,                    -- se guarda el nombre por si después se borra el usuario
    fecha          date not null default ((now() at time zone 'America/Argentina/Buenos_Aires')::date),
    hora           text,                    -- HH:mm de cuando la cargó (hora del equipo)
    descripcion    text not null check (length(trim(descripcion)) > 0),
    minutos        integer check (minutos is null or minutos >= 0),   -- tiempo que le llevó (opcional)
    creado         timestamptz not null default now()
);
create index if not exists ix_tareas_kiosco_fecha on public.tareas (kiosco_id, fecha);
create index if not exists ix_tareas_usuario on public.tareas (usuario_id, fecha);

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        alter table public.tareas alter column kiosco_id set default public.mi_kiosco();
        alter table public.tareas enable row level security;
        drop policy if exists aislado_por_negocio on public.tareas;
        create policy aislado_por_negocio on public.tareas for all to authenticated
            using (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()))
            with check (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
        revoke all on public.tareas from anon;
        grant select, insert, update, delete on public.tareas to authenticated;
        grant usage, select on sequence public.tareas_id_seq to authenticated;
    else
        grant select, insert, update, delete on public.tareas to anon, authenticated;
        grant usage, select on sequence public.tareas_id_seq to anon, authenticated;
    end if;

    -- la función "tareas" entra en el plan Avanzado (si existen los planes del SQL 20)
    if to_regclass('public.planes') is not null then
        update public.planes set funciones = array_append(funciones, 'tareas')
         where codigo = 'avanzado' and not ('tareas' = any(funciones));
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Tabla de tareas lista' as resultado;
