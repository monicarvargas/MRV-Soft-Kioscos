-- =====================================================================
-- Hora exacta de cada venta (para el Reporte del día)
-- La pone sola la base al guardar la venta. Las ventas viejas quedan sin hora.
-- =====================================================================
alter table ventas add column if not exists hora_venta timestamptz;
alter table ventas alter column hora_venta set default now();
