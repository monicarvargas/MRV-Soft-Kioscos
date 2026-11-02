-- =====================================================================
-- REPORTES DE PRODUCTOS POR SUBFAMILIA
-- Agrega el filtro por subfamilia al reporte de productos (y muestra "FAMILIA › SUBFAMILIA").
-- Correr en TEST y en PRODUCCIÓN, DESPUÉS del 19 y del 26. Se puede correr más de una vez.
-- =====================================================================

drop function if exists public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer);
drop function if exists public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer, bigint);
create or replace function public.reporte_productos(
    p_kiosco     bigint,
    p_tipo       text,
    p_dias       integer default 30,
    p_max        numeric default 5,
    p_familia    bigint  default null,
    p_con_stock  boolean default false,
    p_offset     integer default 0,
    p_limit      integer default 500,
    p_subfamilia bigint  default null)
returns table (
    id bigint, codigo text, nombre text, familia text, proveedor text,
    por_peso boolean, precio_costo numeric, precio_venta numeric, stock numeric,
    fecha_precio text, ultima_venta text, vendido numeric, importe numeric, total_filas bigint)
language sql
stable
set search_path = public
as $$
with desde as (
    select now() - make_interval(days => greatest(coalesce(p_dias, 30), 0)) as d
),
prods as (
    select p.*,
           (coalesce(p.venta_por_peso, false)
            or upper(trim(coalesce(p.unidad, ''))) in ('G','GR','GRS','GRAMO','GRAMOS','KG','KGS','KILO','KILOS')) as es_peso
    from productos p
    where p.kiosco_id = p_kiosco
      and coalesce(p.activo, true)
      and coalesce(p.es_combo, false) = false
      and (p_familia is null or p.familia_id = p_familia)
      and (p_subfamilia is null or p.subfamilia_id = p_subfamilia)
      and (not coalesce(p_con_stock, false) or coalesce(p.stock_actual, 0) > 0)
),
vtas as (
    select d.producto_id,
           sum(d.cantidad) filter (where d.created_at >= (select d from desde)) as cant,
           sum(d.subtotal) filter (where d.created_at >= (select d from desde)) as imp,
           max(d.created_at) as ultima
    from venta_detalles d
    where d.producto_id in (select prods.id from prods)
    group by d.producto_id
),
base as (
    select p.id, p.codigo_barras::text as codigo, p.nombre::text as nombre,
           (f.nombre || coalesce(' › ' || sf.nombre, ''))::text as familia, pr.nombre::text as proveedor,
           p.es_peso as por_peso, p.precio_costo, p.precio_venta,
           case when p.es_peso then coalesce(p.stock_actual, 0) / 1000.0 else coalesce(p.stock_actual, 0) end as stock,
           p.fecha_precio as f_precio, v.ultima,
           case when p.es_peso then coalesce(v.cant, 0) / 1000.0 else coalesce(v.cant, 0) end as vendido,
           coalesce(v.imp, 0) as importe
    from prods p
    left join vtas v on v.producto_id = p.id
    left join familias f on f.id = p.familia_id
    left join subfamilias sf on sf.id = p.subfamilia_id
    left join proveedores pr on pr.id = p.proveedor_id
),
filtrado as (
    select b.* from base b
    where case p_tipo
            when 'precio'    then coalesce(b.f_precio, '-infinity') < (select d from desde)
            when 'poca'      then b.vendido > 0 and b.vendido <= coalesce(p_max, 5)
            when 'sin_venta' then b.ultima is null or b.ultima < (select d from desde)
            else false
          end
)
select x.id, x.codigo, x.nombre, x.familia, x.proveedor, x.por_peso, x.precio_costo, x.precio_venta, round(x.stock, 3),
       to_char(x.f_precio at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD'),
       to_char(x.ultima   at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD'),
       round(x.vendido, 3), x.importe,
       count(*) over ()
from filtrado x
order by
    case when p_tipo = 'precio'    then x.f_precio end asc nulls first,
    case when p_tipo = 'poca'      then x.vendido  end asc,
    case when p_tipo = 'sin_venta' then x.ultima   end asc nulls first,
    x.stock desc, x.nombre
offset greatest(coalesce(p_offset, 0), 0)
limit  least(greatest(coalesce(p_limit, 500), 1), 1000);
$$;

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        revoke all on function public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer, bigint) from public, anon;
        grant execute on function public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer, bigint) to authenticated;
    else
        grant execute on function public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer, bigint) to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Reporte de productos con subfamilias: listo' as resultado;
