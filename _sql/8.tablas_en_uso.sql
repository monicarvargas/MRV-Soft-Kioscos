-- =====================================================================
-- ¿QUÉ TABLAS USA EL SISTEMA?  (solo lee, no cambia nada)
-- Correrlo en Supabase > SQL Editor y mirar la columna "estado".
-- Las que digan "NO SE USA" se pueden borrar (pasame la lista antes, por las dudas).
-- =====================================================================
with usadas_por_app(nombre) as (
    values
    -- tablas que lee/escribe la aplicación
    ('kioscos'), ('usuarios'), ('personas'), ('productos'), ('productos_proveedores'), ('proveedores'),
    ('familias'), ('formas_pago'), ('listas_precios'), ('horarios_ventas'),
    ('ventas'), ('venta_detalles'), ('productos_varios_ventas'),
    ('categorias_venta_rapida'), ('combo_detalle'),
    ('compras_mayoristas'), ('compra_mayorista_detalles'), ('importaciones_excel'),
    ('lista_pedidos'), ('lista_pedido_detalles'),
    ('turnos'), ('asistencias'),
    -- vistas que usa la aplicación
    ('vw_deudas_pendientes_detallado'), ('vw_pagos_parciales_x_deudas'), ('vw_resumen_deudores'),
    ('vw_resumen_formas_pago'), ('vw_asistencias')
),
objetos as (
    select c.oid, c.relname as nombre,
           case c.relkind when 'r' then 'tabla' when 'v' then 'vista' when 'm' then 'vista materializada' end as tipo
    from pg_class c join pg_namespace n on n.oid = c.relnamespace
    where n.nspname = 'public' and c.relkind in ('r', 'v', 'm')
),
-- vistas que dependen de cada objeto
en_vistas as (
    select d.refobjid as oid, string_agg(distinct v.relname, ', ') as vistas
    from pg_depend d
    join pg_rewrite r on r.oid = d.objid
    join pg_class v on v.oid = r.ev_class
    where d.refobjid <> r.ev_class
    group by d.refobjid
),
-- funciones que nombran al objeto en su código
en_funciones as (
    select o.oid, string_agg(distinct p.proname, ', ') as funciones
    from objetos o
    join pg_proc p on p.prosrc ~* ('\m' || o.nombre || '\M')
    join pg_namespace n on n.oid = p.pronamespace and n.nspname = 'public'
    group by o.oid
),
-- tablas usadas que apuntan (FK) a este objeto
en_fk as (
    select c.confrelid as oid, string_agg(distinct t.relname, ', ') as desde
    from pg_constraint c join pg_class t on t.oid = c.conrelid
    where c.contype = 'f'
    group by c.confrelid
)
select o.tipo,
       o.nombre,
       case when o.tipo = 'tabla' then (xpath('/row/n/text()', query_to_xml(format('select count(*) as n from public.%I', o.nombre), false, true, '')))[1]::text::bigint end as filas,
       case
           when u.nombre is not null then 'USADA por la app'
           when v.vistas is not null then 'usada por vista: ' || v.vistas
           when f.funciones is not null then 'usada por función: ' || f.funciones
           when k.desde is not null then 'enlazada desde: ' || k.desde
           else 'NO SE USA → se puede borrar'
       end as estado
from objetos o
left join usadas_por_app u on u.nombre = o.nombre
left join en_vistas v on v.oid = o.oid
left join en_funciones f on f.oid = o.oid
left join en_fk k on k.oid = o.oid
order by (u.nombre is null and v.vistas is null and f.funciones is null and k.desde is null) desc, o.tipo, o.nombre;
