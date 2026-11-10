using BlazorKioscoCompartido.Models;
using Newtonsoft.Json;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Guardar MUCHOS productos de una sola vez (lo usa el importador de Excel).
    /// Grabar uno por uno (miles de grabaciones seguidas) agota el "presupuesto de disco" del plan gratis
    /// de Supabase y la base se cuelga. De a 200 son unas pocas grabaciones.
    ///   - Nuevos:     un Insert con la lista (de a 200).
    ///   - Existentes: la función importar_actualizar_productos (script 40), de a 200.
    ///     Lo que no se manda queda como estaba; las reglas de cada negocio se cumplen igual.
    /// </summary>
    public static class GuardadoEnLote
    {
        /// <summary>Cuántos productos por vez.</summary>
        public const int Tamanio = 200;

        /// <summary>
        /// El producto entero con los nombres de columna de la base, igual que lo graba Update(p):
        /// las 5 columnas "opcionales" (sin_redondeo, subfamilia_id, bulto_*) solo van si tienen valor.
        /// subfamiliaNula = true: manda subfamilia_id = null para borrarla.
        /// </summary>
        public static Dictionary<string, object?> Fila(Producto p, bool subfamiliaNula = false)
        {
            var d = new Dictionary<string, object?>
            {
                ["id"] = p.Id,
                ["familia_id"] = p.FamiliaId,
                ["proveedor_id"] = p.ProveedorId,
                ["codigo_barras"] = p.CodigoBarras,
                ["nombre"] = p.Nombre,
                ["marca"] = p.Marca,
                ["precio_costo"] = p.PrecioCosto,
                ["precio_venta"] = p.PrecioVenta,
                ["utilidad_pv"] = p.UtilidadPV,
                ["precio_lista2"] = p.PrecioLista2,
                ["utilidad_lista2"] = p.UtilidadLista2,
                ["precio_lista3"] = p.PrecioLista3,
                ["utilidad_lista3"] = p.UtilidadLista3,
                ["precio_lista4"] = p.PrecioLista4,
                ["utilidad_lista4"] = p.UtilidadLista4,
                ["precio_lista5"] = p.PrecioLista5,
                ["utilidad_lista5"] = p.UtilidadLista5,
                ["precio_venta_por_mayor"] = p.PrecioVentaxMayor,
                ["utilidad_x_mayor"] = p.UtilidadVentaxMayor,
                ["cant_por_mayor"] = p.CantxMayor,
                ["venta_por_peso"] = p.VentaxPeso,
                ["unidad"] = p.UnidadMedida,
                ["stock_actual"] = p.StockActual,
                ["stock_ideal"] = p.StockIdeal,
                ["stock_minimo"] = p.StockMinimo,
                ["nota_producto"] = p.NotaProducto,
                ["imagen_url"] = p.ImagenUrl,
                ["fecha_actualizacion"] = p.FechaActualizacion,
                ["activo"] = p.Activo,
                ["es_combo"] = p.EsCombo,
                ["fecha_vigencia_desde_combo"] = p.FechaVigenciaDesdeCombo,
                ["fecha_vigencia_hasta_combo"] = p.FechaVigenciaHastaCombo,
                ["limite_unidades_combo"] = p.LimiteUnidadesCombo,
                ["categoria_venta_rapida_id"] = p.CategoriaVentaRapidaId,
                ["mostrar_en_venta_rapida"] = p.MostrarEnVentaRapida,
                ["orden_venta_rapida"] = p.OrdenVentaRapida,
                ["admite_importe"] = p.AdmiteImporte,
            };
            if (p.SinRedondeo.HasValue) d["sin_redondeo"] = p.SinRedondeo;
            if (p.SubfamiliaId.HasValue) d["subfamilia_id"] = p.SubfamiliaId;
            else if (subfamiliaNula) d["subfamilia_id"] = null;
            if (p.BultoPrecio.HasValue) d["bulto_precio"] = p.BultoPrecio;
            if (p.BultoUnidades.HasValue) d["bulto_unidades"] = p.BultoUnidades;
            if (p.BultoFecha.HasValue) d["bulto_fecha"] = p.BultoFecha;
            return d;
        }

        /// <summary>Solo algunas columnas de un producto (ej: el código o el bulto). Lo demás queda como está.</summary>
        public static Dictionary<string, object?> Campos(long id, params (string Columna, object? Valor)[] campos)
        {
            var d = new Dictionary<string, object?> { ["id"] = id };
            foreach (var (c, v) in campos) d[c] = v;
            return d;
        }

        /// <summary>
        /// Actualiza hasta <see cref="Tamanio"/> productos en una sola grabación (script 40).
        /// Devuelve los ids que se actualizaron (los que no están: no se encontraron).
        /// </summary>
        public static async Task<HashSet<long>> ActualizarAsync(Supabase.Client db, IEnumerable<Dictionary<string, object?>> filas)
        {
            var res = await db.Rpc("importar_actualizar_productos",
                new Dictionary<string, object> { { "p_filas", filas.ToList() } });
            var ids = JsonConvert.DeserializeObject<List<long>>(string.IsNullOrWhiteSpace(res.Content) ? "[]" : res.Content!) ?? new();
            return new HashSet<long>(ids);
        }

        /// <summary>¿El error es porque todavía no se corrió el script 40? (entonces se guarda uno por uno, como antes)</summary>
        public static bool FaltaLaFuncion(Exception ex)
        {
            var m = ex.Message ?? "";
            return m.Contains("PGRST202") || m.Contains("Could not find the function")
                || (m.Contains("importar_actualizar_productos") && (m.Contains("does not exist") || m.Contains("no existe")));
        }

        /// <summary>
        /// Grupos para insertar juntos: en un Insert con varios, todos tienen que mandar las mismas columnas
        /// (las 5 opcionales solo van si tienen valor), así que se separan según cuáles tienen.
        /// </summary>
        public static IEnumerable<List<T>> GruposParaInsertar<T>(IEnumerable<T> items, Func<T, Producto> producto) =>
            items.GroupBy(x =>
                {
                    var p = producto(x);
                    return (p.SinRedondeo.HasValue, p.SubfamiliaId.HasValue, p.BultoPrecio.HasValue, p.BultoUnidades.HasValue, p.BultoFecha.HasValue);
                })
                .SelectMany(g => g.Chunk(Tamanio).Select(c => c.ToList()));
    }
}
