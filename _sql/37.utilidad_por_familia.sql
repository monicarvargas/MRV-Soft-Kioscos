-- =====================================================================
-- % DE UTILIDAD POR FAMILIA Y SUBFAMILIA
-- Ej: FERRETERIA 60% · FIAMBRES 50% · GOLOSINAS 40% · LACTEOS 30%. La subfamilia puede tener la suya (GOMITAS 45%).
-- Se usa al crear productos (a mano o importando un Excel). Se configura en Configuración → Familias.
-- aplicar_utilidad_grupo: pasa esa utilidad a los productos del grupo y recalcula su precio de venta
-- (con el redondeo del negocio; en GRAMOS sobre los 100 g) y, si el negocio tiene recargo de noche, su Lista 2.
-- Correr en TEST y en PRODUCCIÓN (después del 26 y del 33). Se puede correr más de una vez.
-- =====================================================================

alter table public.familias    add column if not exists utilidad numeric(6,2) check (utilidad is null or utilidad >= 0);
alter table public.subfamilias add column if not exists utilidad numeric(6,2) check (utilidad is null or utilidad >= 0);

create or replace function public.aplicar_utilidad_grupo(p_kiosco bigint, p_familia bigint, p_subfamilia bigint, p_utilidad numeric)
returns integer language plpgsql as $$
declare
    v_red   boolean;
    v_paso  numeric;
    v_l2    bigint;
    v_n     integer;
begin
    if p_utilidad is null or p_utilidad < 0 then return 0; end if;

    select coalesce(k.usar_redondeo, true), case when coalesce(k.multiplo_redondeo, 0) > 0 then k.multiplo_redondeo else 50 end
      into v_red, v_paso from public.kioscos k where k.id = p_kiosco;

    select l.id into v_l2 from public.listas_precios l
     where l.kiosco_id = p_kiosco and l.nombre_columna_productos = 'precio_lista2'
       and exists (select 1 from public.listas_recargos r where r.lista_precio_id = l.id)
     order by l.id limit 1;

    with grupo as (
        select p.id, p.precio_costo, p.precio_venta as pv_antes, p.precio_lista2 as l2_antes,
               (upper(trim(coalesce(p.unidad, ''))) in ('GR', 'GRS', 'GRAMOS')) as gr,
               (coalesce(v_red, true) and coalesce(p.sin_redondeo, false) = false) as red
        from public.productos p
        where p.kiosco_id = p_kiosco
          and p.familia_id = p_familia
          and coalesce(p.precio_costo, 0) > 0
          and coalesce(p.es_combo, false) = false
          and (
                (p_subfamilia is not null and p.subfamilia_id = p_subfamilia)
             or (p_subfamilia is null and (p.subfamilia_id is null
                    or not exists (select 1 from public.subfamilias s where s.id = p.subfamilia_id and s.utilidad is not null)))
          )
    ), pv as (
        select g.*,
               case when g.gr then
                        (case when g.red then ceil(g.precio_costo * (1 + p_utilidad / 100) / 10 / v_paso) * v_paso
                              else round(g.precio_costo * (1 + p_utilidad / 100) / 10, 2) end) * 10
                    else
                        (case when g.red then ceil(g.precio_costo * (1 + p_utilidad / 100) / v_paso) * v_paso
                              else round(g.precio_costo * (1 + p_utilidad / 100), 2) end)
               end as pv_nuevo
        from grupo g
    ), l2 as (
        select pv.*,
               case
                   -- mismo precio de día y de noche (gomitas): se sigue igualando
                   when coalesce(pv.l2_antes, 0) > 0 and pv.l2_antes = pv.pv_antes then pv.pv_nuevo
                   -- noche con recargo automático
                   when v_l2 is not null then
                        (case when pv.red then ceil(((case when pv.gr then pv.pv_nuevo / 10 else pv.pv_nuevo end)
                                                    + public.recargo_lista(v_l2, case when pv.gr then pv.pv_nuevo / 10 else pv.pv_nuevo end)) / v_paso) * v_paso
                              else round((case when pv.gr then pv.pv_nuevo / 10 else pv.pv_nuevo end)
                                         + public.recargo_lista(v_l2, case when pv.gr then pv.pv_nuevo / 10 else pv.pv_nuevo end), 2) end)
                        * (case when pv.gr then 10 else 1 end)
                   else pv.l2_antes
               end as l2_nuevo
        from pv
    )
    update public.productos p
       set utilidad_pv = p_utilidad,
           precio_venta = l2.pv_nuevo,
           precio_lista2 = l2.l2_nuevo,
           utilidad_lista2 = case when coalesce(l2.l2_nuevo, 0) > 0 and coalesce(l2.precio_costo, 0) > 0
                                  then round((l2.l2_nuevo - l2.precio_costo) / l2.precio_costo * 100, 2) else p.utilidad_lista2 end,
           fecha_actualizacion = now()
      from l2
     where p.id = l2.id
       and (p.precio_venta is distinct from l2.pv_nuevo or p.utilidad_pv is distinct from p_utilidad);
    get diagnostics v_n = row_count;
    return v_n;
end $$;

do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        revoke all on function public.aplicar_utilidad_grupo(bigint, bigint, bigint, numeric) from anon;
        grant execute on function public.aplicar_utilidad_grupo(bigint, bigint, bigint, numeric) to authenticated;
    else
        grant execute on function public.aplicar_utilidad_grupo(bigint, bigint, bigint, numeric) to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'ok: utilidad por familia y subfamilia' as resultado;
