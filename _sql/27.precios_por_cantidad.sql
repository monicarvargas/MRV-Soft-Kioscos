-- =====================================================================
-- PRECIOS POR CANTIDAD (promos por pack): ej. gomitas c/u $30 · 10 u x $250 · 23 u x $500.
-- Cada fila es "llevando CANTIDAD unidades, pagan PRECIO en total".
-- Al vender se arma la combinación más barata para el cliente (ej: 35 u = 23 u + 10 u + 2 sueltas).
-- El stock baja por unidades, como siempre.
-- Correr en TEST y en PRODUCCIÓN (después del 15). Se puede correr más de una vez.
-- =====================================================================

create table if not exists public.precios_cantidad (
    id           bigserial primary key,
    kiosco_id    bigint not null references public.kioscos(id) on delete cascade,
    producto_id  bigint not null references public.productos(id) on delete cascade,
    cantidad     integer not null check (cantidad > 1),
    precio       numeric(14,2) not null check (precio > 0),
    creado       timestamptz not null default now()
);
create unique index if not exists ux_precios_cantidad on public.precios_cantidad (producto_id, cantidad);
create index if not exists ix_precios_cantidad_kiosco on public.precios_cantidad (kiosco_id);

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        alter table public.precios_cantidad alter column kiosco_id set default public.mi_kiosco();
        alter table public.precios_cantidad enable row level security;
        drop policy if exists aislado_por_negocio on public.precios_cantidad;
        create policy aislado_por_negocio on public.precios_cantidad for all to authenticated
            using (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()))
            with check (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
        revoke all on public.precios_cantidad from anon;
        grant select, insert, update, delete on public.precios_cantidad to authenticated;
        grant usage, select on sequence public.precios_cantidad_id_seq to authenticated;
    else
        grant select, insert, update, delete on public.precios_cantidad to anon, authenticated;
        grant usage, select on sequence public.precios_cantidad_id_seq to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Precios por cantidad: listo' as resultado;
