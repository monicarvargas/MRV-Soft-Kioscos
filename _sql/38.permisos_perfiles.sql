-- =====================================================================
-- 38. PERMISOS DE CADA PERFIL (Configuración → Ayuda y plan → Perfiles)
-- El dueño elige qué puede hacer el encargado y el vendedor (ej: que el encargado vea la recaudación del día).
-- Se guarda en kioscos.permisos_roles como { "encargado": ["mercaderia", ...], "vendedor": [...] }.
-- null = los permisos de fábrica (ver Services\Roles.cs).
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

alter table public.kioscos add column if not exists permisos_roles jsonb;

notify pgrst, 'reload schema';
select 'Columna permisos_roles en kioscos lista' as resultado;
