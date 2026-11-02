-- =====================================================================
-- PRODUCCIÓN · PASO 1: dejar el esquema "public" VACÍO (sin tablas)
--
-- ⚠️  SOLO EN EL PROYECTO DE **PRODUCCIÓN** y SOLO mientras no tenga negocios reales.
--     Borra TODAS las tablas, vistas y funciones de "public" (con sus datos).
--     No toca usuarios de Supabase, Storage ni configuración del proyecto.
--
-- CÓMO USARLO
--   1) Corrélo como está: NO BORRA NADA. Muestra qué tablas hay y cuántas filas tienen.
--   2) Si está bien, poné las DOS opciones en true y corrélo de nuevo.
--   3) Después corré, en este orden:
--        a) el script de estructura generado en TEST (9.generar_script_estructura → estructura_test_xxxx.sql)
--        b) 13.produccion_permisos_y_superadmin.sql
-- =====================================================================

drop table if exists _prod;
create temp table _prod (tabla text, filas bigint, accion text);

do $$
declare
    -- ======== OPCIONES ========
    v_es_produccion  boolean := false;   -- confirmá que estás en el proyecto de PRODUCCIÓN
    v_ejecutar       boolean := false;   -- false = solo mostrar; true = BORRAR TODO "public"
    -- ==========================
    r record;
    n bigint;
begin
    for r in
        select c.relname from pg_class c join pg_namespace s on s.oid = c.relnamespace
        where s.nspname = 'public' and c.relkind = 'r' and c.relname not like '\_%'
        order by c.relname
    loop
        execute format('select count(*) from public.%I', r.relname) into n;
        insert into _prod values (r.relname, n, case when v_ejecutar and v_es_produccion then 'BORRADA' else 'se borraría' end);
    end loop;

    if not (v_ejecutar and v_es_produccion) then return; end if;

    -- las extensiones que vivan en "public" se mueven a "extensions" para no perderlas
    for r in select e.extname from pg_extension e join pg_namespace s on s.oid = e.extnamespace
             where s.nspname = 'public' and e.extname not in ('plpgsql')
    loop
        begin
            execute format('alter extension %I set schema extensions', r.extname);
        exception when others then
            execute format('drop extension if exists %I cascade', r.extname);
        end;
    end loop;

    drop schema public cascade;
    create schema public;

    -- permisos que Supabase necesita sobre el esquema (los de cada tabla los pone el script 13)
    grant usage on schema public to postgres, anon, authenticated, service_role;
    grant all on schema public to postgres, service_role;
    alter default privileges in schema public grant all on tables    to postgres, anon, authenticated, service_role;
    alter default privileges in schema public grant all on functions to postgres, anon, authenticated, service_role;
    alter default privileges in schema public grant all on sequences to postgres, anon, authenticated, service_role;

    insert into _prod values ('LISTO', null, 'Esquema vacío. Ahora corré el script de estructura de TEST y después el 13.');
end $$;

select tabla, filas, accion from _prod order by (tabla = 'LISTO'), tabla;
