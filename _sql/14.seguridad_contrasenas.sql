-- =====================================================================
-- SEGURIDAD 1: CONTRASEÑAS ENCRIPTADAS + LOGIN VERIFICADO EN LA BASE
-- Correr en TEST primero, probar la app, y después en PRODUCCIÓN.
-- Se puede correr más de una vez.
--
-- Qué hace:
--  1) Encripta (bcrypt) las contraseñas que estén guardadas tal cual.
--     Los usuarios siguen entrando con LA MISMA contraseña.
--  2) Toda contraseña nueva o cambiada se encripta sola (trigger).
--  3) Función login_usuario: la base verifica usuario + contraseña.
--  4) Desde la app (clave pública) YA NO se puede leer la columna password.
--
-- ⚠️ Después de correrlo, la versión VIEJA de la app no puede entrar:
--    actualizá la app (compilar) en el mismo momento.
-- =====================================================================

-- 0) pgcrypto (en Supabase vive en el esquema "extensions")
create schema if not exists extensions;
create extension if not exists pgcrypto schema extensions;

-- la columna tiene que aguantar el hash (60 caracteres)
do $$ begin
    if (select data_type from information_schema.columns
        where table_schema = 'public' and table_name = 'usuarios' and column_name = 'password') <> 'text' then
        drop trigger if exists trg_usuarios_hash_password on public.usuarios;
        alter table public.usuarios alter column password type text;
    end if;
end $$;

-- 1) ¿ya está encriptada?  ($2a$ / $2b$ / $2y$ = bcrypt)
create or replace function public.es_hash_bcrypt(p text) returns boolean
language sql immutable as $$ select coalesce(p, '') ~ '^\$2[aby]\$\d{2}\$' $$;

-- 2) encriptar las que están tal cual
update public.usuarios
set password = extensions.crypt(password, extensions.gen_salt('bf', 10))
where password is not null and password <> '' and not public.es_hash_bcrypt(password);

-- 3) trigger: lo que se guarde en password se encripta solo
create or replace function public.tg_usuarios_hash_password() returns trigger
language plpgsql security definer set search_path = public, extensions as $$
begin
    if new.password is not null and new.password <> '' and not public.es_hash_bcrypt(new.password) then
        new.password := extensions.crypt(new.password, extensions.gen_salt('bf', 10));
    end if;
    -- en un UPDATE que no manda contraseña nueva, se conserva la anterior
    if tg_op = 'UPDATE' and (new.password is null or new.password = '') then
        new.password := old.password;
    end if;
    return new;
end $$;

drop trigger if exists trg_usuarios_hash_password on public.usuarios;
create trigger trg_usuarios_hash_password
before insert or update of password on public.usuarios
for each row execute function public.tg_usuarios_hash_password();

-- 4) login: devuelve el usuario solo si la contraseña es correcta (nunca devuelve la contraseña)
drop function if exists public.login_usuario(bigint, text, text);
create or replace function public.login_usuario(p_kiosco bigint, p_usuario text, p_password text)
returns table (id uuid, username text, rol text, id_kiosco bigint)
language sql stable security definer set search_path = public, extensions as $$
    select u.id, u.username::text, u.rol::text, u.id_kiosco
    from public.usuarios u
    where u.id_kiosco = p_kiosco
      and lower(trim(u.username)) = lower(trim(p_usuario))
      and coalesce(p_password, '') <> ''
      and u.password = extensions.crypt(p_password, u.password)
    limit 1;
$$;

revoke all on function public.login_usuario(bigint, text, text) from public;
grant execute on function public.login_usuario(bigint, text, text) to anon, authenticated, service_role;

-- 5) la app ya no puede LEER la contraseña (sí puede guardar una nueva: el trigger la encripta)
revoke select on public.usuarios from anon, authenticated;
grant select (id, username, id_kiosco, rol) on public.usuarios to anon, authenticated;
grant insert, update, delete on public.usuarios to anon, authenticated;

notify pgrst, 'reload schema';

-- control: cuántas quedaron encriptadas
select count(*) filter (where public.es_hash_bcrypt(password)) as encriptadas,
       count(*) filter (where not public.es_hash_bcrypt(password)) as sin_encriptar
from public.usuarios;
