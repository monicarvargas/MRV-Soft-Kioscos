-- =====================================================================
-- PRODUCCIÓN · PASO 3 (después de correr el script de estructura de TEST)
--  a) Permisos para que la app pueda leer/escribir las tablas.
--  b) Crea el negocio del superadmin y el usuario superadmin.
--
-- ANTES DE CORRER: completá abajo usuario, contraseña y nombre.
-- Se puede correr más de una vez (no duplica).
-- =====================================================================

-- a) permisos
grant usage on schema public to postgres, anon, authenticated, service_role;
grant all on all tables    in schema public to postgres, anon, authenticated, service_role;
grant all on all functions in schema public to postgres, anon, authenticated, service_role;
grant all on all sequences in schema public to postgres, anon, authenticated, service_role;

-- b) negocio + usuario superadmin
do $$
declare
    -- ======== COMPLETAR ========
    v_usuario     text := 'superadmin';          -- con este usuario vas a entrar
    v_password    text := 'CAMBIAR_ESTA_CLAVE';  -- poné una clave fuerte
    v_negocio     text := 'MRV SOFT';            -- nombre del negocio "administrador"
    v_codigo      text := 'MRV-ADMIN';           -- código de negocio que se escribe en el login
    -- ===========================
    v_kiosco bigint;
begin
    if v_password = 'CAMBIAR_ESTA_CLAVE' then
        raise exception 'Primero cambiá la contraseña en v_password.';
    end if;

    select id into v_kiosco from public.kioscos where upper(codigo_vinculacion) = upper(v_codigo) limit 1;
    if v_kiosco is null then
        insert into public.kioscos (nombre_negocio, codigo_vinculacion, estado, fecha_alta, fecha_vencimiento_abono, dias_anticipacion_aviso)
        values (upper(v_negocio), upper(v_codigo), true, now(), (now() + interval '10 years')::date, 5)
        returning id into v_kiosco;
    end if;

    if exists (select 1 from public.usuarios where lower(username) = lower(v_usuario)) then
        update public.usuarios set password = v_password, rol = 'superadmin', id_kiosco = v_kiosco
        where lower(username) = lower(v_usuario);
    else
        insert into public.usuarios (username, password, rol, id_kiosco)
        values (v_usuario, v_password, 'superadmin', v_kiosco);
    end if;

    raise notice 'Listo: entrá con código de negocio % y usuario %', upper(v_codigo), v_usuario;
end $$;

-- que la API de Supabase vea las tablas nuevas enseguida
notify pgrst, 'reload schema';

select u.username, u.rol, k.codigo_vinculacion, k.nombre_negocio
from public.usuarios u join public.kioscos k on k.id = u.id_kiosco
where lower(u.rol) = 'superadmin';
