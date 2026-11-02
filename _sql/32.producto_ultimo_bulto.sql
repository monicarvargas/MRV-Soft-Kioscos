-- =====================================================================
-- ÚLTIMO BULTO CARGADO EN EL PRODUCTO
-- En Nuevo / Editar producto → "Lo compro por bulto": se guarda el precio del bulto,
-- las unidades que trae y la fecha. Al volver a abrir el producto se ven los últimos
-- valores cargados, para saber si el bulto aumentó o no.
--
-- Correr en TEST y en PRODUCCIÓN (después del 15). Se puede correr más de una vez.
-- =====================================================================

alter table public.productos add column if not exists bulto_precio   numeric(14,2);
alter table public.productos add column if not exists bulto_unidades integer check (bulto_unidades is null or bulto_unidades > 0);
alter table public.productos add column if not exists bulto_fecha    timestamptz;

notify pgrst, 'reload schema';

select 'ok: productos ahora guardan el último bulto cargado' as resultado;
