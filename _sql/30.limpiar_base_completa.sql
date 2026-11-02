-- =====================================================================
-- LIMPIEZA COMPLETA DE LA BASE (sirve para TEST y para PRODUCCIÓN)
-- Deja la base vacía, como recién instalada:
--   QUEDA:  el/los usuario(s) SUPERADMIN, su negocio y los PLANES (Básico/Medio/Avanzado).
--   SE BORRA TODO LO DEMÁS: negocios, usuarios, productos, ventas, fiados, clientes, proveedores,
--          compras, pedidos, familias, formas de pago, listas, turnos, tareas, asistencias,
--          ventas sin internet, sesiones...
--   (no se toca la estructura: tablas, funciones y permisos quedan igual)
--
-- ANTES, EN PRODUCCIÓN: hacé una copia (doble clic en _backup_base\backup_produccion.bat).
--
-- CÓMO USARLO
--   1) Fijate arriba a la izquierda en Supabase qué proyecto está abierto (TEST o PRODUCCIÓN).
--   2) Corrélo como está: NO BORRA NADA. Muestra qué se borra y qué queda (pestaña Results).
--   3) Si está bien: en v_confirmo escribí el nombre del proyecto que está abierto ('TEST' o 'PRODUCCION'),
--      poné v_ejecutar en true y corrélo de nuevo.
--   4) Después: cerrá la app y volvé a entrar con el superadmin (las sesiones se borran).
-- Se puede correr más de una vez.
-- =====================================================================

drop table if exists _reset;
create temp table _reset (orden int, tabla text, filas bigint, accion text);

do $$
declare
    -- ======== OPCIONES ========
    v_confirmo        text    := '';      -- escribí 'TEST' o 'PRODUCCION' (el proyecto abierto)
    v_ejecutar        boolean := false;   -- false = solo mostrar; true = BORRAR
    v_borrar_anonimos boolean := true;    -- true = borra también las credenciales anónimas de los equipos (auth)
    -- ==========================
    se_queda constant text[] := array['usuarios', 'kioscos', 'planes'];
    t text;
    n bigint;
    lista text[];
    peligro text;
    v_super int;
    v_borrar boolean;
begin
    v_borrar := v_ejecutar and upper(trim(v_confirmo)) in ('TEST', 'PRODUCCION', 'PRODUCCIÓN');
    if v_ejecutar and not v_borrar then
        raise exception 'Para borrar, escribí en v_confirmo el proyecto abierto: TEST o PRODUCCION.';
    end if;

    select count(*) into v_super from public.usuarios where lower(rol) = 'superadmin';
    if v_super = 0 then
        raise exception 'No hay ningún usuario superadmin: no se puede limpiar (no podrías volver a entrar).';
    end if;

    -- todas las tablas del esquema public menos las que quedan
    select array_agg(c.relname order by c.relname) into lista
    from pg_class c join pg_namespace s on s.oid = c.relnamespace
    where s.nspname = 'public' and c.relkind = 'r'
      and c.relname <> all (se_queda);

    -- control: si lo que queda depende de alguna tabla que se vacía, se cancela (se borraría en cascada)
    select string_agg(distinct c.conrelid::regclass::text || ' → ' || c.confrelid::regclass::text, ', ') into peligro
    from pg_constraint c
    where c.contype = 'f'
      and c.conrelid in (select ('public.' || x)::regclass from unnest(se_queda) x)
      and (select relname from pg_class where oid = c.confrelid) <> all (se_queda);

    if peligro is not null then
        insert into _reset values (0, peligro, null, '¡NO SE PUEDE! lo que queda depende de estas tablas. Pasale este texto al chat.');
        return;
    end if;

    -- vista previa
    insert into _reset
    select 0, 'QUEDA: ' || u.username || ' (negocio ' || coalesce(k.codigo_vinculacion, '?') || ' - ' || coalesce(k.nombre_negocio, '?') || ')', null, 'se conserva'
    from public.usuarios u left join public.kioscos k on k.id = u.id_kiosco
    where lower(u.rol) = 'superadmin';
    select count(*) into n from public.planes;
    insert into _reset values (0, 'QUEDAN: planes', n, 'se conservan');

    select count(*) into n from public.usuarios where lower(coalesce(rol, '')) <> 'superadmin';
    insert into _reset values (1, 'usuarios', n, case when v_borrar then 'BORRADOS (menos el superadmin)' else 'se borran todos menos el superadmin' end);
    select count(*) into n from public.kioscos
    where id not in (select id_kiosco from public.usuarios where lower(rol) = 'superadmin');
    insert into _reset values (1, 'kioscos (negocios)', n, case when v_borrar then 'BORRADOS (menos el del superadmin)' else 'se borran todos menos el negocio del superadmin' end);

    foreach t in array coalesce(lista, '{}') loop
        execute format('select count(*) from public.%I', t) into n;
        insert into _reset values (2, t, n, case when v_borrar then 'VACIADA' else 'se vaciaría' end);
    end loop;

    if v_borrar_anonimos then
        begin
            execute 'select count(*) from auth.users where is_anonymous' into n;
            insert into _reset values (3, 'auth.users (anónimos de los equipos)', n,
                case when v_borrar then 'BORRADOS' else 'se borrarían (los equipos piden otra credencial al entrar)' end);
        exception when others then
            insert into _reset values (3, 'auth.users', null, 'no se pudo leer: se deja como está');
            v_borrar_anonimos := false;
        end;
    end if;

    if not v_borrar then return; end if;

    -- ======== BORRADO ========
    foreach t in array coalesce(lista, '{}') loop
        execute format('truncate table public.%I restart identity cascade', t);
    end loop;

    delete from public.usuarios where lower(coalesce(rol, '')) <> 'superadmin';
    delete from public.kioscos
    where id not in (select id_kiosco from public.usuarios where lower(rol) = 'superadmin');

    -- el número de movimiento de las ventas vuelve a empezar
    if to_regclass('public.secuencia_movimientos_ventas') is not null then
        perform setval('public.secuencia_movimientos_ventas', 1, false);
    end if;

    if v_borrar_anonimos then
        begin
            execute 'delete from auth.users where is_anonymous';
        exception when others then
            insert into _reset values (8, 'auth.users', null, 'no se pudieron borrar los anónimos (no es grave)');
        end;
    end if;

    insert into _reset values (9, 'LISTO', null,
        'Base limpia (' || upper(trim(v_confirmo)) || '). Cerrá la app, entrá con el superadmin y creá los negocios desde "Administrar negocios".');
end $$;

select tabla, filas, accion from _reset order by orden, tabla;
