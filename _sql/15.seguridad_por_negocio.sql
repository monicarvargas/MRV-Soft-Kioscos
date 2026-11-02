-- =====================================================================
-- SEGURIDAD 2: CADA NEGOCIO SOLO VE LO SUYO (reglas de acceso / RLS)
--
-- ANTES DE CORRER:
--   1) Haber corrido 14.seguridad_contrasenas.sql.
--   2) En Supabase: Authentication > Sign In / Providers > activar "Allow anonymous sign-ins".
--   3) Tener lista la versión NUEVA de la app (la vieja deja de funcionar al correr esto).
-- Correr en TEST, probar todo, y recién después en PRODUCCIÓN. Se puede correr más de una vez.
-- Para volver atrás: 16.seguridad_deshacer.sql
--
-- Cómo funciona:
--   * Cada equipo obtiene una credencial de Supabase (sesión anónima, sin mail).
--   * Al entrar con usuario y contraseña, iniciar_sesion() ata esa credencial al negocio y al usuario (tabla sesiones).
--   * Cada tabla tiene una regla: solo filas de MI negocio (el superadmin ve todas).
-- =====================================================================

-- ---------- 1) sesiones: qué credencial es de qué negocio/usuario ----------
create table if not exists public.sesiones (
    auth_uid      uuid primary key,          -- auth.uid() de la credencial del equipo
    usuario_id    uuid not null references public.usuarios(id) on delete cascade,
    kiosco_propio bigint not null,           -- negocio del usuario
    kiosco_id     bigint not null,           -- negocio que está usando (el superadmin lo cambia con "Ver como")
    rol           text,
    creada        timestamptz not null default now(),
    actualizada   timestamptz not null default now()
);
alter table public.sesiones enable row level security;   -- sin reglas: nadie la lee directo
revoke all on public.sesiones from anon, authenticated;

create or replace function public.mi_kiosco() returns bigint
language sql stable security definer set search_path = public as $$
    select kiosco_id from public.sesiones where auth_uid = auth.uid()
$$;

create or replace function public.mi_rol() returns text
language sql stable security definer set search_path = public as $$
    select lower(coalesce(rol, '')) from public.sesiones where auth_uid = auth.uid()
$$;

create or replace function public.soy_superadmin() returns boolean
language sql stable security definer set search_path = public as $$
    select coalesce((select lower(rol) = 'superadmin' from public.sesiones where auth_uid = auth.uid()), false)
$$;

-- ---------- 2) entrar / salir / "ver como" ----------
create or replace function public.iniciar_sesion(p_codigo text, p_usuario text, p_password text)
returns jsonb
language plpgsql volatile security definer set search_path = public, extensions as $$
declare
    k public.kioscos;
    u public.usuarios;
begin
    if auth.uid() is null then
        return jsonb_build_object('resultado', 'sin_credencial');
    end if;

    select * into k from public.kioscos where upper(codigo_vinculacion) = upper(trim(coalesce(p_codigo, '')));
    if not found then return jsonb_build_object('resultado', 'negocio'); end if;
    if k.estado = false then return jsonb_build_object('resultado', 'baja'); end if;

    select * into u from public.usuarios
    where id_kiosco = k.id
      and lower(trim(username)) = lower(trim(coalesce(p_usuario, '')))
      and coalesce(p_password, '') <> ''
      and password = extensions.crypt(p_password, password);
    if not found then return jsonb_build_object('resultado', 'clave'); end if;

    insert into public.sesiones (auth_uid, usuario_id, kiosco_propio, kiosco_id, rol)
    values (auth.uid(), u.id, k.id, k.id, u.rol)
    on conflict (auth_uid) do update
        set usuario_id = excluded.usuario_id, kiosco_propio = excluded.kiosco_propio,
            kiosco_id = excluded.kiosco_id, rol = excluded.rol, actualizada = now();

    return jsonb_build_object(
        'resultado', 'ok',
        'id', u.id, 'username', u.username, 'rol', u.rol, 'id_kiosco', k.id,
        'kiosco', to_jsonb(k) - 'notas_administrativas');
end $$;

create or replace function public.cerrar_sesion() returns void
language sql volatile security definer set search_path = public as $$
    delete from public.sesiones where auth_uid = auth.uid();
$$;

