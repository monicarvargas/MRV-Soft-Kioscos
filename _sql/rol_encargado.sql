-- =====================================================================
-- ROL "ENCARGADO" (todo menos recaudación y usuarios)
-- Correr en Supabase > SQL Editor. Se puede correr más de una vez.
-- Solo hace falta si al guardar un usuario como "Encargado/a" da un error de la base
-- (por ejemplo "violates check constraint" o "invalid input value for enum").
-- =====================================================================
do $$
declare
    r record;
    v_tipo text;
begin
    -- 1) Si la columna rol es un ENUM, se le agrega el valor 'encargado'
    select t.typname into v_tipo
    from pg_attribute a
    join pg_type t on t.oid = a.atttypid
    where a.attrelid = 'public.usuarios'::regclass and a.attname = 'rol' and t.typtype = 'e';

    if v_tipo is not null then
        execute format('alter type %I add value if not exists %L', v_tipo, 'encargado');
        raise notice 'Se agregó "encargado" al tipo %', v_tipo;
    end if;

    -- 2) Si hay una regla (CHECK) que limita los roles, se quita (no dejaba guardar 'encargado')
    for r in
        select conname from pg_constraint
        where conrelid = 'public.usuarios'::regclass and contype = 'c'
          and pg_get_constraintdef(oid) ilike '%rol%'
    loop
        execute format('alter table public.usuarios drop constraint %I', r.conname);
        raise notice 'Se quitó la regla vieja % (los roles los controla la aplicación)', r.conname;
    end loop;
end $$;
