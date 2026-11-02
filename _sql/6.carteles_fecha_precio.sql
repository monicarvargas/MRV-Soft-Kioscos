-- =====================================================================
-- Carteles de precios: que la "fecha de actualización" cambie sola
-- cada vez que cambia un precio (también al importar Excel o recibir mercadería).
-- Correr en Supabase > SQL Editor. Se puede correr más de una vez.
-- =====================================================================
create or replace function public.tg_productos_fecha_precio() returns trigger
language plpgsql as $$
begin
    if new.precio_venta is distinct from old.precio_venta
       or new.precio_lista2 is distinct from old.precio_lista2 then
        new.fecha_actualizacion := now();
    end if;
    return new;
end $$;

drop trigger if exists trg_productos_fecha_precio on productos;
create trigger trg_productos_fecha_precio
    before update on productos
    for each row execute function public.tg_productos_fecha_precio();

create index if not exists ix_productos_kiosco_fecha_act on productos (kiosco_id, fecha_actualizacion);
