-- =====================================================================
-- Búsqueda de productos sin importar acentos ni mayúsculas
-- ("cafe", "CAFÉ", "Café" encuentran lo mismo)
-- Correr TODO junto en Supabase > SQL Editor. Se puede correr más de una vez.
-- =====================================================================

create extension if not exists unaccent;
create extension if not exists pg_trgm;

-- unaccent() no se puede usar directo en una columna calculada: se envuelve en una función "fija"
do $$
declare esquema text;
begin
    select n.nspname into esquema
    from pg_extension e join pg_namespace n on n.oid = e.extnamespace
    where e.extname = 'unaccent';

    execute format(
        'create or replace function public.f_sin_acentos(text) returns text
         language sql immutable parallel safe strict
         as $f$ select lower(%I.unaccent(%L::regdictionary, $1)) $f$',
        esquema, esquema || '.unaccent');
end $$;

-- nombre "limpio" (sin acentos y en minúsculas) que calcula sola la base
alter table productos add column if not exists nombre_busqueda text
    generated always as (public.f_sin_acentos(coalesce(nombre, ''))) stored;

create index if not exists ix_productos_nombre_busqueda_trgm
    on productos using gin (nombre_busqueda gin_trgm_ops);

analyze productos;
