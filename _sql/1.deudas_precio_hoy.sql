-- =====================================================================
-- Deudas (fiado): cobrar a precio viejo o a precio de hoy
-- Correr TODO junto en Supabase > SQL Editor, ANTES de probar la app.
-- =====================================================================

-- 1) Marca para el renglón "ajuste por aumento de precios"
alter table ventas add column if not exists es_ajuste_precio boolean not null default false;


-- 2) Detalle de lo que se debe (por producto)
--    Arreglos: los productos por peso (gramos / kilos) se calculaban como si fueran unidades
--    (200 g de fiambre a $10.000 el kilo daba $2.000.000) y el "histórico" ahora es lo que
--    realmente se cobró en ese momento (venta_detalles.subtotal).
drop view if exists vw_deudas_pendientes_detallado;
create view vw_deudas_pendientes_detallado as
select v.kiosco_id,
       p.id                                     as persona_id,
       p.nombre,
       v.numero_unico_identificatorio_movimiento,
       v.fecha,
       prod.id                                  as producto_id,
       prod.codigo_barras,
       prod.nombre                              as producto,
       vd.cantidad,
       vd.precio_unitario                       as precio_historico,
       prod.precio_venta                        as precio_actual,
       vd.subtotal                              as subtotal_historico,
       case when lower(trim(prod.unidad)) in ('gr','grs','gramos','kg','kilo','kilos')
            then prod.precio_venta / 1000 * vd.cantidad
            else prod.precio_venta * vd.cantidad
       end                                      as subtotal_actual
from ventas v
join venta_detalles vd on vd.numero_unico_identificatorio_movimiento = v.numero_unico_identificatorio_movimiento
join personas p        on p.id = v.persona_id
join productos prod    on prod.id = vd.producto_id
where v.pagado = false
order by p.nombre;


-- 3) Resumen por deudor
--    Arreglos:
--    * La deuda "histórica" ahora es lo que quedó fiado (ventas.total), igual que en Cobrar fiado.
--      Antes sumaba TODOS los productos de la compra aunque una parte se haya pagado en efectivo.
--    * Ahora cuentan los productos "varios" fiados (antes no aparecían).
--    * Precio de hoy: cada fiado se actualiza en proporción a cuánto subieron SUS productos
--      (solo aumentos; si un producto bajó, se respeta el precio de cuando lo sacó).
--    * Los ajustes por aumento ya hechos suman a lo histórico pero no a "precio de hoy"
--      (porque ya están incluidos en él).
--    * Los productos en kilos se calculan bien.
drop view if exists vw_resumen_deudores;
create view vw_resumen_deudores as
with pagos as (                       -- (igual que antes)
    select v.persona_id, v.kiosco_id, sum(v.total) as pago_entregado
    from ventas v
    where exists (select 1 from ventas vp
                  where vp.numero_unico_identificatorio_movimiento = v.numero_unico_identificatorio_movimiento
                    and vp.total < 0)
    group by v.persona_id, v.kiosco_id
),
movs as (                             -- movimientos con algo fiado sin pagar
    select distinct numero_unico_identificatorio_movimiento as mov
    from ventas
    where pagado = false
),
items as (                            -- cuánto valía cada compra y cuánto vale hoy
    select vd.numero_unico_identificatorio_movimiento as mov,
           sum(vd.subtotal) as hist,
           sum(greatest(vd.subtotal,
               case when lower(trim(prod.unidad)) in ('gr','grs','gramos','kg','kilo','kilos')
                    then prod.precio_venta / 1000 * vd.cantidad
                    else prod.precio_venta * vd.cantidad
               end)) as hoy
    from venta_detalles vd
    join productos prod on prod.id = vd.producto_id
    where vd.numero_unico_identificatorio_movimiento in (select mov from movs)
    group by vd.numero_unico_identificatorio_movimiento
),
varios as (                           -- productos "varios": valen lo mismo hoy
    select numero_unico_identificatorio_movimiento as mov, sum(subtotal) as total
    from productos_varios_ventas
    where numero_unico_identificatorio_movimiento in (select mov from movs)
    group by numero_unico_identificatorio_movimiento
),
deudas as (
    select v.kiosco_id,
           v.persona_id,
           sum(v.total) as historico,
           sum(case
                 when v.es_ajuste_precio then 0
                 when coalesce(i.hist, 0) + coalesce(va.total, 0) > 0
                   then v.total * (coalesce(i.hoy, 0) + coalesce(va.total, 0))
                                / (coalesce(i.hist, 0) + coalesce(va.total, 0))
                 else v.total
               end) as actual
    from ventas v
    left join items  i  on i.mov  = v.numero_unico_identificatorio_movimiento
    left join varios va on va.mov = v.numero_unico_identificatorio_movimiento
    where v.pagado = false
      and v.persona_id is not null
    group by v.kiosco_id, v.persona_id
)
select d.kiosco_id,
       d.persona_id,
       p.nombre,
       coalesce(pg.pago_entregado, 0) as total_pago_entregado,
       round(d.actual, 2)             as deuda_precio_actual,
       d.historico                    as deuda_precio_historico
from deudas d
join personas p  on p.id = d.persona_id
left join pagos pg on pg.persona_id = d.persona_id and pg.kiosco_id = d.kiosco_id
order by p.nombre;

-- vw_pagos_parciales_x_deudas: no cambia.
