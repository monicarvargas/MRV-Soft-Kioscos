-- =====================================================================
-- 39. EL NOMBRE DE USUARIO ES ÚNICO POR NEGOCIO (no en todo el sistema)
-- Antes: si el negocio A tenía "MARIA", el negocio B no podía tener otra "MARIA".
-- Ahora: cada negocio puede tener sus propios nombres. El login ya pide el código del negocio,
-- así que no hay confusión. Dentro de un mismo negocio sigue sin poder repetirse.
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

do $$
declare repetidos text;
begin
    -- control: si dentro de un mismo negocio ya hay dos usuarios con el mismo nombre, no se puede seguir
    select string_agg(format('negocio %s: %s', id_kiosco, nombre), ', ') into repetidos
    from (select id_kiosco, lower(trim(username)) nombre from public.usuarios
          group by 1, 2 having count(*) > 1) x;
    if repetidos is not null then
        raise exception 'Hay usuarios repetidos dentro del mismo negocio (%). Cambiale el nombre a uno y volvé a correr el script.', repetidos;
    end if;
end $$;

-- se saca la regla vieja (único en todo el sistema) y se pone la nueva (único dentro de cada negocio)
drop index if exists public.ux_usuarios_username;
create unique index if not exists ux_usuarios_negocio_username
    on public.usuarios (id_kiosco, lower(trim(username)));

-- ¿ya existe ese nombre en ESE negocio? (cada negocio solo puede preguntar por el suyo; el superadmin, por cualquiera)
create or replace function public.usuario_existe_en_negocio(p_kiosco bigint, p_username text, p_excluir uuid default null)
returns boolean
language sql stable security definer set search_path = public as $$
    select exists (select 1 from public.usuarios
                   where id_kiosco = case when public.soy_superadmin() then p_kiosco else public.mi_kiosco() end
                     and lower(trim(username)) = lower(trim(coalesce(p_username, '')))
                     and (p_excluir is null or id <> p_excluir))
$$;

revoke all on function public.usuario_existe_en_negocio(bigint, text, uuid) from public, anon;
grant execute on function public.usuario_existe_en_negocio(bigint, text, uuid) to authenticated, service_role;

notify pgrst, 'reload schema';
select 'Listo: el nombre de usuario ahora es único dentro de cada negocio' as resultado;
