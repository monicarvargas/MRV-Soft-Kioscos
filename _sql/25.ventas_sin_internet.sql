-- =====================================================================
-- ETAPA 7 · VENDER SIN INTERNET (app de escritorio)
--
-- Cuando se corta internet, la PC guarda las ventas en un archivo. Cuando vuelve, las sube con
-- subir_venta_offline(): cada venta se carga UNA sola vez (aunque se intente subir dos veces),
-- con su hora real, y descuenta el stock igual que una venta normal.
--
-- Para subir no hace falta que haya alguien logueado: cada PC tiene una "llave de equipo"
-- que se le da (registrar_equipo) la primera vez que alguien entra CON internet a ese negocio.
--
-- Correr en TEST y en PRODUCCIÓN (después del 15). Se puede correr más de una vez.
-- =====================================================================

-- 1) Llaves de los equipos (se guarda solo el hash, nunca la llave)
create table if not exists public.equipos_offline (
    token_hash text primary key,
    kiosco_id  bigint not null references public.kioscos(id) on delete cascade,
    activo     boolean not null default true,
    creado     timestamptz not null default now(),
    ultimo_uso timestamptz
);

-- 2) Ventas ya subidas (para no duplicar: la PC manda siempre el mismo uuid)
create table if not exists public.ventas_offline (
    uuid        uuid primary key,
    kiosco_id   bigint not null references public.kioscos(id) on delete cascade,
    movimiento  bigint not null,
    hora_venta  timestamptz,
    subido      timestamptz not null default now()
);
create index if not exists ix_ventas_offline_kiosco on public.ventas_offline (kiosco_id, subido);

-- solo se tocan desde las funciones de abajo
alter table public.equipos_offline enable row level security;
alter table public.ventas_offline enable row level security;
revoke all on public.equipos_offline from anon, authenticated;
revoke all on public.ventas_offline from anon, authenticated;

-- 3) Darle una llave a esta PC para este negocio (se llama al entrar con internet)
create or replace function public.registrar_equipo(p_kiosco bigint)
returns text
language plpgsql
security definer
set search_path = public
as $$
declare
    v_mio   bigint;
    v_token text;
begin
    -- con la seguridad por negocio (15): solo para el negocio al que entró este equipo
    if to_regprocedure('public.mi_kiosco()') is not null then
        execute 'select public.mi_kiosco()' into v_mio;
        if v_mio is null or v_mio <> p_kiosco then
            raise exception 'Este equipo no entró a ese negocio';
        end if;
    end if;
    if not exists (select 1 from kioscos where id = p_kiosco) then
        raise exception 'Negocio inexistente';
    end if;

    v_token := replace(gen_random_uuid()::text, '-', '') || replace(gen_random_uuid()::text, '-', '');
    insert into equipos_offline (token_hash, kiosco_id)
    values (encode(sha256(convert_to(v_token, 'UTF8')), 'hex'), p_kiosco);
    return v_token;
end $$;

-- 4) Subir una venta hecha sin internet (todo junto o nada; si ya se subió, no la repite)
--    p_venta = { uuid, kiosco_id, usuario_id, fecha, hora,
--                pagos:  [{forma_pago_id, monto}],
--                items:  [{producto_id, nombre, cantidad, precio_unitario, subtotal}],
--                varios: [{nombre, cantidad, precio_unitario, subtotal}] }
create or replace function public.subir_venta_offline(p_token text, p_venta jsonb)
returns bigint
language plpgsql
security definer
set search_path = public
as $$
declare
    v_k       bigint;
    v_uuid    uuid;
    v_mov     bigint;
    v_fecha   timestamptz;
    v_hora    timestamptz;
    v_usr     uuid;
    v_cant    integer;
    v_kdet    boolean;
    p         jsonb;
    it        jsonb;
    v_prod    record;
