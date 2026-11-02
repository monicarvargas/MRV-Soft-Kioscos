-- ¿Por qué no se cobra el precio noche? (no cambia nada, solo mira)
-- Cambiá el 3 por el número de tu negocio.
select 'lista' as que, l.id::text, l.nombre_lista as dato, l.nombre_columna_productos as extra
from listas_precios l where l.kiosco_id = 3
union all
select 'horario', h.id::text, h.hora_inicio::text || ' a ' || h.hora_fin::text, 'lista ' || coalesce(h.lista_precio_id::text, '-') || ' · ' || h.tipo_ajuste
from horarios_ventas h where h.kiosco_id = 3
union all
select 'tramos', count(*)::text, 'tramos de recargo cargados', ''
from listas_recargos r where r.kiosco_id = 3
union all
select 'hora de la base', '', to_char(now() at time zone 'America/Argentina/Buenos_Aires', 'HH24:MI'), '';
