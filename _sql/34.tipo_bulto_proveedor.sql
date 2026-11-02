-- =====================================================================
-- "VIENE POR": BULTO, CAJA, PACK, BOLSA, DISPLAY o PAQUETE (antes solo se guardaba cuántas unidades trae)
-- Ej: alfajores, CAJA de 48 → en el pedido 1 = 1 caja.
--   productos_proveedores.tipo_bulto   : cómo lo vende ese proveedor (vacío = suelto / "BULTO")
--   lista_pedido_detalles.tipo_bulto   : queda guardado en cada pedido
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

alter table public.productos_proveedores add column if not exists tipo_bulto text;
alter table public.lista_pedido_detalles add column if not exists tipo_bulto text;

notify pgrst, 'reload schema';
select 'ok: se guarda si viene por bulto, caja, pack...' as resultado;
