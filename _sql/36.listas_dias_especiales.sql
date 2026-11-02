-- =====================================================================
-- DÍAS CON OTRO PRECIO (domingos, feriados...)
-- Una lista (ej: Lista 3 "PRECIO FERIADO") se usa TODO EL DÍA en las fechas marcadas en el almanaque
-- y/o en los días de la semana elegidos (ej: todos los domingos). Manda sobre el horario (noche).
-- El precio sale del precio de venta + un monto fijo o un % (tramos de listas_recargos, script 33).
-- Se configura en Configuración → Listas de precios.
-- Correr en TEST y en PRODUCCIÓN (después del 33). Se puede correr más de una vez.
-- =====================================================================

create table if not exists public.listas_dias (
    id               bigserial primary key,
    kiosco_id        bigint not null references public.kioscos(id) on delete cascade,
    lista_precio_id  bigint not null references public.listas_precios(id) on delete cascade,
    fecha            date,                                              -- un día puntual (ej: 25/12/2026)
    dia_semana       smallint check (dia_semana between 0 and 6),       -- todas las semanas (0 = domingo ... 6 = sábado)
    creado           timestamptz not null default now(),
    check (fecha is not null or dia_semana is not null)
);
create index if not exists ix_listas_dias_kiosco on public.listas_dias (kiosco_id);
create index if not exists ix_listas_dias_lista on public.listas_dias (lista_precio_id);

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        alter table public.listas_dias alter column kiosco_id set default public.mi_kiosco();
        alter table public.listas_dias enable row level security;
        drop policy if exists aislado_por_negocio on public.listas_dias;
        create policy aislado_por_negocio on public.listas_dias for all to authenticated
            using (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()))
            with check (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
        revoke all on public.listas_dias from anon;
        grant select, insert, update, delete on public.listas_dias to authenticated;
        grant usage, select on sequence public.listas_dias_id_seq to authenticated;
    else
        grant select, insert, update, delete on public.listas_dias to anon, authenticated;
        grant usage, select on sequence public.listas_dias_id_seq to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'ok: días con otro precio (domingos y feriados)' as resultado;
