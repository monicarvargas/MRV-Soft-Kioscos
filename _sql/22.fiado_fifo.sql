-- =====================================================================
-- FIADO: APLICAR LAS ENTREGAS A LAS DEUDAS (FIFO) SIN RENGLONES FALSOS
--
-- Deuda   = renglón de ventas con forma de pago FIADO (o ajuste por aumento), total > 0, de una persona.
-- Entrega = renglón de ventas con total < 0 de una persona (efectivo, transferencia... o "Descontar").
--
-- La plata de las entregas se aplica a las deudas de la más vieja a la más nueva:
--   una entrega puede saldar varias deudas y una deuda puede necesitar varias entregas.
-- Cada aplicación queda en la tabla fiado_aplicaciones ("de la entrega X se usaron $Y para la deuda Z").
-- Una deuda queda pagado = true cuando lo aplicado llega a su total.
-- En "ventas" no se agrega nada: cada venta y cada entrega aparece una sola vez.
--
-- La cuenta se recalcula sola cada vez que se carga una venta o una entrega de esa persona
-- (todo junto o nada: si algo falla, no queda a medias).
--
-- Este script también:
--   * guarda en _ventas_equilibrio_respaldo y BORRA los renglones de "equilibrio" viejos
--     (renglones positivos que se agregaban con el número de una entrega);
--   * recalcula la cuenta de todas las personas con el método nuevo.
-- Correr en TEST y en PRODUCCIÓN (después del 15 y del 21). Se puede correr más de una vez.
-- =====================================================================

-- 1) Tabla de aplicaciones
create table if not exists public.fiado_aplicaciones (
    id             bigserial primary key,
    kiosco_id      bigint not null references public.kioscos(id) on delete cascade,
    persona_id     bigint not null,
    pago_venta_id  bigint not null references public.ventas(id) on delete cascade,   -- renglón de la entrega
    deuda_venta_id bigint not null references public.ventas(id) on delete cascade,   -- renglón de la deuda
    monto          numeric(14,2) not null check (monto > 0),
    creado         timestamptz not null default now()
);
create index if not exists ix_fiado_apl_persona on public.fiado_aplicaciones (persona_id);
create index if not exists ix_fiado_apl_deuda   on public.fiado_aplicaciones (deuda_venta_id);
create index if not exists ix_fiado_apl_pago    on public.fiado_aplicaciones (pago_venta_id);
create index if not exists ix_ventas_persona    on public.ventas (persona_id) where persona_id is not null;

-- 2) Recalcular la cuenta de una persona (FIFO desde cero: siempre da lo mismo, no importa cuántas veces se corra)
create or replace function public.recalcular_fiado(p_persona bigint)
returns void
language plpgsql
set search_path = public
as $$
declare
    v_kiosco bigint;
    d_id bigint[];  d_resto numeric[];
    p_id bigint[];  p_resto numeric[];
    i int := 1; j int := 1; nd int; np int;
    usar numeric;
begin
    if p_persona is null then return; end if;

    -- movimientos que son entregas (tienen algún renglón negativo)
    with movs_pago as (
        select distinct numero_unico_identificatorio_movimiento as mov
        from ventas where persona_id = p_persona and total < 0
    )
    select array_agg(v.id order by v.numero_unico_identificatorio_movimiento, v.id),
           array_agg(v.total order by v.numero_unico_identificatorio_movimiento, v.id),
           max(v.kiosco_id)
      into d_id, d_resto, v_kiosco
    from ventas v
    left join formas_pago fp on fp.id = v.forma_pago_id
    where v.persona_id = p_persona
      and v.total > 0
      and (upper(trim(coalesce(fp.nombre, ''))) = 'FIADO' or coalesce(v.es_ajuste_precio, false))
      and v.numero_unico_identificatorio_movimiento not in (select mov from movs_pago);

    select array_agg(v.id order by v.numero_unico_identificatorio_movimiento, v.id),
           array_agg(-v.total order by v.numero_unico_identificatorio_movimiento, v.id),
           coalesce(v_kiosco, max(v.kiosco_id))
      into p_id, p_resto, v_kiosco
    from ventas v
    where v.persona_id = p_persona and v.total < 0;

    delete from fiado_aplicaciones where persona_id = p_persona;

    nd := coalesce(array_length(d_id, 1), 0);
    np := coalesce(array_length(p_id, 1), 0);
    if nd = 0 then return; end if;

    while i <= nd and j <= np loop
        usar := least(d_resto[i], p_resto[j]);
        if usar > 0 then
            insert into fiado_aplicaciones (kiosco_id, persona_id, pago_venta_id, deuda_venta_id, monto)
            values (v_kiosco, p_persona, p_id[j], d_id[i], usar);
            d_resto[i] := d_resto[i] - usar;
            p_resto[j] := p_resto[j] - usar;
        end if;
        if d_resto[i] <= 0 then i := i + 1; end if;
        if p_resto[j] <= 0 then j := j + 1; end if;
    end loop;

    -- saldadas: las primeras (i - 1); el resto queda debiendo
    update ventas set pagado = true
     where id = any (d_id[1:i-1]) and pagado is distinct from true;
    update ventas set pagado = false
     where id = any (d_id[i:nd]) and pagado is distinct from false;
