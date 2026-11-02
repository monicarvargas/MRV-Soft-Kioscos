-- =====================================================================
-- Rapidez: índices + marcas de stock para la página de Productos
-- Correr TODO junto en Supabase > SQL Editor. Se puede correr más de una vez.
-- =====================================================================

-- 1) Búsqueda rápida por "parte del nombre" o "parte del código" (ILIKE '%texto%')
create extension if not exists pg_trgm;

create index if not exists ix_productos_nombre_trgm  on productos using gin (nombre gin_trgm_ops);
create index if not exists ix_productos_codigo_trgm  on productos using gin (codigo_barras gin_trgm_ops);
create index if not exists ix_productos_kiosco_nombre on productos (kiosco_id, activo, nombre);
create index if not exists ix_productos_kiosco_codigo on productos (kiosco_id, codigo_barras);
create index if not exists ix_productos_kiosco_familia on productos (kiosco_id, familia_id);

-- 2) Marcas calculadas solas por la base (no hay que tocarlas nunca desde la app)
--    sin_stock  = producto normal (no combo) con stock 0 o menos
--    stock_bajo = producto normal con stock pero igual o por debajo del mínimo
alter table productos add column if not exists sin_stock boolean
    generated always as (coalesce(es_combo, false) = false and coalesce(stock_actual, 0) <= 0) stored;

alter table productos add column if not exists stock_bajo boolean
    generated always as (coalesce(es_combo, false) = false
                         and coalesce(stock_minimo, 0) > 0
                         and coalesce(stock_actual, 0) > 0
                         and stock_actual <= stock_minimo) stored;

create index if not exists ix_productos_kiosco_sin_stock  on productos (kiosco_id) where sin_stock and activo;
create index if not exists ix_productos_kiosco_stock_bajo on productos (kiosco_id) where stock_bajo and activo;

-- 3) Otros índices que aceleran ventas, fiados, reportes y proveedores
create index if not exists ix_ventas_kiosco_fecha      on ventas (kiosco_id, fecha);
create index if not exists ix_ventas_movimiento        on ventas (numero_unico_identificatorio_movimiento);
create index if not exists ix_ventas_persona_pendiente on ventas (persona_id) where pagado = false;
create index if not exists ix_venta_detalles_mov       on venta_detalles (numero_unico_identificatorio_movimiento);
create index if not exists ix_venta_detalles_producto  on venta_detalles (producto_id);
create index if not exists ix_varios_ventas_mov        on productos_varios_ventas (numero_unico_identificatorio_movimiento);
create index if not exists ix_combo_detalle_combo      on combo_detalle (combo_id);
create index if not exists ix_prod_prov_producto       on productos_proveedores (id_producto);
create index if not exists ix_prod_prov_proveedor      on productos_proveedores (id_proveedor);
create index if not exists ix_personas_kiosco          on personas (kiosco_id);

analyze productos;
analyze ventas;
analyze venta_detalles;
