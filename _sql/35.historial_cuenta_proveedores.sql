-- =====================================================================
-- HISTORIAL DE LA CUENTA CON PROVEEDORES (para que no haya confusiones)
--   * cada movimiento guarda QUIÉN lo cargó (usuario_nombre) y CUÁNDO (creado, ya existía)
--   * los movimientos ya no se borran: se ANULAN (quedan a la vista, tachados, con quién y cuándo)
--   * el saldo no cuenta los anulados
-- Correr en TEST y en PRODUCCIÓN (después del 17). Se puede correr más de una vez.
-- =====================================================================

alter table public.proveedores_cuenta add column if not exists usuario_nombre text;
alter table public.proveedores_cuenta add column if not exists anulado        boolean not null default false;
alter table public.proveedores_cuenta add column if not exists anulado_por    text;
alter table public.proveedores_cuenta add column if not exists anulado_el     timestamptz;
alter table public.proveedores_cuenta add column if not exists anulado_motivo text;

-- saldo por proveedor, sin los anulados
create or replace view public.vw_saldo_proveedores as
select c.kiosco_id,
       c.proveedor_id,
       coalesce(sum(c.monto) filter (where c.tipo = 'deuda'), 0) as total_deuda,
       coalesce(sum(c.monto) filter (where c.tipo = 'pago'), 0)  as total_pagado,
       coalesce(sum(case when c.tipo = 'deuda' then c.monto else -c.monto end), 0) as saldo,
       max(c.fecha) as ultimo_movimiento
from public.proveedores_cuenta c
where not c.anulado
group by c.kiosco_id, c.proveedor_id;

alter view public.vw_saldo_proveedores set (security_invoker = true);

notify pgrst, 'reload schema';
select 'ok: la cuenta con proveedores guarda quién cargó y anula en vez de borrar' as resultado;