end $$;

-- 3) Renglones de "equilibrio" viejos: se guardan y se borran
--    (positivos dentro de un movimiento de entrega; no son plata, eran el método anterior)
create table if not exists public._ventas_equilibrio_respaldo as
    select v.*, now() as respaldado from public.ventas v where false;
alter table public._ventas_equilibrio_respaldo enable row level security;   -- sin reglas: solo se ve desde Supabase
revoke all on public._ventas_equilibrio_respaldo from anon, authenticated;

drop trigger if exists trg_ventas_recalcular_fiado on public.ventas;          -- que no se disparen mientras se limpia
drop trigger if exists trg_ventas_recalcular_fiado_borrar on public.ventas;

with movs_pago as (
    select distinct numero_unico_identificatorio_movimiento as mov from public.ventas where total < 0
), equilibrio as (
    select v.* from public.ventas v
    where v.total > 0 and v.numero_unico_identificatorio_movimiento in (select mov from movs_pago)
), guardados as (
    insert into public._ventas_equilibrio_respaldo select e.*, now() from equilibrio e returning id
)
delete from public.ventas where id in (select id from guardados);

-- 4) Recalcular todas las personas con el método nuevo
do $$
declare r record;
begin
    for r in select distinct persona_id from public.ventas where persona_id is not null loop
        perform public.recalcular_fiado(r.persona_id);
    end loop;
end $$;

-- 5) De ahora en más se recalcula solo al cargar o borrar una venta / entrega de una persona
create or replace function public.tg_ventas_recalcular_fiado()
returns trigger
language plpgsql
set search_path = public
as $$
begin
    if tg_op = 'DELETE' then
        perform public.recalcular_fiado(old.persona_id);
    else
        perform public.recalcular_fiado(new.persona_id);
    end if;
    return null;
end $$;

create trigger trg_ventas_recalcular_fiado
    after insert on public.ventas
    for each row when (new.persona_id is not null)
    execute function public.tg_ventas_recalcular_fiado();
create trigger trg_ventas_recalcular_fiado_borrar
    after delete on public.ventas
    for each row when (old.persona_id is not null)
    execute function public.tg_ventas_recalcular_fiado();

-- 6) Vistas que usa la app (mismas columnas que antes)

-- Entregas que todavía cuentan para la deuda pendiente:
--   lo que se aplicó a deudas que siguen abiertas + lo que sobra sin aplicar (saldo a favor). Va en negativo.
create or replace view public.vw_pagos_parciales_x_deudas as
 WITH apl AS (
         SELECT a.pago_venta_id,
            sum(a.monto) AS aplicado,
            sum(a.monto) FILTER (WHERE d.pagado = false) AS aplicado_pendiente
           FROM fiado_aplicaciones a
             JOIN ventas d ON d.id = a.deuda_venta_id
          GROUP BY a.pago_venta_id
        )
 SELECT v.persona_id,
    v.kiosco_id,
    - (COALESCE(apl.aplicado_pendiente, 0::numeric) + (- v.total - COALESCE(apl.aplicado, 0::numeric))) AS pago_entregado,
    v.forma_pago_id,
    v.numero_unico_identificatorio_movimiento,
    fp.nombre AS fp_nombre,
    v.fecha
   FROM ventas v
     JOIN formas_pago fp ON fp.id = v.forma_pago_id
     LEFT JOIN apl ON apl.pago_venta_id = v.id
  WHERE v.total < 0::numeric AND v.persona_id IS NOT NULL
    AND (COALESCE(apl.aplicado_pendiente, 0::numeric) + (- v.total - COALESCE(apl.aplicado, 0::numeric))) > 0::numeric;

alter view public.vw_pagos_parciales_x_deudas set (security_invoker = true);

