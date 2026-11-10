using BlazorKioscoCompartido.Models;
using BlazorKioscoCompartido.Pages;
using Supabase;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BlazorKioscoCompartido.Services.SinInternet;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorKioscoCompartido.Services
{
    public class PrecioService
    {
        private readonly Supabase.Client _supaClient;

        // Inyectamos el cliente de Supabase por constructor
        public PrecioService(Supabase.Client supaClient)
        {
            _supaClient = supaClient;
        }

        // ============================================================ REGLAS DE LAS LISTAS (por negocio)
        // Horarios, listas y tramos de recargo de cada negocio, guardados en memoria (se refrescan cada 10 min).
        // Antes el recargo de la Lista 2 estaba fijo en el código para el negocio 1; ahora sale de la base (script 33)
        // y se configura en Configuración → Listas de precios.

        public sealed record ReglasListas(DateTime Cuando, List<HorarioVenta> Horarios, List<ListaPrecio> Listas, List<ListaRecargo> Recargos, List<ListaDia>? Dias = null);

        private static readonly ConcurrentDictionary<long, ReglasListas> _reglas = new();

        /// <summary>Trae (si hace falta) las reglas de listas del negocio. Llamarlo al abrir las páginas que calculan precios.</summary>
        public async Task AsegurarReglasAsync(long kioscoId, bool forzar = false)
        {
            if (kioscoId == 0) return;
            if (!forzar && _reglas.TryGetValue(kioscoId, out var r) && DateTime.UtcNow - r.Cuando < TimeSpan.FromMinutes(10)) return;

            var off = VentaSinInternet.Actual;
            if (off?.ModoLimitado == true)
            {
                var c = off.ReglasGuardadas(kioscoId);
                if (c != null) _reglas[kioscoId] = c;
                return;
            }

            try
            {
                long k = kioscoId;
                var hs = (await _supaClient.From<HorarioVenta>().Where(x => x.KioscoId == k).Get()).Models ?? new();
                var ls = (await _supaClient.From<ListaPrecio>().Where(x => x.KioscoId == k).Get()).Models ?? new();
                List<ListaRecargo> rs;
                try { rs = (await _supaClient.From<ListaRecargo>().Where(x => x.KioscoId == k).Get()).Models ?? new(); }
                catch (Exception ex) when (!VentaSinInternet.EsErrorDeRed(ex)) { rs = new(); }   // falta el script 33
                List<ListaDia> ds;
                try { ds = (await _supaClient.From<ListaDia>().Where(x => x.KioscoId == k).Get()).Models ?? new(); }
                catch (Exception ex) when (!VentaSinInternet.EsErrorDeRed(ex)) { ds = new(); }   // falta el script 36
                _reglas[kioscoId] = new ReglasListas(DateTime.UtcNow, hs, ls, rs, ds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en PrecioService (reglas): {ex.Message}");
                if (!_reglas.ContainsKey(kioscoId))
                {
                    var c = off?.ReglasGuardadas(kioscoId);
                    if (c != null) _reglas[kioscoId] = c;
                }
            }
        }

        /// <summary>Se cambió algo en Configuración: la próxima vez se vuelven a leer.</summary>
        public static void OlvidarReglas(long kioscoId) => _reglas.TryRemove(kioscoId, out _);

        /// <summary>¿La hora cae en el horario? Puede pasar la medianoche (ej: 22:00 a 06:00). La hora final ya no cuenta (a las 08:00 en punto termina).</summary>
        public static bool EnHorario(HorarioVenta h, TimeSpan hora)
        {
            if (h.HoraInicio == h.HoraFin) return false;
            return h.HoraInicio < h.HoraFin
                ? hora >= h.HoraInicio && hora < h.HoraFin
                : hora >= h.HoraInicio || hora < h.HoraFin;
        }

        /// <summary>Columna de precio que corresponde a esa hora (con los horarios y listas que se le pasan).</summary>
        public static string ColumnaSegunHora(IEnumerable<HorarioVenta> horarios, IEnumerable<ListaPrecio> listas, TimeSpan hora)
        {
            var regla = horarios.FirstOrDefault(h => h.TipoAjuste == "LISTA" && EnHorario(h, hora));
            if (regla == null) return "precio_venta";
            var lista = listas.FirstOrDefault(l => l.Id == regla.ListaPrecioId);
            return string.IsNullOrWhiteSpace(lista?.NombreColumnaProductos) ? "precio_venta" : lista!.NombreColumnaProductos!;
        }

        /// <summary>Columna de precio del negocio a esa hora, con las reglas en memoria. null = todavía no se cargaron.</summary>
        public static string? ColumnaSegunHora(long kioscoId, TimeSpan hora) => ColumnaSegunMomento(kioscoId, DateTime.Today.Add(hora));

        /// <summary>
        /// Lista que corresponde a ese momento: si es un día marcado (domingo, feriado...) manda esa lista todo el día;
        /// si no, la del horario (ej: noche). Script 36.
        /// </summary>
        public static string ColumnaSegunMomento(IEnumerable<HorarioVenta> horarios, IEnumerable<ListaPrecio> listas, IEnumerable<ListaDia>? dias, DateTime momento)
        {
            var dia = (dias ?? Enumerable.Empty<ListaDia>()).Where(d => d.Aplica(momento))
                .OrderByDescending(d => d.Fecha != null)   // una fecha puntual gana sobre "todos los domingos"
                .FirstOrDefault();
            if (dia != null)
            {
                var l = listas.FirstOrDefault(x => x.Id == dia.ListaPrecioId);
                if (!string.IsNullOrWhiteSpace(l?.NombreColumnaProductos)) return l!.NombreColumnaProductos!;
            }
            return ColumnaSegunHora(horarios, listas, momento.TimeOfDay);
        }

        /// <summary>Columna de precio del negocio en ese momento, con las reglas en memoria. null = todavía no se cargaron.</summary>
        public static string? ColumnaSegunMomento(long kioscoId, DateTime momento) =>
            _reglas.TryGetValue(kioscoId, out var r) ? ColumnaSegunMomento(r.Horarios, r.Listas, r.Dias, momento) : null;

        /// <summary>Nombre de la lista de esa columna (ej: "NOCHE"), con las reglas en memoria.</summary>
        public static string NombreLista(long kioscoId, string? columna) =>
            _reglas.TryGetValue(kioscoId, out var r) ? r.Listas.FirstOrDefault(l => l.NombreColumnaProductos == columna)?.NombreLista ?? "" : "";

        /// <summary>Nombre de la lista para mostrar: el que le puso el negocio, o el de fábrica (Precio Venta / Precio Noche / Precio Feriado).</summary>
        public static string NombreParaMostrar(long kioscoId, string? columna)
        {
            var n = NombreLista(kioscoId, columna).Trim();
            if (n != "") return n;
            return columna switch
            {
                "precio_lista2" => "Precio Noche",
                "precio_lista3" => "Precio Feriado",
                "precio_venta_por_mayor" => "Precio x Mayor",
                _ => "Precio Venta"
            };
        }

        /// <summary>
        /// Determina el nombre de la columna de precio activa según el kiosco y la hora.
        /// </summary>
        public async Task<string> ObtenerColumnaPrecioActivaAsync(long idNegocio, TimeSpan horaInicioVenta)
        {
            await AsegurarReglasAsync(idNegocio);
            return ColumnaSegunHora(idNegocio, horaInicioVenta) ?? "precio_venta";
        }

        public async Task<string> ObtenerNombreColumnaAsync(long idNegocio, string nombre_columna_en_tabla_productos)
        {
            string nombreColumna = "";
            try
            {
                // Ejecutamos el equivalente de tu query SQL en Supabase
                var res = await _supaClient.From<ListaPrecio>()
                    .Where(l => l.KioscoId == idNegocio && l.NombreColumnaProductos == nombre_columna_en_tabla_productos)
                    .Get();

                var lista = res.Models.FirstOrDefault();
                if (lista != null && !string.IsNullOrEmpty(lista.NombreLista))
                {
                    nombreColumna = lista.NombreLista; // Asignamos el nombre real (ej: "Noche")
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el nombre de la lista: {ex.Message}");
            }
            return nombreColumna;
        }
        /// <summary>Tramos de recargo de la Lista 2 (precio_lista2) del negocio.</summary>
        private static List<ListaRecargo> TramosLista2(long idKiosco) => TramosDe(idKiosco, "precio_lista2");

        /// <summary>Tramos de recargo de una lista (Lista 2 noche, Lista 3 feriados...).</summary>
        private static List<ListaRecargo> TramosDe(long idKiosco, string columna)
        {
            if (!_reglas.TryGetValue(idKiosco, out var r)) return new();
            var l = r.Listas.Where(x => x.NombreColumnaProductos == columna).OrderBy(x => x.Id).FirstOrDefault();
            return l == null ? new() : r.Recargos.Where(x => x.ListaPrecioId == l.Id).OrderBy(x => x.Desde).ToList();
        }

        /// <summary>Recargo de una lista para ese precio (monto fijo o % del tramo). Sin tramos → 0.</summary>
        public static decimal RecargoDe(decimal precio, long idKiosco, string columna)
        {
            var tramo = TramosDe(idKiosco, columna).FirstOrDefault(t => precio > t.Desde && (t.Hasta == null || precio <= t.Hasta));
            if (tramo == null) return 0m;
            return tramo.EsPorcentaje ? Math.Round(precio * tramo.Monto / 100m, 2) : tramo.Monto;
        }

        /// <summary>Precio de una lista con recargo según la regla (en GRAMOS, sobre los 100 g). Por kilo.</summary>
        public static decimal PrecioPorRegla(Producto p, long idKiosco, string columna)
        {
            decimal pv = p.PrecioVenta ?? 0m;
            if (p.PrecioCada100g())
            {
                decimal pv100 = pv / 10m;
                return (pv100 + RecargoDe(pv100, idKiosco, columna)) * 10m;
            }
            return pv + RecargoDe(pv, idKiosco, columna);
        }

        /// <summary>¿La Lista 2 de este negocio se calcula sola (tiene tramos de recargo)? Si no, se carga a mano.</summary>
        public static bool TieneRecargoAutomatico(long idKiosco) => TramosLista2(idKiosco).Count > 0;

        /// <summary>
        /// Recargo de la Lista 2 para ese precio, según los tramos del negocio (Configuración → Listas de precios).
        /// Ej: más de $500 hasta $900 → + $100. Sin tramos → 0.
        /// </summary>
        public static decimal CalcularPrecioVentaConRecargo(decimal precioVenta, long idKiosco)
        {
            var tramo = TramosLista2(idKiosco)
                .FirstOrDefault(t => precioVenta > t.Desde && (t.Hasta == null || precioVenta <= t.Hasta));
            if (tramo == null) return 0m;
            return tramo.EsPorcentaje ? Math.Round(precioVenta * tramo.Monto / 100m, 2) : tramo.Monto;
        }
        /// <summary>
        /// Precio de venta de un producto según la lista activa (por horario). Es la MISMA regla que muestra el buscador:
        /// si la Lista 2 no está cargada (o es menor o igual al precio de venta) se usa el precio de venta + el recargo del kiosco.
        /// En productos por gramos el valor es por kilo.
        /// </summary>
        /// <summary>
        /// ¿La Lista 2 no está cargada (vacía o 0) o quedó MENOR que el precio de venta (un error)? Entonces se usa la regla.
        /// Si es IGUAL al precio de venta se respeta: ej. gomitas, mismo precio de día y de noche.
        /// </summary>
        public static bool Lista2SinCargar(Producto p) =>
            (p.PrecioLista2 ?? 0) <= 0 || p.PrecioLista2 < (p.PrecioVenta ?? 0);

        /// <summary>Lista 2 según la regla del negocio: precio de venta + recargo del tramo (en GRAMOS, sobre los 100 g). Por kilo.</summary>
        public static decimal Lista2PorRegla(Producto p, long idKiosco)
        {
            decimal pv = p.PrecioVenta ?? 0m;
            if (p.PrecioCada100g())
            {
                decimal pv100 = pv / 10m;
                return (pv100 + CalcularPrecioVentaConRecargo(pv100, idKiosco)) * 10m;
            }
            return pv + CalcularPrecioVentaConRecargo(pv, idKiosco);
        }

        // ---------- mismo precio de día y de noche (ej: gomitas) ----------
        private static readonly ConditionalWeakTable<Producto, object> _igualAlEmpezar = new();

        /// <summary>¿La Lista 2 está cargada IGUAL al precio de venta? (a propósito: mismo precio de día y de noche)</summary>
        public static bool EsMismoPrecioDiaNoche(Producto p) => (p.PrecioLista2 ?? 0) > 0 && p.PrecioLista2 == p.PrecioVenta;

        /// <summary>Igual que EsMismoPrecioDiaNoche, pero recuerda cómo estaba la PRIMERA vez que se preguntó por ese producto
        /// (antes de que se le empiecen a cambiar los precios en la pantalla).</summary>
        public static bool EraMismoPrecioDiaNoche(Producto p) => (bool)_igualAlEmpezar.GetValue(p, x => EsMismoPrecioDiaNoche(x));

        /// <summary>Lista 2 automática, pero si tenía el mismo precio de día y de noche, la sigue igualando al precio de venta.</summary>
        public static void ProcesarLista2Automatico(Producto producto, long idKiosco, AppState state, bool mismoPrecioDiaNoche)
        {
            if (producto == null) return;
            if (!mismoPrecioDiaNoche) { ProcesarLista2Automatico(producto, idKiosco, state); return; }
            producto.PrecioLista2 = producto.PrecioVenta;
            decimal c = producto.PrecioCosto ?? 0m;
            producto.UtilidadLista2 = c > 0 ? Math.Round(((producto.PrecioLista2 ?? 0m) - c) / c * 100m, 2) : 0m;
        }

        public static decimal PrecioSegunLista(Producto p, string? columnaPrecioActiva, long idKiosco)
        {
            decimal pv = p.PrecioVenta ?? 0m;
            static decimal OPv(decimal? x, decimal pv) => x.HasValue && x.Value > 0 ? x.Value : pv;

            return columnaPrecioActiva switch
            {
                "precio_lista2" => Lista2SinCargar(p) ? Lista2PorRegla(p, idKiosco) : p.PrecioLista2!.Value,
                // Lista 3 (ej: domingos y feriados): la cargada a mano, o precio de venta + el monto fijo / % de la regla
                "precio_lista3" => ((p.PrecioLista3 ?? 0) <= 0 || p.PrecioLista3 < pv) ? PrecioPorRegla(p, idKiosco, "precio_lista3") : p.PrecioLista3!.Value,
                "precio_lista4" => OPv(p.PrecioLista4, pv),
                "precio_lista5" => OPv(p.PrecioLista5, pv),
                _ => pv
            };
        }

        public static void ProcesarLista2Automatico_(Producto producto, long idKiosco)
        {
            if (producto == null) return;

            decimal precioVenta = producto.PrecioVenta ?? 0m;
            decimal costo = producto.PrecioCosto ?? 0m;

            if (precioVenta > 0)
            {
                // 1. Obtener recargo y fijar el Precio Lista 2
                decimal recargo = CalcularPrecioVentaConRecargo(precioVenta, idKiosco);
                producto.PrecioLista2 = precioVenta + recargo;

                // 2. Calcular la Utilidad de Lista 2 respecto al Costo
                if (costo > 0)
                {
                    decimal utilidad = ((producto.PrecioLista2.Value - costo) / costo) * 100m;
                    producto.UtilidadLista2 = Math.Round(utilidad, 2);

                }
                else
                {
                    producto.UtilidadLista2 = 0m;
                }
            }
        }
        public static void ProcesarLista2Automatico(Producto producto, long idKiosco, AppState state)
            {
            if (producto == null) return;

            // Sin tramos de recargo: la Lista 2 se carga a mano; si tiene utilidad, sigue al costo (como la Lista 3)
            if (!TieneRecargoAutomatico(idKiosco))
            {
                if ((producto.UtilidadLista2 ?? 0) > 0 && (producto.PrecioCosto ?? 0) > 0)
                    producto.PrecioLista2 = producto.CalcularPrecioPorUtilidad(producto.PrecioCosto, producto.UtilidadLista2, state);
                return;
            }

            decimal precioVentaKg = producto.PrecioVenta ?? 0m;
            decimal costo = producto.PrecioCosto ?? 0m;

            if (precioVentaKg > 0)
            {
                if (producto.PrecioCada100g())   // en KILO el precio va por kilo
                {
                    // 1. Convertir el Precio de Venta a 100g
                    decimal precioVenta100g = precioVentaKg / 10m;

                    // 2. Calcular el recargo sobre los 100g
                    decimal recargo100g = CalcularPrecioVentaConRecargo(precioVenta100g, idKiosco);
                    decimal precioLista2_100gBruto = precioVenta100g + recargo100g;

                    // 3. Aplicar redondeo a los 100g si corresponde
                    decimal precioLista2_100gFinal = precioLista2_100gBruto;
                    if (producto.UsaRedondeo(state))   // salvo los productos "sin redondeo"
                    {
                        decimal paso = state!.MultiploRedondeo > 0 ? (decimal)state.MultiploRedondeo : 50m;
                        precioLista2_100gFinal = Math.Ceiling(precioLista2_100gBruto / paso) * paso;
                    }

                    // 4. Guardar en la BD el valor equivalente por Kilo
                    producto.PrecioLista2 = precioLista2_100gFinal * 10m;
                }
                else
                {
                    // Lógica para productos por unidad
                    decimal recargo = CalcularPrecioVentaConRecargo(precioVentaKg, idKiosco);
                    decimal precioLista2Bruto = precioVentaKg + recargo;

                    if (producto.UsaRedondeo(state))   // salvo los productos "sin redondeo"
                    {
                        decimal paso = state!.MultiploRedondeo > 0 ? (decimal)state.MultiploRedondeo : 50m;
                        producto.PrecioLista2 = Math.Ceiling(precioLista2Bruto / paso) * paso;
                    }
                    else
                    {
                        producto.PrecioLista2 = Math.Round(precioLista2Bruto, 2);
                    }
                }

                // 5. Recalcular la Utilidad de Lista 2 respecto al Costo por Kilo/Unidad
                if (costo > 0 && producto.PrecioLista2.HasValue)
                {
                    decimal utilidad = ((producto.PrecioLista2.Value - costo) / costo) * 100m;
                    producto.UtilidadLista2 = Math.Round(utilidad, 2);
                }
                else
                {
                    producto.UtilidadLista2 = 0m;
                }
            }
        }
        public static decimal  CalcularPrecioCostoDeLista2desdePrecioLista2(decimal precioLista2, decimal costo)
         {
            decimal utilidad = 0m;
            // Implementación pendiente: Calcular el precio de costo basado en la lista 2 y la utilidad.
            if (costo > 0 && precioLista2 > 0)
            {
                utilidad = ((precioLista2 - costo) / costo) * 100m;
                //producto.UtilidadLista2 = Math.Round(utilidad, 2);
            }
            return utilidad;
        }
        public static void CalcularPrecioLista2DesdeUtilidad(Producto producto, decimal nuevaUtilidad, AppState state)
        {
            if (producto == null) return;

            producto.UtilidadLista2 = nuevaUtilidad;

            // Calcula el Precio Lista 2 basado en el % de utilidad ingresado
            if (producto.PrecioCosto > 0)
            {
                producto.PrecioLista2 = producto.CalcularPrecioPorUtilidad(producto.PrecioCosto, nuevaUtilidad, state);
            }
        }
    }
}