-- solo el superadmin: pasa a usar otro negocio ("Ver como"); null = vuelve al suyo
create or replace function public.cambiar_kiosco_sesion(p_kiosco bigint) returns void
language plpgsql volatile security definer set search_path = public as $$
begin
    if not public.soy_superadmin() then raise exception 'Solo el superadmin puede ver otros negocios'; end if;
    update public.sesiones set kiosco_id = coalesce(p_kiosco, kiosco_propio), actualizada = now()
    where auth_uid = auth.uid();
end $$;

-- el nombre de usuario es único en todo el sistema: se controla sin dejar ver usuarios de otros negocios
create or replace function public.usuario_existe(p_username text, p_excluir uuid default null) returns boolean
language sql stable security definer set search_path = public as $$
    select exists (select 1 from public.usuarios
                   where lower(trim(username)) = lower(trim(coalesce(p_username, '')))
                     and (p_excluir is null or id <> p_excluir))
$$;

revoke all on function public.iniciar_sesion(text, text, text), public.cerrar_sesion(),
                       public.cambiar_kiosco_sesion(bigint), public.usuario_existe(text, uuid),
                       public.mi_kiosco(), public.mi_rol(), public.soy_superadmin() from public, anon;
grant execute on function public.iniciar_sesion(text, text, text), public.cerrar_sesion(),
                          public.cambiar_kiosco_sesion(bigint), public.usuario_existe(text, uuid),
                          public.mi_kiosco(), public.mi_rol(), public.soy_superadmin() to authenticated, service_role;

-- el login de la parte 1 ya no se usa (dejaba probar contraseñas sin credencial)
drop function if exists public.login_usuario(bigint, text, text);

-- ---------- 3) tablas "detalle" sin negocio: se les agrega (se completa solo al guardar) ----------
alter table public.venta_detalles            add column if not exists kiosco_id bigint;
alter table public.compra_mayorista_detalles add column if not exists kiosco_id bigint;
alter table public.lista_pedido_detalles     add column if not exists kiosco_id bigint;

update public.venta_detalles d set kiosco_id = p.kiosco_id
from public.productos p where d.kiosco_id is null and p.id = d.producto_id;
update public.venta_detalles d set kiosco_id = v.kiosco_id
from public.ventas v where d.kiosco_id is null
  and v.numero_unico_identificatorio_movimiento = d.numero_unico_identificatorio_movimiento;
update public.compra_mayorista_detalles d set kiosco_id = c.kiosco_id
from public.compras_mayoristas c where d.kiosco_id is null and c.id = d.compra_id;
update public.lista_pedido_detalles d set kiosco_id = l.kiosco_id
from public.lista_pedidos l where d.kiosco_id is null and l.id = d.pedido_id;

alter table public.venta_detalles            alter column kiosco_id set default public.mi_kiosco();
alter table public.compra_mayorista_detalles alter column kiosco_id set default public.mi_kiosco();
alter table public.lista_pedido_detalles     alter column kiosco_id set default public.mi_kiosco();
create index if not exists ix_venta_detalles_kiosco on public.venta_detalles (kiosco_id);
create index if not exists ix_compra_det_kiosco     on public.compra_mayorista_detalles (kiosco_id);
create index if not exists ix_pedido_det_kiosco     on public.lista_pedido_detalles (kiosco_id);

-- ---------- 4) regla en cada tabla con negocio: solo MI negocio (o superadmin) ----------
do $$
declare
    r record;
begin
    for r in
        select c.table_name, c.column_name
        from information_schema.columns c
        join information_schema.tables t on t.table_schema = c.table_schema and t.table_name = c.table_name and t.table_type = 'BASE TABLE'
        where c.table_schema = 'public'
          and c.column_name in ('kiosco_id', 'id_kiosco')
          and c.table_name not in ('usuarios', 'kioscos', 'sesiones')
          and c.table_name not like '%\_sacar'
    loop
        execute format('alter table public.%I enable row level security', r.table_name);
        execute format('drop policy if exists aislado_por_negocio on public.%I', r.table_name);
        execute format(
            'create policy aislado_por_negocio on public.%I for all to authenticated
               using (%2$I = (select public.mi_kiosco()) or (select public.soy_superadmin()))
               with check (%2$I = (select public.mi_kiosco()) or (select public.soy_superadmin()))',
            r.table_name, r.column_name);
    end loop;
