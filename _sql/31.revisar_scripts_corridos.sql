-- =====================================================================
-- ¿QUÉ SCRIPTS ESTÁN CORRIDOS EN ESTA BASE? (no cambia nada, solo mira)
-- Correrlo en TEST y en PRODUCCIÓN y comparar: lo que dice "FALTA" hay que correrlo.
-- =====================================================================
select script, case when ok then 'ok' else 'FALTA' end as estado
from (values
  ('14 seguridad contraseñas',      exists (select 1 from pg_proc where proname = 'es_hash_bcrypt')),
  ('15 seguridad por negocio',      to_regclass('public.sesiones') is not null and to_regprocedure('public.mi_kiosco()') is not null),
  ('15 reglas (RLS) en productos',  coalesce((select relrowsecurity from pg_class where oid = to_regclass('public.productos')), false)),
  ('15 login nuevo (iniciar_sesion)', exists (select 1 from pg_proc where proname = 'iniciar_sesion')),
  ('17 deudas con proveedores',     to_regclass('public.proveedores_cuenta') is not null),
  ('18 proveedores activo',         exists (select 1 from information_schema.columns where table_name = 'proveedores' and column_name = 'activo')),
  ('19 reportes de productos',      exists (select 1 from information_schema.columns where table_name = 'productos' and column_name = 'fecha_precio')),
  ('20 planes',                     to_regclass('public.planes') is not null),
  ('22 fiado FIFO',                 to_regclass('public.fiado_aplicaciones') is not null),
  ('23 tareas',                     to_regclass('public.tareas') is not null),
  ('24 unidades del negocio',       exists (select 1 from information_schema.columns where table_name = 'kioscos' and column_name = 'unidades')),
  ('25 ventas sin internet',        to_regclass('public.equipos_offline') is not null),
  ('26 redondeo y subfamilias',     to_regclass('public.subfamilias') is not null),
  ('27 precios por cantidad',       to_regclass('public.precios_cantidad') is not null),
  ('28 reportes por subfamilia',    to_regprocedure('public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer, bigint)') is not null),
  ('29 exportar negocio',           exists (select 1 from pg_proc where proname = 'exportar_negocio')),
  ('32 último bulto del producto',  exists (select 1 from information_schema.columns where table_name = 'productos' and column_name = 'bulto_precio')),
  ('33 recargos y horario de listas', to_regclass('public.listas_recargos') is not null),
  ('34 viene por caja / bulto',    exists (select 1 from information_schema.columns where table_name = 'productos_proveedores' and column_name = 'tipo_bulto')),
  ('35 historial cuenta proveedores', exists (select 1 from information_schema.columns where table_name = 'proveedores_cuenta' and column_name = 'anulado')),
  ('36 domingos y feriados',       to_regclass('public.listas_dias') is not null),
  ('37 utilidad por familia',      exists (select 1 from information_schema.columns where table_schema='public' and table_name='familias' and column_name='utilidad')),
  ('38 permisos de los perfiles',  exists (select 1 from information_schema.columns where table_schema='public' and table_name='kioscos' and column_name='permisos_roles'))
) as t(script, ok)
order by 1;
