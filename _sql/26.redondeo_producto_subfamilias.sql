-- =====================================================================
-- 1) PRODUCTOS SIN REDONDEO: cosas chicas que se venden sueltas (gomitas a $30, caramelos, clavos...).
--    Por defecto todos los productos siguen el redondeo del negocio (Configuración → Redondeo).
--    Si un producto tiene "sin_redondeo" = true, sus precios quedan exactos (ej: $30).
--
-- 2) SUBFAMILIAS: dentro de una familia (ej: GOLOSINAS) se puede separar en ALFAJORES, CHICLES, GOMITAS...
--    Es opcional: un producto puede tener familia sola, o familia + subfamilia.
--
-- Correr en TEST y en PRODUCCIÓN (después del 15). Se puede correr más de una vez.
-- =====================================================================

-- 1) Redondeo por producto
alter table public.productos add column if not exists sin_redondeo boolean not null default false;

-- 2) Subfamilias
create table if not exists public.subfamilias (
    id          bigserial primary key,
    kiosco_id   bigint not null references public.kioscos(id) on delete cascade,
    familia_id  bigint not null references public.familias(id) on delete cascade,
    nombre      text not null check (length(trim(nombre)) > 0),
    creado      timestamptz not null default now()
);
create unique index if not exists ux_subfamilias_nombre on public.subfamilias (kiosco_id, familia_id, upper(trim(nombre)));
create index if not exists ix_subfamilias_familia on public.subfamilias (familia_id);

alter table public.productos add column if not exists subfamilia_id bigint references public.subfamilias(id) on delete set null;
create index if not exists ix_productos_subfamilia on public.productos (subfamilia_id);

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        alter table public.subfamilias alter column kiosco_id set default public.mi_kiosco();
        alter table public.subfamilias enable row level security;
        drop policy if exists aislado_por_negocio on public.subfamilias;
        create policy aislado_por_negocio on public.subfamilias for all to authenticated
            using (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()))
            with check (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
        revoke all on public.subfamilias from anon;
        grant select, insert, update, delete on public.subfamilias to authenticated;
        grant usage, select on sequence public.subfamilias_id_seq to authenticated;
    else
        grant select, insert, update, delete on public.subfamilias to anon, authenticated;
        grant usage, select on sequence public.subfamilias_id_seq to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Redondeo por producto y subfamilias: listo' as resultado;
