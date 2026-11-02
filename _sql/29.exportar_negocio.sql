-- =====================================================================
-- "HACER COPIA" (Configuración → Copia de seguridad): baja los datos del negocio a un Excel.
-- exportar_negocio(p_kiosco, p_dias) devuelve todo junto (una hoja por tema).
-- Respeta la seguridad por negocio (cada uno ve solo lo suyo).
-- Correr en TEST y en PRODUCCIÓN, DESPUÉS del 26. Se puede correr más de una vez.
-- =====================================================================

drop function if exists public.exportar_negocio(bigint, integer);
create or replace function public.exportar_negocio(p_kiosco bigint, p_dias integer default 90)
returns json
language sql
stable
security invoker
set search_path = public
as $$
with desde as (
    select (now() at time zone 'America/Argentina/Buenos_Aires')::date - greatest(coalesce(p_dias, 90), 1) as fd
)
select json_build_object(
  'Productos', coalesce((select json_agg(row_to_json(x) order by x."Producto") from (
      select p.codigo_barras as "Codigo", p.nombre as "Producto", p.marca as "Marca",
             f.nombre as "Familia", sf.nombre as "Subfamilia", pr.nombre as "Proveedor",
             p.precio_costo as "Costo", p.utilidad_pv as "Utilidad %", p.precio_venta as "Precio venta",
             p.precio_lista2 as "Lista 2", p.precio_venta_por_mayor as "Precio x mayor", p.cant_por_mayor as "Desde cant. x mayor",
             p.stock_actual as "Stock", p.stock_minimo as "Stock minimo", p.stock_ideal as "Stock ideal", p.unidad as "Unidad",
             case when coalesce(p.es_combo, false) then 'SI' else 'NO' end as "Combo",
             case when coalesce(p.activo, true) then 'SI' else 'NO' end as "Activo"
      from productos p
      left join familias f on f.id = p.familia_id
      left join subfamilias sf on sf.id = p.subfamilia_id
      left join proveedores pr on pr.id = p.proveedor_id
      where p.kiosco_id = p_kiosco) x), '[]'::json),

  'Familias', coalesce((select json_agg(row_to_json(x) order by x."Familia", x."Subfamilia" nulls first) from (
      select f.nombre as "Familia", sf.nombre as "Subfamilia"
      from familias f left join subfamilias sf on sf.familia_id = f.id
      where f.kiosco_id = p_kiosco) x), '[]'::json),

  'Proveedores', coalesce((select json_agg(row_to_json(x) order by x."Proveedor") from (
      select pr.nombre as "Proveedor", pr.cuit as "CUIT", pr.telefono as "Telefono", pr.email as "Email", pr.direccion as "Direccion",
             s.saldo as "Le debemos"
      from proveedores pr left join vw_saldo_proveedores s on s.proveedor_id = pr.id
      where pr.kiosco_id = p_kiosco) x), '[]'::json),

  'Clientes', coalesce((select json_agg(row_to_json(x) order by x."Nombre") from (
      select pe.nombre as "Nombre", pe.cuit as "CUIT/DNI", pe.telefono as "Telefono", pe.email as "Email", pe.direccion as "Direccion",
             case when coalesce(pe.es_empleado, false) then 'SI' else 'NO' end as "Empleado",
             case when coalesce(pe.habilitado, true) then 'SI' else 'NO' end as "Habilitado", pe.nota as "Nota"
      from personas pe where pe.kiosco_id = p_kiosco) x), '[]'::json),

  'Deudas', coalesce((select json_agg(row_to_json(x) order by x."Cliente") from (
      select d.nombre as "Cliente", d.deuda_precio_actual as "Debe (precio de hoy)",
             d.deuda_precio_historico as "Debe (precio de cuando llevo)", d.total_pago_entregado as "Entrego a cuenta"
      from vw_resumen_deudores d where d.kiosco_id = p_kiosco) x), '[]'::json),

  'Deudas detalle', coalesce((select json_agg(row_to_json(x) order by x."Cliente", x."Fecha") from (
      select d.nombre as "Cliente", to_char(d.fecha, 'YYYY-MM-DD') as "Fecha", d.numero_unico_identificatorio_movimiento as "Movimiento",
             d.codigo_barras as "Codigo", d.producto as "Producto", d.cantidad as "Cantidad",
             d.precio_historico as "Precio de cuando llevo", d.precio_actual as "Precio de hoy", d.subtotal_actual as "Subtotal hoy"
      from vw_deudas_pendientes_detallado d where d.kiosco_id = p_kiosco) x), '[]'::json),

  'Ventas', coalesce((select json_agg(row_to_json(x) order by x."Fecha", x."Hora", x."Movimiento") from (
      select to_char(v.fecha at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD') as "Fecha",
             to_char(coalesce(v.hora_venta, v.fecha) at time zone 'America/Argentina/Buenos_Aires', 'HH24:MI') as "Hora",
             v.numero_unico_identificatorio_movimiento as "Movimiento", fp.nombre as "Forma de pago", v.total as "Total",
             pe.nombre as "Cliente", u.username as "Usuario",
             case when coalesce(v.pagado, true) then 'SI' else 'NO (fiado)' end as "Pagado"
      from ventas v
      left join formas_pago fp on fp.id = v.forma_pago_id
      left join personas pe on pe.id = v.persona_id
      left join usuarios u on u.id = v.usuario_operador_id
      where v.kiosco_id = p_kiosco and v.fecha >= (select fd from desde)) x), '[]'::json),

  'Ventas detalle', coalesce((select json_agg(row_to_json(x) order by x."Fecha", x."Movimiento") from (
      select to_char(d.created_at at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD HH24:MI') as "Fecha",
             d.numero_unico_identificatorio_movimiento as "Movimiento", p.codigo_barras as "Codigo", p.nombre as "Producto",
             d.cantidad as "Cantidad", d.precio_unitario as "Precio", d.subtotal as "Subtotal"
      from venta_detalles d join productos p on p.id = d.producto_id
      where p.kiosco_id = p_kiosco and d.created_at >= (select fd from desde)
      union all
      select to_char(w.created_at at time zone 'America/Argentina/Buenos_Aires', 'YYYY-MM-DD HH24:MI'),
             w.numero_unico_identificatorio_movimiento, null, coalesce(w.nombre_producto, 'VARIOS') || ' (sin código)',
             w.cantidad, w.preciounitario, w.subtotal
      from productos_varios_ventas w
      where w.kiosco_id = p_kiosco and w.created_at >= (select fd from desde)) x), '[]'::json)
);
$$;

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        revoke all on function public.exportar_negocio(bigint, integer) from public, anon;
        grant execute on function public.exportar_negocio(bigint, integer) to authenticated;
    else
        grant execute on function public.exportar_negocio(bigint, integer) to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Copia del negocio en Excel: listo' as resultado;
