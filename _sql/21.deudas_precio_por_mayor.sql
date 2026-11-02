-- =====================================================================
-- DEUDAS (FIADO) A PRECIO DE HOY: respetar el PRECIO POR MAYOR
-- Antes, una compra fiada por mayor (ej: 6 abrazaderas a $133,33 c/u = $800) se comparaba
-- con el precio por unidad de hoy (6 x $200 = $1.200) y mostraba un "aumento" falso de $400.
-- Ahora, si la cantidad llega al mínimo por mayor, se compara contra el precio por mayor de hoy.
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

create or replace view public.vw_resumen_deudores as
 WITH pagos AS (
         SELECT v.persona_id,
            v.kiosco_id,
            sum(v.total) AS pago_entregado
           FROM ventas v
          WHERE (EXISTS ( SELECT 1
                   FROM ventas vp
                  WHERE vp.numero_unico_identificatorio_movimiento = v.numero_unico_identificatorio_movimiento AND vp.total < 0::numeric))
          GROUP BY v.persona_id, v.kiosco_id
        ), movs AS (
         SELECT DISTINCT ventas.numero_unico_identificatorio_movimiento AS mov
           FROM ventas
          WHERE ventas.pagado = false
        ), items AS (
         SELECT vd.numero_unico_identificatorio_movimiento AS mov,
            sum(vd.subtotal) AS hist,
            sum(GREATEST(vd.subtotal,
                CASE
                    WHEN lower(TRIM(BOTH FROM prod.unidad)) = ANY (ARRAY['g'::text, 'gr'::text, 'grs'::text, 'gramos'::text, 'kg'::text, 'kilo'::text, 'kilos'::text])
                        THEN prod.precio_venta / 1000::numeric * vd.cantidad::numeric
                    -- precio por mayor: desde cant_por_mayor unidades, cada una a precio_venta_por_mayor
                    WHEN COALESCE(prod.cant_por_mayor, 0) > 1
                         AND COALESCE(prod.precio_venta_por_mayor, 0) > 0
                         AND vd.cantidad >= prod.cant_por_mayor
                        THEN prod.precio_venta_por_mayor * vd.cantidad::numeric
                    ELSE prod.precio_venta * vd.cantidad::numeric
                END)) AS hoy
           FROM venta_detalles vd
             JOIN productos prod ON prod.id = vd.producto_id
          WHERE (vd.numero_unico_identificatorio_movimiento IN ( SELECT movs.mov
                   FROM movs))
          GROUP BY vd.numero_unico_identificatorio_movimiento
        ), varios AS (
         SELECT productos_varios_ventas.numero_unico_identificatorio_movimiento AS mov,
            sum(productos_varios_ventas.subtotal) AS total
           FROM productos_varios_ventas
          WHERE (productos_varios_ventas.numero_unico_identificatorio_movimiento IN ( SELECT movs.mov
                   FROM movs))
          GROUP BY productos_varios_ventas.numero_unico_identificatorio_movimiento
        ), deudas AS (
         SELECT v.kiosco_id,
            v.persona_id,
            sum(v.total) AS historico,
            sum(
                CASE
                    WHEN v.es_ajuste_precio THEN 0::numeric
                    WHEN (COALESCE(i.hist, 0::numeric) + COALESCE(va.total, 0::numeric)) > 0::numeric THEN v.total * (COALESCE(i.hoy, 0::numeric) + COALESCE(va.total, 0::numeric)) / (COALESCE(i.hist, 0::numeric) + COALESCE(va.total, 0::numeric))
                    ELSE v.total
                END) AS actual
           FROM ventas v
             LEFT JOIN items i ON i.mov = v.numero_unico_identificatorio_movimiento
             LEFT JOIN varios va ON va.mov = v.numero_unico_identificatorio_movimiento
          WHERE v.pagado = false AND v.persona_id IS NOT NULL
          GROUP BY v.kiosco_id, v.persona_id
        )
 SELECT d.kiosco_id,
    d.persona_id,
    p.nombre,
    COALESCE(pg.pago_entregado, 0::numeric) AS total_pago_entregado,
    round(d.actual, 2) AS deuda_precio_actual,
    d.historico AS deuda_precio_historico
   FROM deudas d
     JOIN personas p ON p.id = d.persona_id
     LEFT JOIN pagos pg ON pg.persona_id = d.persona_id AND pg.kiosco_id = d.kiosco_id
  ORDER BY p.nombre;

-- que siga respetando la seguridad por negocio
alter view public.vw_resumen_deudores set (security_invoker = true);

notify pgrst, 'reload schema';
select 'Deudas a precio de hoy: ahora respetan el precio por mayor' as resultado;
