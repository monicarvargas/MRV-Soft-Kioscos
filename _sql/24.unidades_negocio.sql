-- =====================================================================
-- UNIDADES EN QUE VENDE CADA NEGOCIO (Configuración → Unidades)
-- null = las de siempre: Unidad, Kilo y Gramos.
-- Se pueden sumar Litros, Metros y Bultos, o sacar Kilo / Gramos. "Unidad" está siempre.
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

alter table public.kioscos add column if not exists unidades text[];

notify pgrst, 'reload schema';
select 'Columna unidades en kioscos lista' as resultado;
