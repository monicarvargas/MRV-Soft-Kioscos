-- =====================================================================
-- REPORTES DE PRODUCTOS
--   * precios sin actualizar hace X días
--   * productos con poca venta en los últimos X días
--   * productos que no se venden hace X días (o nunca)
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

-- 1) Fecha del último cambio de PRECIO.
--    (fecha_actualizacion no sirve: se mueve también con cada venta o ingreso de stock)
alter table public.productos add column if not exists fecha_precio timestamptz;
update public.productos set fecha_precio = coalesce(fecha_actualizacion, now()) where fecha_precio is null;
alter table public.productos alter column fecha_precio set default now();

create or replace function public.tg_productos_fecha_precio()
 returns trigger
 language plpgsql
as $function$
begin
    if new.precio_venta is distinct from old.precio_venta
       or new.precio_lista2 is distinct from old.precio_lista2 then
        new.fecha_actualizacion := now();
    end if;
    -- cualquier cambio de costo o de precios de venta = precio actualizado
    if new.precio_venta            is distinct from old.precio_venta
       or new.precio_costo         is distinct from old.precio_costo
       or new.precio_lista2        is distinct from old.precio_lista2
       or new.precio_lista3        is distinct from old.precio_lista3
       or new.precio_lista4        is distinct from old.precio_lista4
       or new.precio_lista5        is distinct from old.precio_lista5
       or new.precio_venta_por_mayor is distinct from old.precio_venta_por_mayor then
        new.fecha_precio := now();
    end if;
    return new;
end $function$;

drop trigger if exists trg_productos_fecha_precio on public.productos;
create trigger trg_productos_fecha_precio before update on public.productos
    for each row execute function public.tg_productos_fecha_precio();

-- 2) Índice para buscar rápido las ventas de cada producto por fecha
create index if not exists ix_venta_detalles_producto_fecha on public.venta_detalles (producto_id, created_at);

-- 3) El reporte (respeta la seguridad por negocio: corre con los permisos de quien lo pide)
--    p_tipo: 'precio' | 'poca' | 'sin_venta'
--    p_dias: cantidad de días hacia atrás
--    p_max : (solo 'poca') vendió como mucho esta cantidad (unidades, o kilos en los de peso)
drop function if exists public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer);
create or replace function public.reporte_productos(
    p_kiosco     bigint,
    p_tipo       text,
    p_dias       integer default 30,
    p_max        numeric default 5,
    p_familia    bigint  default null,
    p_con_stock  boolean default false,
    p_offset     integer default 0,
    p_limit      integer default 500)
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
           f.nombre::text as familia, pr.nombre::text as proveedor,
           p.es_peso as por_peso, p.precio_costo, p.precio_venta,
           case when p.es_peso then coalesce(p.stock_actual, 0) / 1000.0 else coalesce(p.stock_actual, 0) end as stock,
           p.fecha_precio as f_precio, v.ultima,
           case when p.es_peso then coalesce(v.cant, 0) / 1000.0 else coalesce(v.cant, 0) end as vendido,
           coalesce(v.imp, 0) as importe
    from prods p
    left join vtas v on v.producto_id = p.id
    left join familias f on f.id = p.familia_id
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

-- 4) "El precio está bien": marca como revisados sin cambiar el precio
create or replace function public.marcar_precio_revisado(p_ids bigint[])
returns integer
language sql
set search_path = public
as $$
    with u as (update productos set fecha_precio = now() where id = any(p_ids) returning 1)
    select count(*)::integer from u;
$$;

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        revoke all on function public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer) from public, anon;
        revoke all on function public.marcar_precio_revisado(bigint[]) from public, anon;
        grant execute on function public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer) to authenticated;
        grant execute on function public.marcar_precio_revisado(bigint[]) to authenticated;
    else
        grant execute on function public.reporte_productos(bigint, text, integer, numeric, bigint, boolean, integer, integer) to anon, authenticated;
        grant execute on function public.marcar_precio_revisado(bigint[]) to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Reportes de productos listos' as resultado;