begin
    select kiosco_id into v_k
    from equipos_offline
    where token_hash = encode(sha256(convert_to(coalesce(p_token, ''), 'UTF8')), 'hex') and activo;
    if v_k is null then
        raise exception 'Equipo no autorizado para subir ventas' using errcode = '28000';
    end if;
    if (p_venta->>'kiosco_id')::bigint is distinct from v_k then
        raise exception 'La venta es de otro negocio';
    end if;

    v_uuid := (p_venta->>'uuid')::uuid;
    perform pg_advisory_xact_lock(hashtext(v_uuid::text));   -- si llegan dos a la vez, una espera a la otra
    select movimiento into v_mov from ventas_offline where uuid = v_uuid;
    if found then
        return v_mov;                                         -- ya estaba subida: no se duplica
    end if;

    v_fecha := coalesce((p_venta->>'fecha')::timestamptz, now());
    v_hora  := coalesce((p_venta->>'hora')::timestamptz, now());
    v_usr   := nullif(p_venta->>'usuario_id', '')::uuid;
    if v_usr is not null and not exists (select 1 from usuarios where id = v_usr and id_kiosco = v_k) then
        v_usr := null;
    end if;
    v_mov := nextval('secuencia_movimientos_ventas');

    -- cabeceras: una por forma de pago (igual que una venta normal)
    for p in select * from jsonb_array_elements(coalesce(p_venta->'pagos', '[]'::jsonb)) loop
        if not exists (select 1 from formas_pago where id = (p->>'forma_pago_id')::bigint and kiosco_id = v_k) then
            raise exception 'Forma de pago inválida (%)', p->>'forma_pago_id';
        end if;
        insert into ventas (fecha, total, usuario_operador_id, forma_pago_id, kiosco_id, persona_id,
                            numero_unico_identificatorio_movimiento, pagado, hora_venta)
        values (v_fecha, (p->>'monto')::numeric, v_usr, (p->>'forma_pago_id')::bigint, v_k, null,
                v_mov, true, v_hora);
    end loop;

    -- productos (y stock)
    v_kdet := exists (select 1 from information_schema.columns
                      where table_schema = 'public' and table_name = 'venta_detalles' and column_name = 'kiosco_id');
    for it in select * from jsonb_array_elements(coalesce(p_venta->'items', '[]'::jsonb)) loop
        v_cant := (it->>'cantidad')::integer;
        select id, es_combo, limite_unidades_combo into v_prod
        from productos where id = (it->>'producto_id')::bigint and kiosco_id = v_k;

        if not found then
            -- el producto se borró mientras tanto: queda como "varios" para no perder la venta
            insert into productos_varios_ventas (kiosco_id, numero_unico_identificatorio_movimiento, nombre_producto,
                                                 preciounitario, cantidad, subtotal, created_at)
            values (v_k, v_mov, coalesce(nullif(it->>'nombre', ''), 'PRODUCTO'), (it->>'precio_unitario')::numeric,
                    v_cant, (it->>'subtotal')::numeric, v_hora);
            continue;
        end if;

        if v_kdet then
            insert into venta_detalles (producto_id, cantidad, precio_unitario, subtotal,
                                        numero_unico_identificatorio_movimiento, created_at, kiosco_id)
            values (v_prod.id, v_cant, (it->>'precio_unitario')::numeric, (it->>'subtotal')::numeric, v_mov, v_hora, v_k);
        else
            insert into venta_detalles (producto_id, cantidad, precio_unitario, subtotal,
                                        numero_unico_identificatorio_movimiento, created_at)
            values (v_prod.id, v_cant, (it->>'precio_unitario')::numeric, (it->>'subtotal')::numeric, v_mov, v_hora);
        end if;

        if coalesce(v_prod.es_combo, false) then
            -- combo: se descuenta el stock de cada producto que lo compone (sin bajar de 0, como en la venta normal)
            update productos pr
               set stock_actual = greatest(coalesce(pr.stock_actual, 0) - x.cant * v_cant, 0)
              from (select producto_hijo_id, sum(cantidad) as cant
                      from combo_detalle where combo_id = v_prod.id group by producto_hijo_id) x
             where pr.id = x.producto_hijo_id and pr.kiosco_id = v_k;
            if v_prod.limite_unidades_combo is not null then
                update productos set limite_unidades_combo = greatest(limite_unidades_combo - v_cant, 0)
                 where id = v_prod.id;
            end if;
        else
            update productos set stock_actual = coalesce(stock_actual, 0) - v_cant where id = v_prod.id;
        end if;
    end loop;

    -- productos sin código ("varios")
    for it in select * from jsonb_array_elements(coalesce(p_venta->'varios', '[]'::jsonb)) loop
        insert into productos_varios_ventas (kiosco_id, numero_unico_identificatorio_movimiento, nombre_producto,
                                             preciounitario, cantidad, subtotal, created_at)
        values (v_k, v_mov, coalesce(nullif(it->>'nombre', ''), 'VARIOS'), (it->>'precio_unitario')::numeric,
                (it->>'cantidad')::integer, (it->>'subtotal')::numeric, v_hora);
    end loop;

    insert into ventas_offline (uuid, kiosco_id, movimiento, hora_venta) values (v_uuid, v_k, v_mov, v_hora);
    update equipos_offline set ultimo_uso = now()
     where token_hash = encode(sha256(convert_to(p_token, 'UTF8')), 'hex');
    return v_mov;
end $$;

-- 5) Permisos: subir se puede sin estar logueado (la llave del equipo es la que autoriza)
revoke all on function public.registrar_equipo(bigint) from public;
revoke all on function public.subir_venta_offline(text, jsonb) from public;
grant execute on function public.subir_venta_offline(text, jsonb) to anon, authenticated;
do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        grant execute on function public.registrar_equipo(bigint) to authenticated;
    else
        grant execute on function public.registrar_equipo(bigint) to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Ventas sin internet: listo' as resultado;
