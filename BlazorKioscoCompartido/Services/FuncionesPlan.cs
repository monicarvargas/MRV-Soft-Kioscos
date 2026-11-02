namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Funciones que se habilitan según el plan del negocio (ver _sql\20.planes_funciones.sql).
    /// Siempre están (no dependen del plan): Vender, Productos, Nuevo producto y Configuración.
    /// Para agregar una función nueva: sumarla a <see cref="Todas"/> (y a sus rutas en <see cref="Rutas"/>),
    /// y después tildarla en los planes desde Administrar negocios → Planes.
    /// </summary>
    public static class FuncionesPlan
    {
        public const string Carteles = "carteles";
        public const string ImportarExcel = "importar_excel";
        public const string ReporteDia = "reporte_dia";
        public const string PreciosGrupo = "precios_grupo";
        public const string VentaRapida = "venta_rapida";
        public const string Fiado = "fiado";
        public const string ReportesProductos = "reportes_productos";
        public const string ComprasMayorista = "compras_mayorista";
        public const string PedidosProveedores = "pedidos_proveedores";
        public const string DeudasProveedores = "deudas_proveedores";
        public const string Asistencia = "asistencia";
        public const string Encargado = "encargado";
        public const string Tareas = "tareas";

        public sealed record Funcion(string Clave, string Nombre, string Icono, string Detalle);

        /// <summary>Todas las funciones que se pueden habilitar o no, en el orden en que se muestran.</summary>
        public static readonly IReadOnlyList<Funcion> Todas = new List<Funcion>
        {
            new(Carteles,           "Carteles y códigos",        "bi-tags-fill",               "Imprimir carteles para la góndola y etiquetas con código de barras"),
            new(ImportarExcel,      "Importar desde Excel",      "bi-file-earmark-excel-fill", "Cargar o actualizar productos y precios desde un Excel"),
            new(ReporteDia,         "Recaudación del día",       "bi-calendar-check-fill",     "Recaudación del día y por turno"),
            new(PreciosGrupo,       "Cambiar precios en grupo",  "bi-collection-fill",         "Aumentos y cambios de precio a varios productos juntos"),
            new(VentaRapida,        "Venta rápida y combos",     "bi-lightning-charge-fill",   "Botones de venta rápida y armado de combos"),
            new(Fiado,              "Clientes y fiado",          "bi-journal-bookmark-fill",   "Vender fiado, deudas de clientes y cobro de deudas"),
            new(ReportesProductos,  "Reportes de productos",     "bi-clipboard-data-fill",     "Precios viejos, poca venta y sin ventas"),
            new(ComprasMayorista,   "Compras al mayorista",      "bi-basket2-fill",            "Calculadora y registro de compras al mayorista"),
            new(PedidosProveedores, "Pedidos a proveedores",     "bi-truck",                   "Armar pedidos y recibir mercadería"),
            new(DeudasProveedores,  "Deudas con proveedores",    "bi-cash-coin",               "Cuánto se le debe a cada proveedor y las entregas"),
            new(Asistencia,         "Asistencia del personal",   "bi-person-check-fill",       "Fichaje de entrada y salida de empleados"),
            new(Encargado,          "Rol encargado/a",           "bi-person-badge",            "Usuario de confianza: el dueño elige qué puede hacer"),
            new(Tareas,             "Tareas del personal",       "bi-list-check",              "Cada empleado anota lo que hizo en el día"),
        };

        public static Funcion? Buscar(string? clave) => Todas.FirstOrDefault(f => f.Clave == clave);

        /// <summary>Páginas de cada función (se compara el comienzo de la dirección).</summary>
        private static readonly (string Ruta, string Clave)[] Rutas =
        {
            ("/carteles", Carteles),
            ("/importar-excel", ImportarExcel),
            ("/producto/importar-excel", ImportarExcel),
            ("/reportebasico", ReporteDia),
            ("/precios-en-lote", PreciosGrupo),
            ("/gestion-venta-rapida", VentaRapida),
            ("/gestion-combo", VentaRapida),
            ("/reportedeudores", Fiado),
            ("/cobrar-deuda", Fiado),
            ("/reportes-productos", ReportesProductos),
            ("/compras-mayorista", ComprasMayorista),
            ("/calculadora-mayorista", ComprasMayorista),
            ("/listado-pedidos-mayorista", ComprasMayorista),
            ("/gestor-pedidos-proveedor", PedidosProveedores),
            ("/compra-mercaderia", PedidosProveedores),
            ("/asistencia", Asistencia),
            ("/tareas", Tareas),
            ("/reporte-tareas", Tareas),
        };

        /// <summary>Qué función necesita una dirección (null = ninguna, está siempre).</summary>
        public static string? ClaveDeRuta(string? rutaRelativa)
        {
            var r = "/" + (rutaRelativa ?? "").Split('?', '#')[0].Trim('/').ToLowerInvariant();
            foreach (var (ruta, clave) in Rutas)
                if (r.StartsWith(ruta)) return clave;   // incluye las variantes "-desktop" y con parámetros
            return null;
        }

        /// <summary>Las funciones efectivas: las del plan + extra - quitadas.</summary>
        public static HashSet<string> Calcular(IEnumerable<string>? delPlan, IEnumerable<string>? extra, IEnumerable<string>? quitadas)
        {
            var set = new HashSet<string>(delPlan ?? Enumerable.Empty<string>());
            set.UnionWith(extra ?? Enumerable.Empty<string>());
            set.ExceptWith(quitadas ?? Enumerable.Empty<string>());
            return set;
        }
    }
}
