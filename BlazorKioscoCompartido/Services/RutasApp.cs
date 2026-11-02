namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Direcciones de las páginas. El celular y el escritorio usan las mismas;
    /// se dejan separadas por si algún día una app necesita una distinta.
    /// </summary>
    public sealed record RutasApp
    {
        public string Vender { get; init; } = "/producto/vender";
        public string Productos { get; init; } = "/productos";
        public string NuevoProducto { get; init; } = "/producto/nuevo";
        public string ImportarExcel { get; init; } = "/importar-excel";
        public string ReporteDiario { get; init; } = "/reportebasico-recaudacion";
        public string Deudas { get; init; } = "/reportedeudores";

        // Iguales en las dos aplicaciones
        public string VentaRapida { get; init; } = "/gestion-venta-rapida";
        public string Carteles { get; init; } = "/carteles";
        public string PreciosEnGrupo { get; init; } = "/precios-en-lote";
        public string ReportesProductos { get; init; } = "/reportes-productos";
        public string Tareas { get; init; } = "/tareas";
        public string ReporteTareas { get; init; } = "/reporte-tareas";
        public string Asistencia { get; init; } = "/asistencia";
        public string ComprasMayorista { get; init; } = "/compras-mayorista";
        public string PedidosProveedor { get; init; } = "/gestor-pedidos-proveedor";
        public string CuentasProveedores { get; init; } = "/cuentas-proveedores";
        public string Clientes { get; init; } = "/configuracion?tab=clientes";
        public string Configuracion { get; init; } = "/configuracion";
        public string AdminNegocios { get; init; } = "/superadmin/negocios";
        public string Login { get; init; } = "/login";

        public static readonly RutasApp Celular = new();

        // El escritorio ahora tiene las mismas direcciones que el celular
        // (las viejas con "-desktop" siguen existiendo para que nada se rompa).
        public static readonly RutasApp Escritorio = new();
    }
}
