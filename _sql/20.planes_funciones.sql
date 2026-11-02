-- =====================================================================
-- PLANES (Básico / Medio / Avanzado) Y FUNCIONES HABILITADAS POR NEGOCIO
--   * planes: qué funciones trae cada plan (lo edita el superadmin desde la app)
--   * kioscos.plan_codigo: el plan del negocio
--   * kioscos.funciones_extra / funciones_quitadas: excepciones de ese negocio
-- Funciones que siempre están (no dependen del plan): Vender, Productos, Nuevo producto, Configuración.
-- Correr en TEST y en PRODUCCIÓN (después del 15). Se puede correr más de una vez.
-- =====================================================================

-- 1) Planes
create table if not exists public.planes (
    codigo      text primary key,                 -- basico | medio | avanzado (o los que agregues)
    nombre      text not null,
    orden       integer not null default 0,
    precio      numeric(12,2),                    -- referencia para vos (opcional)
    funciones   text[] not null default '{}',
    actualizado timestamptz not null default now()
);

insert into public.planes (codigo, nombre, orden, funciones) values
 ('basico',   'Básico',   1, array['carteles','importar_excel','reporte_dia']),
 ('medio',    'Medio',    2, array['carteles','importar_excel','reporte_dia',
                               'precios_grupo','venta_rapida','fiado','reportes_productos','compras_mayorista']),
 ('avanzado', 'Avanzado', 3, array['carteles','importar_excel','reporte_dia',
                               'precios_grupo','venta_rapida','fiado','reportes_productos','compras_mayorista',
                               'pedidos_proveedores','deudas_proveedores','asistencia','encargado'])
on conflict (codigo) do nothing;   -- si ya existen, NO se pisan tus cambios

-- 2) Plan y excepciones de cada negocio
alter table public.kioscos add column if not exists plan_codigo text;
alter table public.kioscos add column if not exists funciones_extra    text[] not null default '{}';
alter table public.kioscos add column if not exists funciones_quitadas text[] not null default '{}';
update public.kioscos set plan_codigo = 'avanzado' where plan_codigo is null;   -- los que ya están: no pierden nada
alter table public.kioscos alter column plan_codigo set default 'basico';       -- los nuevos arrancan en Básico
do $$
begin
    if not exists (select 1 from pg_constraint where conname = 'fk_kioscos_plan') then
        alter table public.kioscos add constraint fk_kioscos_plan
            foreign key (plan_codigo) references public.planes(codigo) on update cascade;
    end if;
end $$;

-- 3) Funciones que tiene un negocio = las del plan + extra - quitadas
create or replace function public.funciones_negocio(p_kiosco bigint)
returns text[]
language sql
stable
set search_path = public
as $$
    select coalesce(array(
        select distinct f
        from kioscos k
        left join planes pl on pl.codigo = k.plan_codigo
        cross join lateral unnest(coalesce(pl.funciones, '{}') || coalesce(k.funciones_extra, '{}')) as f
        where k.id = p_kiosco
          and not (f = any (coalesce(k.funciones_quitadas, '{}')))
        order by f), '{}')
$$;

-- 4) Seguridad
alter table public.planes enable row level security;
drop policy if exists planes_ver on public.planes;
drop policy if exists planes_cambio on public.planes;
create policy planes_ver on public.planes for select to authenticated using (true);

do $$
begin
    if to_regprocedure('public.soy_superadmin()') is not null then
        create policy planes_cambio on public.planes for all to authenticated
            using ((select public.soy_superadmin())) with check ((select public.soy_superadmin()));
        revoke all on public.planes from anon;
        grant select, insert, update, delete on public.planes to authenticated;
        revoke all on function public.funciones_negocio(bigint) from public, anon;
        grant execute on function public.funciones_negocio(bigint) to authenticated;
    else
        create policy planes_cambio on public.planes for all to anon, authenticated using (true) with check (true);
        grant select, insert, update, delete on public.planes to anon, authenticated;
        grant execute on function public.funciones_negocio(bigint) to anon, authenticated;
    end if;
end $$;

-- El dueño puede cambiar datos de SU negocio (color, redondeo...), pero NO su plan,
-- sus funciones, el estado ni el vencimiento del abono: eso solo el superadmin.
create or replace function public.tg_kioscos_solo_superadmin()
returns trigger
language plpgsql
set search_path = public
as $$
declare
    es_super boolean := true;
begin
    if auth.uid() is not null and to_regprocedure('public.soy_superadmin()') is not null then
        execute 'select public.soy_superadmin()' into es_super;
    end if;
    if not es_super and (
           new.plan_codigo             is distinct from old.plan_codigo
        or new.funciones_extra         is distinct from old.funciones_extra
        or new.funciones_quitadas      is distinct from old.funciones_quitadas
        or new.estado                  is distinct from old.estado
        or new.fecha_vencimiento_abono is distinct from old.fecha_vencimiento_abono) then
        raise exception 'Solo el superadmin puede cambiar el plan, las funciones, el estado o el vencimiento del negocio';
    end if;
    return new;
end $$;

drop trigger if exists trg_kioscos_solo_superadmin on public.kioscos;
create trigger trg_kioscos_solo_superadmin before update on public.kioscos
    for each row execute function public.tg_kioscos_solo_superadmin();

notify pgrst, 'reload schema';
select codigo, nombre, array_length(funciones, 1) as cant_funciones from public.planes order by orden;
