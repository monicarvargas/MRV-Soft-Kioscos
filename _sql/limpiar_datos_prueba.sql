-- =====================================================================
-- LIMPIAR DATOS DE PRUEBA  (Supabase > SQL Editor)
--
-- CÓMO USARLO
--   1) Corrélo así como está: NO BORRA NADA, solo muestra cuántas filas tiene cada tabla
--      (mirá la pestaña "Results").
--   2) Si estás de acuerdo, cambiá  v_ejecutar := false  por  true  y corrélo de nuevo.
--
-- ¡OJO! Lo borrado NO se puede recuperar. Si querés una copia antes, en Supabase:
--       Database > Backups (o exportá las tablas a CSV desde el Table Editor).
--
-- QUÉ SE BORRA (para TODOS los negocios)
--   * Siempre (movimientos): ventas, sus detalles y pagos, fiados y cobros, varios vendidos,
--     compras al mayorista, pedidos a proveedores, importaciones de Excel y asistencias.
--   * Si v_borrar_catalogo = true: también productos, combos, venta rápida,
--     proveedores de cada producto y clientes/empleados (personas).
--
-- QUÉ NO SE TOCA NUNCA
--   negocios (kioscos), usuarios, formas de pago, listas de precios, familias,
--   proveedores, turnos y horarios de venta.
-- =====================================================================

drop table if exists _limpieza;
create temp table _limpieza (tabla text, filas bigint, accion text);

do $$
declare
    -- ======== OPCIONES (cambiar acá) ========
    v_ejecutar        boolean := false;  -- false = solo mostrar; true = BORRAR
    v_borrar_catalogo boolean := false;  -- true = borra también productos, combos y clientes
    v_stock_a_cero    boolean := true;   -- si se dejan los productos: stock en 0 (las ventas borradas lo habían descontado)
    -- ========================================

    movimientos text[] := array[
        'venta_pagos', 'venta_detalles', 'productos_varios_ventas', 'ventas',
        'compra_mayorista_detalles', 'compras_mayoristas',
        'lista_pedido_detalles', 'lista_pedidos',
        'importaciones_excel', 'asistencias'
    ];
    catalogo text[] := array[
        'combo_detalle', 'categorias_venta_rapida', 'productos_proveedores', 'productos', 'personas'
    ];
    lista text[];
    t text;
    n bigint;
begin
    lista := movimientos;
    if v_borrar_catalogo then lista := lista || catalogo; end if;

    raise notice '---------------------------------------------';
    raise notice '%', case when v_ejecutar then 'BORRANDO...' else 'SOLO VISTA PREVIA (no se borra nada)' end;
    raise notice '---------------------------------------------';

    foreach t in array lista loop
        if to_regclass('public.' || t) is null then
            insert into _limpieza values (t, null, 'no existe (se saltea)');
            continue;
        end if;
        execute format('select count(*) from public.%I', t) into n;
        insert into _limpieza values (t, n, case when v_ejecutar then 'BORRADA' else 'se borraría' end);
    end loop;

    if v_ejecutar then
        foreach t in array lista loop
            if to_regclass('public.' || t) is not null then
                -- RESTART IDENTITY: los id vuelven a empezar en 1
                -- CASCADE: borra también filas de otras tablas que apunten a esta (aparecen como NOTICE)
                execute format('truncate table public.%I restart identity cascade', t);
            end if;
        end loop;

        if not v_borrar_catalogo and v_stock_a_cero and to_regclass('public.productos') is not null then
            begin
                execute 'update public.productos set stock_actual = 0';
                insert into _limpieza values ('productos', null, 'stock puesto en 0');
            exception when undefined_column then
                insert into _limpieza values ('productos', null, 'no se encontró la columna stock_actual: stock sin cambios');
            end;
        end if;
    end if;
end $$;

select tabla, filas, accion from _limpieza order by accion, tabla;
