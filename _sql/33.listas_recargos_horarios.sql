-- =====================================================================
-- RECARGO POR TRAMOS DE CADA LISTA + HORARIO (antes estaba fijo en el código para el negocio 1)
--
-- Ej. Lista 2 "NOCHE" de 00:00 a 08:00: precio de venta + un recargo según el tramo del precio
--   más de $0 hasta $500 → + $50 · más de $500 hasta $900 → + $100 · ... · más de $14.000 → + $1.000
-- El recargo puede ser en $ fijo o en %. Se configura en Configuración → Listas de precios.
--
-- Este script:
--   1) crea la tabla listas_recargos y la función recalcular_lista2 (la usa el botón "Recalcular precios")
--   2) le carga al negocio v_kiosco (3) la lista NOCHE, el horario 00:00 a 08:00 y los tramos de siempre
--   3) recalcula la Lista 2 de todos los productos de ese negocio con los tramos
--
-- Correr en TEST y en PRODUCCIÓN (después del 15). Se puede correr más de una vez.
-- =====================================================================

create table if not exists public.listas_recargos (
    id               bigserial primary key,
    kiosco_id        bigint not null references public.kioscos(id) on delete cascade,
    lista_precio_id  bigint not null references public.listas_precios(id) on delete cascade,
    desde            numeric(14,2) not null default 0 check (desde >= 0),      -- más de este precio...
    hasta            numeric(14,2) check (hasta is null or hasta > desde),     -- ...hasta este (vacío = sin tope)
    monto            numeric(14,2) not null check (monto >= 0),                -- lo que se suma
    es_porcentaje    boolean not null default false,                           -- true = monto es un %
    creado           timestamptz not null default now()
);
create index if not exists ix_listas_recargos_lista on public.listas_recargos (lista_precio_id, desde);
create index if not exists ix_listas_recargos_kiosco on public.listas_recargos (kiosco_id);

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        alter table public.listas_recargos alter column kiosco_id set default public.mi_kiosco();
        alter table public.listas_recargos enable row level security;
        drop policy if exists aislado_por_negocio on public.listas_recargos;
        create policy aislado_por_negocio on public.listas_recargos for all to authenticated
            using (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()))
            with check (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
        revoke all on public.listas_recargos from anon;
        grant select, insert, update, delete on public.listas_recargos to authenticated;
        grant usage, select on sequence public.listas_recargos_id_seq to authenticated;
    else
        grant select, insert, update, delete on public.listas_recargos to anon, authenticated;
        grant usage, select on sequence public.listas_recargos_id_seq to anon, authenticated;
    end if;
end $$;

-- ---------------------------------------------------------------------
-- Recargo de una lista para un precio (misma regla que PrecioService.CalcularPrecioVentaConRecargo)
-- ---------------------------------------------------------------------
create or replace function public.recargo_lista(p_lista bigint, p_precio numeric)
returns numeric language sql stable as $$
    select coalesce((
        select case when r.es_porcentaje then round(p_precio * r.monto / 100, 2) else r.monto end
        from public.listas_recargos r
        where r.lista_precio_id = p_lista
          and p_precio > r.desde and (r.hasta is null or p_precio <= r.hasta)
        order by r.desde
        limit 1), 0);
$$;

-- ---------------------------------------------------------------------
-- Recalcula precio_lista2 (y su utilidad) de todos los productos del negocio con los tramos.
-- Igual que la app: en GRAMOS el recargo y el redondeo van sobre los 100 g; se respeta "sin redondeo".
-- No se tocan los que tienen la Lista 2 igual al precio de venta (mismo precio de día y de noche).
-- Devuelve cuántos productos cambió. Si el negocio no tiene tramos en la Lista 2, no toca nada.
-- ---------------------------------------------------------------------
create or replace function public.recalcular_lista2(p_kiosco bigint)
returns integer language plpgsql as $$
declare
    v_lista bigint;
    v_red   boolean;
    v_paso  numeric;
    v_n     integer;
begin
    select l.id into v_lista from public.listas_precios l
     where l.kiosco_id = p_kiosco and l.nombre_columna_productos = 'precio_lista2'
     order by l.id limit 1;
    if v_lista is null or not exists (select 1 from public.listas_recargos where lista_precio_id = v_lista) then
        return 0;
    end if;

    select coalesce(k.usar_redondeo, true), case when coalesce(k.multiplo_redondeo, 0) > 0 then k.multiplo_redondeo else 50 end
      into v_red, v_paso
      from public.kioscos k where k.id = p_kiosco;

    with calc as (
        select p.id, p.precio_costo,
               (upper(trim(coalesce(p.unidad, ''))) in ('GR', 'GRS', 'GRAMOS')) as gr,
               (coalesce(v_red, true) and coalesce(p.sin_redondeo, false) = false) as red,
               case when upper(trim(coalesce(p.unidad, ''))) in ('GR', 'GRS', 'GRAMOS')
                    then p.precio_venta / 10 else p.precio_venta end as base
        from public.productos p
        where p.kiosco_id = p_kiosco and coalesce(p.precio_venta, 0) > 0
          -- los que tienen la Lista 2 IGUAL al precio de venta se dejan (ej: gomitas, mismo precio de día y de noche)
          and not (coalesce(p.precio_lista2, 0) > 0 and p.precio_lista2 = p.precio_venta)
    ), l2 as (
        select c.id, c.precio_costo,
               (case when c.red then ceil((c.base + public.recargo_lista(v_lista, c.base)) / v_paso) * v_paso
                     else round(c.base + public.recargo_lista(v_lista, c.base), 2) end)
               * (case when c.gr then 10 else 1 end) as nuevo
        from calc c
    )
    update public.productos p
       set precio_lista2   = l2.nuevo,
           utilidad_lista2 = case when coalesce(l2.precio_costo, 0) > 0
                                  then round((l2.nuevo - l2.precio_costo) / l2.precio_costo * 100, 2) else 0 end
      from l2
     where p.id = l2.id
       and p.precio_lista2 is distinct from l2.nuevo;
    get diagnostics v_n = row_count;
    return v_n;
end $$;

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        revoke all on function public.recalcular_lista2(bigint) from anon;
        grant execute on function public.recalcular_lista2(bigint) to authenticated;
        grant execute on function public.recargo_lista(bigint, numeric) to authenticated;
    else
        grant execute on function public.recalcular_lista2(bigint) to anon, authenticated;
        grant execute on function public.recargo_lista(bigint, numeric) to anon, authenticated;
    end if;
end $$;

-- ---------------------------------------------------------------------
-- Carga inicial para el negocio 3 (los mismos tramos que tenía el negocio 1 en el código)
-- ---------------------------------------------------------------------
do $$
declare
    v_kiosco bigint := 3;      -- << negocio que usa la lista NOCHE
    v_lista  bigint;
    v_n      integer;
begin
    if not exists (select 1 from public.kioscos where id = v_kiosco) then
        raise notice 'No existe el negocio %, no se cargan reglas.', v_kiosco;
        return;
    end if;

    select id into v_lista from public.listas_precios
     where kiosco_id = v_kiosco and nombre_columna_productos = 'precio_lista2' order by id limit 1;
    if v_lista is null then
        insert into public.listas_precios (kiosco_id, nombre_columna_productos, nombre_lista, mostrar_en_buscador)
        values (v_kiosco, 'precio_lista2', 'NOCHE', true) returning id into v_lista;
    end if;

    -- horario: de 00:00 a 08:00 (a las 08:00 en punto ya vuelve el precio de venta)
    if exists (select 1 from public.horarios_ventas where kiosco_id = v_kiosco and lista_precio_id = v_lista) then
        update public.horarios_ventas set hora_inicio = '00:00', hora_fin = '08:00', tipo_ajuste = 'LISTA'
         where kiosco_id = v_kiosco and lista_precio_id = v_lista;
    else
        insert into public.horarios_ventas (kiosco_id, hora_inicio, hora_fin, tipo_ajuste, lista_precio_id)
        values (v_kiosco, '00:00', '08:00', 'LISTA', v_lista);
    end if;

    if not exists (select 1 from public.listas_recargos where lista_precio_id = v_lista) then
        insert into public.listas_recargos (kiosco_id, lista_precio_id, desde, hasta, monto) values
            (v_kiosco, v_lista,     0,   500,   50),
            (v_kiosco, v_lista,   500,   900,  100),
            (v_kiosco, v_lista,   900,  1100,  200),
            (v_kiosco, v_lista,  1100,  5000,  300),
            (v_kiosco, v_lista,  5000,  7000,  400),
            (v_kiosco, v_lista,  7000, 10000,  500),
            (v_kiosco, v_lista, 10000, 14000,  600),
            (v_kiosco, v_lista, 14000,  null, 1000);
    end if;

    v_n := public.recalcular_lista2(v_kiosco);
    raise notice 'Negocio %: Lista 2 recalculada en % producto(s).', v_kiosco, v_n;
end $$;

notify pgrst, 'reload schema';
select 'ok: listas con recargo por tramos y horario' as resultado;
