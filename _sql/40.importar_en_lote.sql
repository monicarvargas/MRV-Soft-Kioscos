-- =====================================================================
-- 40. IMPORTAR EXCEL GASTANDO MUCHO MENOS DISCO
-- Antes el importador actualizaba los productos UNO POR UNO (miles de grabaciones seguidas):
-- en el plan gratis de Supabase eso agota el "presupuesto de disco" (Disk IO) y la base se cuelga.
-- Con esta función el importador manda los productos DE A 200 y se graban de una sola vez.
--
-- Seguridad: la función corre con los permisos de quien la llama (security invoker), así que
-- las reglas de cada negocio (script 15) se siguen cumpliendo: solo se cambian productos del negocio propio.
-- Lo que no viene en cada producto queda como estaba. El negocio (kiosco_id) nunca se cambia.
--
-- Si este script no se corrió, el importador igual funciona (guarda uno por uno, como antes).
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- =====================================================================

create or replace function public.importar_actualizar_productos(p_filas jsonb)
returns jsonb
language plpgsql volatile security invoker set search_path = public as $$
declare
    e   jsonb;
    a   public.productos;     -- como está ahora
    n   public.productos;     -- como queda (lo que vino pisa; lo que no vino, queda)
    ids bigint[] := '{}';
begin
    if jsonb_typeof(p_filas) <> 'array' then
        raise exception 'Se esperaba una lista de productos';
    end if;
    if jsonb_array_length(p_filas) > 500 then
        raise exception 'Máximo 500 productos por vez';
    end if;

    for e in select value from jsonb_array_elements(p_filas) loop
        -- (con las reglas del negocio: si el producto es de otro negocio, no se encuentra)
        select * into a from public.productos where id = (e->>'id')::bigint;
        if not found then continue; end if;

        n := jsonb_populate_record(a, e - 'id' - 'kiosco_id');

        update public.productos set (
            familia_id,
            proveedor_id,
            codigo_barras,
            nombre,
            marca,
            precio_costo,
            precio_venta,
            utilidad_pv,
            precio_lista2,
            utilidad_lista2,
            precio_lista3,
            utilidad_lista3,
            precio_lista4,
            utilidad_lista4,
            precio_lista5,
            utilidad_lista5,
            precio_venta_por_mayor,
            utilidad_x_mayor,
            cant_por_mayor,
            venta_por_peso,
            unidad,
            stock_actual,
            stock_ideal,
            stock_minimo,
            nota_producto,
            imagen_url,
            fecha_actualizacion,
            activo,
            es_combo,
            fecha_vigencia_desde_combo,
            fecha_vigencia_hasta_combo,
            limite_unidades_combo,
            categoria_venta_rapida_id,
            mostrar_en_venta_rapida,
            orden_venta_rapida,
            admite_importe,
            sin_redondeo,
            subfamilia_id,
            bulto_precio,
            bulto_unidades,
            bulto_fecha
        ) = (
            n.familia_id,
            n.proveedor_id,
            n.codigo_barras,
            n.nombre,
            n.marca,
            n.precio_costo,
            n.precio_venta,
            n.utilidad_pv,
            n.precio_lista2,
            n.utilidad_lista2,
            n.precio_lista3,
            n.utilidad_lista3,
            n.precio_lista4,
            n.utilidad_lista4,
            n.precio_lista5,
            n.utilidad_lista5,
            n.precio_venta_por_mayor,
            n.utilidad_x_mayor,
            n.cant_por_mayor,
            n.venta_por_peso,
            n.unidad,
            n.stock_actual,
            n.stock_ideal,
            n.stock_minimo,
            n.nota_producto,
            n.imagen_url,
            n.fecha_actualizacion,
            n.activo,
            n.es_combo,
            n.fecha_vigencia_desde_combo,
            n.fecha_vigencia_hasta_combo,
            n.limite_unidades_combo,
            n.categoria_venta_rapida_id,
            n.mostrar_en_venta_rapida,
            n.orden_venta_rapida,
            n.admite_importe,
            n.sin_redondeo,
            n.subfamilia_id,
            n.bulto_precio,
            n.bulto_unidades,
            n.bulto_fecha
        )
        where id = a.id;

        ids := ids || a.id;
    end loop;

    return to_jsonb(ids);   -- los que se actualizaron
end $$;

revoke all on function public.importar_actualizar_productos(jsonb) from public, anon;
grant execute on function public.importar_actualizar_productos(jsonb) to authenticated, service_role;

notify pgrst, 'reload schema';
select 'Listo: el importador ya puede guardar de a 200 productos' as resultado;
