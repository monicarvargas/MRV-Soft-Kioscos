-- =====================================================================
-- DESHACER "15.seguridad_por_negocio.sql" (por si algo no anda y hay que volver ya)
-- Saca las reglas por negocio y vuelve a dejar entrar a la app SIN credencial.
-- Las contraseñas siguen encriptadas (eso es del 14). Ojo: la app NUEVA usa iniciar_sesion,
-- que se mantiene; la app VIEJA (anterior al 15) necesita la función login_usuario del 14:
-- si volvés a la app vieja, corré de nuevo 14.seguridad_contrasenas.sql después de este.
-- =====================================================================
do $$
declare r record;
begin
    for r in select schemaname, tablename, policyname from pg_policies where schemaname = 'public' loop
        execute format('drop policy if exists %I on public.%I', r.policyname, r.tablename);
    end loop;
    for r in select c.relname from pg_class c join pg_namespace n on n.oid = c.relnamespace
             where n.nspname = 'public' and c.relkind = 'r' and c.relname <> 'sesiones' loop
        execute format('alter table public.%I disable row level security', r.relname);
    end loop;
end $$;

grant select, insert, update, delete on all tables in schema public to anon, authenticated;
revoke select on public.usuarios from anon, authenticated;
grant select (id, username, id_kiosco, rol) on public.usuarios to anon, authenticated;
revoke all on public.sesiones from anon, authenticated;
grant usage, select on all sequences in schema public to anon, authenticated;
notify pgrst, 'reload schema';
select 'Reglas por negocio desactivadas' as resultado;