end $$;

-- ---------- 5) usuarios: ver los de mi negocio; tocar solo admin (y nadie se hace superadmin) ----------
alter table public.usuarios enable row level security;
drop policy if exists usuarios_ver on public.usuarios;
drop policy if exists usuarios_alta on public.usuarios;
drop policy if exists usuarios_cambio on public.usuarios;
drop policy if exists usuarios_baja on public.usuarios;
create policy usuarios_ver on public.usuarios for select to authenticated
    using (id_kiosco = (select public.mi_kiosco()) or (select public.soy_superadmin()));
create policy usuarios_alta on public.usuarios for insert to authenticated
    with check ((select public.soy_superadmin()) or (id_kiosco = (select public.mi_kiosco()) and (select public.mi_rol()) = 'admin'));
create policy usuarios_cambio on public.usuarios for update to authenticated
    using ((select public.soy_superadmin()) or (id_kiosco = (select public.mi_kiosco()) and (select public.mi_rol()) = 'admin'))
    with check ((select public.soy_superadmin()) or (id_kiosco = (select public.mi_kiosco()) and (select public.mi_rol()) = 'admin'));
create policy usuarios_baja on public.usuarios for delete to authenticated
    using ((select public.soy_superadmin()) or (id_kiosco = (select public.mi_kiosco()) and (select public.mi_rol()) = 'admin'));

create or replace function public.tg_usuarios_sin_superadmin() returns trigger
language plpgsql security definer set search_path = public as $$
begin
    -- desde la app (con credencial) solo el superadmin puede dar rol superadmin; desde el SQL Editor sí se puede
    if lower(coalesce(new.rol, '')) = 'superadmin' and auth.uid() is not null and not public.soy_superadmin() then
        raise exception 'No se puede asignar el rol superadmin';
    end if;
    return new;
end $$;
drop trigger if exists trg_usuarios_sin_superadmin on public.usuarios;
create trigger trg_usuarios_sin_superadmin before insert or update of rol on public.usuarios
for each row execute function public.tg_usuarios_sin_superadmin();

-- ---------- 6) kioscos: ver/cambiar el mío; alta y baja solo superadmin ----------
alter table public.kioscos enable row level security;
drop policy if exists kioscos_ver on public.kioscos;
drop policy if exists kioscos_cambio on public.kioscos;
drop policy if exists kioscos_alta on public.kioscos;
drop policy if exists kioscos_baja on public.kioscos;
create policy kioscos_ver on public.kioscos for select to authenticated
    using (id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
create policy kioscos_cambio on public.kioscos for update to authenticated
    using ((select public.soy_superadmin()) or (id = (select public.mi_kiosco()) and (select public.mi_rol()) in ('admin', 'encargado')))
    with check ((select public.soy_superadmin()) or (id = (select public.mi_kiosco()) and (select public.mi_rol()) in ('admin', 'encargado')));
create policy kioscos_alta on public.kioscos for insert to authenticated with check ((select public.soy_superadmin()));
create policy kioscos_baja on public.kioscos for delete to authenticated using ((select public.soy_superadmin()));

-- ---------- 7) las vistas respetan las reglas de quien consulta ----------
do $$
declare r record;
begin
    for r in select table_name from information_schema.views where table_schema = 'public' loop
        execute format('alter view public.%I set (security_invoker = true)', r.table_name);
    end loop;
end $$;

-- ---------- 8) sin credencial (anon) no se toca ninguna tabla ----------
revoke all on all tables in schema public from anon;
grant select, insert, update, delete on all tables in schema public to authenticated;
revoke select on public.usuarios from authenticated;
grant select (id, username, id_kiosco, rol) on public.usuarios to authenticated;
revoke all on public.sesiones from authenticated;
grant usage, select on all sequences in schema public to authenticated;
alter default privileges in schema public revoke all on tables from anon;

notify pgrst, 'reload schema';

-- ---------- control ----------
select c.relname as tabla,
       c.relrowsecurity as con_reglas,
       (select count(*) from pg_policies p where p.schemaname = 'public' and p.tablename = c.relname) as reglas
from pg_class c join pg_namespace n on n.oid = c.relnamespace
where n.nspname = 'public' and c.relkind = 'r' and c.relname not like '%\_sacar'
order by c.relrowsecurity, c.relname;
