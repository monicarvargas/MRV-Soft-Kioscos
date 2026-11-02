-- Índice para que el importador de Excel encuentre rápido los códigos de cada proveedor
-- Correr en Supabase > SQL Editor (test y producción). Se puede correr más de una vez.
create index if not exists ix_prod_prov_kiosco_proveedor on productos_proveedores (id_kiosco, id_proveedor);
analyze productos_proveedores;
