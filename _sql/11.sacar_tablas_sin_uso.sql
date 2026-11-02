-- =====================================================================
-- TABLAS QUE NO USA EL SISTEMA (base de TEST)
-- configuracion_negocio, productos_ruta25, venta_pagos
--
-- PASO A (ahora): se les cambia el nombre a ..._sacar  (no se borra nada).
--   Si algo deja de andar, se vuelven a su nombre con el PASO DESHACER.
-- PASO B (en unas semanas, si todo anda bien): borrarlas de verdad.
-- =====================================================================

-- ---------- PASO A: renombrar ----------
alter table if exists public.configuracion_negocio rename to configuracion_negocio_sacar;
alter table if exists public.productos_ruta25      rename to productos_ruta25_sacar;
alter table if exists public.venta_pagos           rename to venta_pagos_sacar;

-- ---------- PASO DESHACER (solo si hace falta) ----------
-- alter table if exists public.configuracion_negocio_sacar rename to configuracion_negocio;
-- alter table if exists public.productos_ruta25_sacar      rename to productos_ruta25;
-- alter table if exists public.venta_pagos_sacar           rename to venta_pagos;

-- ---------- PASO B: borrar de verdad (más adelante) ----------
-- drop table if exists public.configuracion_negocio_sacar;
-- drop table if exists public.productos_ruta25_sacar;
-- drop table if exists public.venta_pagos_sacar;
