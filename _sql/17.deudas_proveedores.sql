-- =====================================================================
-- DEUDAS CON PROVEEDORES (cuenta corriente simple)
-- Se cargan solo totales: "le debo $X" (deuda) y "le entregué $Y" (pago, en efectivo/transferencia...).
-- Saldo = deudas - pagos.
-- Correr en TEST y en PRODUCCIÓN. Se puede correr más de una vez.
-- Funciona con o sin la seguridad por negocio (15); si está, la tabla queda protegida igual que las demás.
-- =====================================================================

create table if not exists public.proveedores_cuenta (
    id           bigserial primary key,
    kiosco_id    bigint not null references public.kioscos(id) on delete cascade,
    proveedor_id bigint not null references public.proveedores(id) on delete cascade,
    fecha        date not null default ((now() at time zone 'America/Argentina/Buenos_Aires')::date),
    tipo         text not null check (tipo in ('deuda', 'pago')),
    monto        numeric(14,2) not null check (monto > 0),
    medio        text,              -- en los pagos: EFECTIVO, TRANSFERENCIA...
    nota         text,
    usuario_id   uuid,
    creado       timestamptz not null default now()
);
create index if not exists ix_prov_cuenta_kiosco_prov on public.proveedores_cuenta (kiosco_id, proveedor_id, fecha);

-- saldo por proveedor
drop view if exists public.vw_saldo_proveedores;
create view public.vw_saldo_proveedores as
select c.kiosco_id,
       c.proveedor_id,
       coalesce(sum(c.monto) filter (where c.tipo = 'deuda'), 0) as total_deuda,
       coalesce(sum(c.monto) filter (where c.tipo = 'pago'), 0)  as total_pagado,
       coalesce(sum(case when c.tipo = 'deuda' then c.monto else -c.monto end), 0) as saldo,
       max(c.fecha) as ultimo_movimiento
from public.proveedores_cuenta c
group by c.kiosco_id, c.proveedor_id;

alter view public.vw_saldo_proveedores set (security_invoker = true);

-- permisos / reglas (igual que el resto de las tablas)
do $$
begin
    if to_regprocedure('public.mi_kiosco()') is not null then
        -- con seguridad por negocio: solo MI negocio (o superadmin)
        alter table public.proveedores_cuenta alter column kiosco_id set default public.mi_kiosco();
        alter table public.proveedores_cuenta enable row level security;
        drop policy if exists aislado_por_negocio on public.proveedores_cuenta;
        create policy aislado_por_negocio on public.proveedores_cuenta for all to authenticated
            using (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()))
            with check (kiosco_id = (select public.mi_kiosco()) or (select public.soy_superadmin()));
        revoke all on public.proveedores_cuenta from anon;
        revoke all on public.vw_saldo_proveedores from anon;
        grant select, insert, update, delete on public.proveedores_cuenta to authenticated;
        grant select on public.vw_saldo_proveedores to authenticated;
        grant usage, select on sequence public.proveedores_cuenta_id_seq to authenticated;
    else
        grant select, insert, update, delete on public.proveedores_cuenta to anon, authenticated;
        grant select on public.vw_saldo_proveedores to anon, authenticated;
        grant usage, select on sequence public.proveedores_cuenta_id_seq to anon, authenticated;
    end if;
end $$;

notify pgrst, 'reload schema';
select 'Tabla de deudas con proveedores lista' as resultado;
