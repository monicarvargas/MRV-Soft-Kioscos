-- =====================================================================
-- PROVEEDORES: dar de baja (activo = false)
-- Un proveedor con deuda (o ya usado en productos/pedidos/compras) no se borra:
-- se da de baja y deja de aparecer para elegir en todo el sistema.
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

alter table public.proveedores
    add column if not exists activo boolean not null default true;

notify pgrst, 'reload schema';
select 'Columna activo en proveedores lista' as resultado;