-- Resumen por deudor (deudas abiertas):
--   deuda_precio_historico = total de las deudas abiertas
--   deuda_precio_actual    = lo ya entregado + lo que falta, actualizado al precio de hoy (precio por mayor incluido;
--                            los ajustes por aumento no suman de nuevo, igual que antes)
--   total_pago_entregado   = lo entregado a esas deudas + saldo a favor (en negativo)
create or replace view public.vw_resumen_deudores as
 WITH deudas AS (
         SELECT v.id, v.kiosco_id, v.persona_id, v.total, v.es_ajuste_precio,
            v.numero_unico_identificatorio_movimiento AS mov
           FROM ventas v
          WHERE v.pagado = false AND v.persona_id IS NOT NULL AND v.total > 0::numeric
        ), apl AS (
         SELECT a.deuda_venta_id, sum(a.monto) AS aplicado
           FROM fiado_aplicaciones a
          GROUP BY a.deuda_venta_id
        ), items AS (
         SELECT vd.numero_unico_identificatorio_movimiento AS mov,
            sum(vd.subtotal) AS hist,
            sum(GREATEST(vd.subtotal,
                CASE
                    WHEN lower(TRIM(BOTH FROM prod.unidad)) = ANY (ARRAY['g'::text, 'gr'::text, 'grs'::text, 'gramos'::text, 'kg'::text, 'kilo'::text, 'kilos'::text])
                        THEN prod.precio_venta / 1000::numeric * vd.cantidad::numeric
                    WHEN COALESCE(prod.cant_por_mayor, 0) > 1
                         AND COALESCE(prod.precio_venta_por_mayor, 0::numeric) > 0::numeric
                         AND vd.cantidad >= prod.cant_por_mayor
                        THEN prod.precio_venta_por_mayor * vd.cantidad::numeric
                    ELSE prod.precio_venta * vd.cantidad::numeric
                END)) AS hoy
           FROM venta_detalles vd
             JOIN productos prod ON prod.id = vd.producto_id
          WHERE vd.numero_unico_identificatorio_movimiento IN (SELECT deudas.mov FROM deudas)
          GROUP BY vd.numero_unico_identificatorio_movimiento
        ), varios AS (
         SELECT pv.numero_unico_identificatorio_movimiento AS mov,
            sum(pv.subtotal) AS total
           FROM productos_varios_ventas pv
          WHERE pv.numero_unico_identificatorio_movimiento IN (SELECT deudas.mov FROM deudas)
          GROUP BY pv.numero_unico_identificatorio_movimiento
        ), por_deuda AS (
         SELECT d.kiosco_id, d.persona_id, d.total,
            COALESCE(a.aplicado, 0::numeric) AS aplicado,
            CASE
                -- el ajuste por aumento ya está incluido al calcular las otras deudas a precio de hoy:
                -- de él solo cuenta lo que ya se le entregó (si no, el aumento se contaría dos veces)
                WHEN COALESCE(d.es_ajuste_precio, false) THEN 0::numeric
                WHEN (COALESCE(i.hist, 0::numeric) + COALESCE(va.total, 0::numeric)) > 0::numeric
                    THEN (COALESCE(i.hoy, 0::numeric) + COALESCE(va.total, 0::numeric)) / (COALESCE(i.hist, 0::numeric) + COALESCE(va.total, 0::numeric))
                ELSE 1::numeric
            END AS factor
           FROM deudas d
             LEFT JOIN apl a ON a.deuda_venta_id = d.id
             LEFT JOIN items i ON i.mov = d.mov
             LEFT JOIN varios va ON va.mov = d.mov
        ), resumen AS (
         SELECT kiosco_id, persona_id,
            sum(total) AS historico,
            sum(aplicado + (total - aplicado) * factor) AS actual,
            sum(aplicado) AS aplicado
           FROM por_deuda
          GROUP BY kiosco_id, persona_id
        ), libre AS (
         SELECT v.persona_id, sum(- v.total - COALESCE(x.usado, 0::numeric)) AS saldo
           FROM ventas v
             LEFT JOIN (SELECT pago_venta_id, sum(monto) AS usado FROM fiado_aplicaciones GROUP BY pago_venta_id) x ON x.pago_venta_id = v.id
          WHERE v.total < 0::numeric AND v.persona_id IS NOT NULL
          GROUP BY v.persona_id
        )
 SELECT r.kiosco_id,
    r.persona_id,
    p.nombre,
    - (r.aplicado + COALESCE(GREATEST(l.saldo, 0::numeric), 0::numeric)) AS total_pago_entregado,
    round(r.actual, 2) AS deuda_precio_actual,
    r.historico AS deuda_precio_historico
   FROM resumen r
     JOIN personas p ON p.id = r.persona_id
     LEFT JOIN libre l ON l.persona_id = r.persona_id
  ORDER BY p.nombre;

alter view public.vw_resumen_deudores set (security_invoker = true);

-- 7) Permisos (igual que el resto de las tablas)
do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        alter table public.fiado_aplicaciones enable row level security;
        drop policy if exists aislado_por_negocio on public.fiado_aplicaciones;
        create policy aislado_por_negocio on public.fiado_aplicaciones for all to authenticated
            using (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()))
            with check (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
        revoke all on public.fiado_aplicaciones from anon;
        grant select, insert, update, delete on public.fiado_aplicaciones to authenticated;
        grant usage, select on sequence public.fiado_aplicaciones_id_seq to authenticated;
        revoke all on function public.recalcular_fiado(bigint) from public, anon;
        grant execute on function public.recalcular_fiado(bigint) to authenticated;
    else
        grant select, insert, update, delete on public.fiado_aplicaciones to anon, authenticated;
        grant usage, select on sequence public.fiado_aplicaciones_id_seq to anon, authenticated;
        grant execute on function public.recalcular_fiado(bigint) to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select (select count(*) from public._ventas_equilibrio_respaldo) as renglones_equilibrio_guardados,
       (select count(*) from public.fiado_aplicaciones) as aplicaciones,
       (select count(*) from public.ventas where pagado = false) as deudas_abiertas;
