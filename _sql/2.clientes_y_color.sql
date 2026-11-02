-- =====================================================================
-- ABM de Clientes + color del negocio
-- Correr en Supabase > SQL Editor ANTES de probar la app.
-- =====================================================================

-- Clientes / empleados (tabla personas)
alter table personas add column if not exists telefono    text;
alter table personas add column if not exists es_empleado boolean not null default false;
alter table personas add column if not exists nota        text;

-- Color elegido por el dueño (azul, celeste, turquesa, verde, rosa, fucsia, violeta, naranja, rojo, grafito)
alter table kioscos add column if not exists color_tema text not null default 'azul';
