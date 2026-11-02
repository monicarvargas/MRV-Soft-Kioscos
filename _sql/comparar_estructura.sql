-- =====================================================================
-- FOTO DE LA ESTRUCTURA (no cambia nada, solo lee)
-- Correrlo en TEST y en PRODUCCIÓN, y pasarle al chat los dos resultados
-- (botón "Copy" / "Download CSV" en Results). Así vemos qué le falta a producción.
-- =====================================================================
select 'tabla/columna' as tipo, table_name as objeto,
       column_name || ' ' || data_type || coalesce(' default ' || column_default, '') ||
       case when is_generated = 'ALWAYS' then ' (calculada)' else '' end as detalle
from information_schema.columns
where table_schema = 'public'
union all
select 'vista', table_name, '' from information_schema.views where table_schema = 'public'
union all
select 'funcion', p.proname, pg_get_function_identity_arguments(p.oid)
from pg_proc p join pg_namespace n on n.oid = p.pronamespace where n.nspname = 'public'
union all
select 'trigger', event_object_table, trigger_name from information_schema.triggers where trigger_schema = 'public'
union all
select 'indice', tablename, indexname from pg_indexes where schemaname = 'public'
union all
select 'extension', extname, extversion from pg_extension
order by 1, 2, 3;
