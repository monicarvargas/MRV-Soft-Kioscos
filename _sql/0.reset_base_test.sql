-- =====================================================================
-- RESET COMPLETO DE LA BASE DE **TEST**
-- Deja la base vacía, salvo el/los usuario(s) SUPERADMIN y su negocio
-- (el login pide código de negocio, por eso se conserva el negocio del superadmin).
--
-- ⚠️  SOLO PARA LA BASE DE TEST. NUNCA EN PRODUCCIÓN.
--     Antes de correrlo fijate arriba a la izquierda en Supabase qué proyecto está abierto.
--
-- CÓMO USARLO
--   1) Corrélo como está: NO BORRA NADA. Muestra qué se borra y qué queda (pestaña Results).
--   2) Si está todo bien, poné las DOS opciones de abajo en true y corrélo de nuevo.
-- =====================================================================

drop table if exists _reset;
create temp table _reset (orden int, tabla text, filas bigint, accion text);

do $$
declare
    -- ======== OPCIONES ========
    v_es_la_base_de_test boolean := false;   -- confirmá que estás en el proyecto de TEST
    v_ejecutar           boolean := false;   -- false = solo mostrar; true = BORRAR
    -- ==========================
    t text;
    n bigint;
    lista text[];
    peligro text;
    v_super int;
begin
    select count(*) into v_super from public.usuarios where lower(rol) = 'superadmin';
    if v_super = 0 then
        raise exception 'No hay ningún usuario superadmin: no se puede limpiar (no podrías volver a entrar).';
    end if;

    -- todas las tablas del esquema public, menos usuarios y kioscos (esas se limpian fila por fila)
    select array_agg(c.relname order by c.relname) into lista
    from pg_class c join pg_namespace s on s.oid = c.relnamespace
    where s.nspname = 'public' and c.relkind = 'r'
      and c.relname not in ('usuarios', 'kioscos')
      and c.relname not like '\_%';

    -- control: si usuarios o kioscos dependen de alguna tabla que se vacía, se cancela (si no, se borrarían en cascada)
    select string_agg(distinct c.conrelid::regclass::text || ' → ' || c.confrelid::regclass::text, ', ') into peligro
    from pg_constraint c
    where c.contype = 'f'
      and c.conrelid in ('public.usuarios'::regclass, 'public.kioscos'::regclass)
      and c.confrelid::regclass::text not in ('usuarios', 'kioscos', 'public.usuarios', 'public.kioscos');

    if peligro is not null then
        insert into _reset values (0, peligro, null, '¡NO SE PUEDE! usuarios/kioscos dependen de estas tablas. Pasale este texto al chat.');
        return;
    end if;

    -- vista previa
    foreach t in array coalesce(lista, '{}') loop
        execute format('select count(*) from public.%I', t) into n;
        insert into _reset values (2, t, n, case when v_ejecutar and v_es_la_base_de_test then 'VACIADA' else 'se vaciaría' end);
    end loop;

    select count(*) into n from public.usuarios where lower(coalesce(rol, '')) <> 'superadmin';
    insert into _reset values (1, 'usuarios', n, 'se borran todos menos el superadmin');
    select count(*) into n from public.kioscos
    where id not in (select id_kiosco from public.usuarios where lower(rol) = 'superadmin');
    insert into _reset values (1, 'kioscos', n, 'se borran todos menos el negocio del superadmin');

    insert into _reset
    select 0, 'QUEDA: ' || u.username || ' (negocio ' || coalesce(k.codigo_vinculacion, '?') || ' - ' || coalesce(k.nombre_negocio, '?') || ')', null, 'se conserva'
    from public.usuarios u left join public.kioscos k on k.id = u.id_kiosco
    where lower(u.rol) = 'superadmin';

    if not (v_ejecutar and v_es_la_base_de_test) then return; end if;

    -- ======== BORRADO ========
    foreach t in array coalesce(lista, '{}') loop
        execute format('truncate table public.%I restart identity cascade', t);
    end loop;

    delete from public.usuarios where lower(coalesce(rol, '')) <> 'superadmin';
    delete from public.kioscos
    where id not in (select id_kiosco from public.usuarios where lower(rol) = 'superadmin');

    insert into _reset values (9, 'LISTO', null, 'Base de test limpia. Entrá con el superadmin y creá los negocios desde "Administrar negocios".');
end $$;

select tabla, filas, accion from _reset order by orden, tabla;
