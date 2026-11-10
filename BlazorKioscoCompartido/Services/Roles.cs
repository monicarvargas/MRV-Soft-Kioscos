namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Roles y permisos.
    ///   superadmin : todo, y además administra los negocios.
    ///   admin      : el dueño. Todo lo del negocio.
    ///   encargado  : por defecto todo, MENOS la recaudación del día, las deudas (fiado) y los usuarios.
    ///   vendedor   : por defecto vender, productos, carteles y sus tareas.
    /// El dueño puede sumarle o sacarle a cada perfil las funciones de <see cref="Configurables"/>
    /// (Configuración → Ayuda y plan → Perfiles). Se guarda en kioscos.permisos_roles (script 38).
    /// Lo que es SOLO del dueño (usuarios, redondeo, copia de seguridad, perfiles) no se puede dar.
    /// </summary>
    public static class Roles
    {
        public const string SuperAdmin = "superadmin";
        public const string Admin = "admin";
        public const string Encargado = "encargado";
        public const string Vendedor = "vendedor";

        // ---------------- permisos que el dueño puede dar o sacar ----------------
        public const string PRecaudacion = "recaudacion";
        public const string PFiado = "fiado";
        public const string PMercaderia = "mercaderia";
        public const string PPrecios = "precios";
        public const string PVentaRapida = "venta_rapida";
        public const string PReportes = "reportes";
        public const string PPersonal = "personal";
        public const string PConfiguracion = "configuracion";
        public const string PEditarProductos = "editar_productos";

        public sealed record Permiso(string Clave, string Nombre, string Icono, string Detalle);

        /// <summary>Lo que se puede dar o sacar a un perfil, en el orden en que se muestra.</summary>
        public static readonly IReadOnlyList<Permiso> Configurables = new List<Permiso>
        {
            new(PRecaudacion,   "Recaudación del día",           "bi-calendar-check-fill",    "Ver cuánta plata entró: total, por forma de pago, por turno y por empleado"),
            new(PFiado,         "Deudas de clientes (fiado)",    "bi-journal-bookmark-fill",  "Ver quién debe y cuánto, y cobrar las deudas"),
            new(PMercaderia,    "Compras y pedidos",             "bi-truck",                  "Compras al mayorista, pedidos a proveedores, recibir mercadería y cuentas con proveedores"),
            new(PPrecios,       "Cambiar precios",               "bi-collection-fill",        "Cambiar precios en grupo e importar productos desde Excel"),
            new(PVentaRapida,   "Armar venta rápida y combos",   "bi-lightning-charge-fill",  "Crear y cambiar los botones de venta rápida y los combos"),
            new(PReportes,      "Reportes de productos",         "bi-clipboard-data-fill",    "Precios viejos, poca venta y productos sin ventas"),
            new(PPersonal,      "Personal",                      "bi-person-check-fill",      "Asistencia de todos los empleados y reporte de tareas"),
            new(PConfiguracion, "Configuración del negocio",     "bi-gear-fill",              "Clientes, proveedores, familias, listas de precios, formas de pago, turnos, unidades y color"),
            new(PEditarProductos, "Modificar productos",         "bi-pencil-square",          "Cambiar precios, stock y datos de los productos que ya existen. Si lo apagás, los ve pero no los puede cambiar"),
        };

        /// <summary>Lo que trae cada perfil si el dueño no cambió nada.</summary>
        public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> PorDefecto = new Dictionary<string, IReadOnlyList<string>>
        {
            [Encargado] = new[] { PMercaderia, PPrecios, PVentaRapida, PReportes, PPersonal, PConfiguracion, PEditarProductos },
            [Vendedor] = new[] { PEditarProductos },
        };

        /// <summary>
        /// Permisos nuevos que vienen PRENDIDOS: los negocios que ya habían guardado sus perfiles antes
        /// de que existieran los siguen teniendo. Para apagarlos se guarda "!clave" en la lista.
        /// </summary>
        private static readonly string[] DefectoSi = { PEditarProductos };

        /// <summary>De lo guardado en la base a lo que tiene de verdad el perfil.</summary>
        public static HashSet<string> DesdeGuardado(IEnumerable<string>? lista)
        {
            var l = (lista ?? Array.Empty<string>()).ToList();
            var r = new HashSet<string>(l.Where(x => !x.StartsWith("!")));
            foreach (var k in DefectoSi)
                if (!l.Contains("!" + k)) r.Add(k);
            return r;
        }

        /// <summary>De lo que tiene el perfil a lo que se guarda en la base (anota los apagados de <see cref="DefectoSi"/>).</summary>
        public static List<string> ParaGuardar(IEnumerable<string> permisos)
        {
            var r = permisos.Where(x => !x.StartsWith("!")).Distinct().OrderBy(x => x).ToList();
            foreach (var k in DefectoSi)
                if (!r.Contains(k)) r.Add("!" + k);
            return r;
        }

        // lo que eligió el dueño de ESTE negocio (null = lo de por defecto). Se carga al entrar.
        private static Dictionary<string, HashSet<string>>? _elegidos;

        /// <summary>Carga lo que eligió el dueño (kioscos.permisos_roles). null o vacío = lo de por defecto.</summary>
        public static void CargarPermisos(Dictionary<string, List<string>>? elegidos)
        {
            _elegidos = elegidos == null ? null
                : elegidos.ToDictionary(x => N(x.Key), x => DesdeGuardado(x.Value));
        }

        /// <summary>"encargado" o "vendedor" (cajero y cualquier otro cuentan como vendedor).</summary>
        public static string Perfil(string? rol) => EsEncargado(rol) ? Encargado : Vendedor;

        /// <summary>Los permisos que tiene hoy ese perfil en este negocio.</summary>
        public static HashSet<string> PermisosDe(string perfil)
        {
            var p = N(perfil);
            if (_elegidos != null && _elegidos.TryGetValue(p, out var elegidos)) return new HashSet<string>(elegidos);
            return new HashSet<string>(PorDefecto.TryGetValue(p, out var def) ? def : Array.Empty<string>());
        }

        /// <summary>¿Este usuario puede usar esa parte? El dueño (y el superadmin) siempre.</summary>
        public static bool Puede(string? rol, string permiso) => EsAdmin(rol) || PermisosDe(Perfil(rol)).Contains(permiso);

        private static string N(string? rol) => (rol ?? "").Trim().ToLowerInvariant();

        public static bool EsSuperAdmin(string? rol) => N(rol) == SuperAdmin;
        public static bool EsEncargado(string? rol) => N(rol) is "encargado" or "encargada" or "encargado/a";

        /// <summary>Dueño o superadmin.</summary>
        public static bool EsAdmin(string? rol) => N(rol) is SuperAdmin or Admin;

        /// <summary>Ve lo que entró de plata (Recaudación del día / por turno). Por defecto solo el dueño.</summary>
        public static bool VeRecaudacion(string? rol) => Puede(rol, PRecaudacion);

        /// <summary>Ve y cobra las deudas de los clientes (fiado). Por defecto solo el dueño.</summary>
        public static bool VeFiado(string? rol) => Puede(rol, PFiado);

        /// <summary>Puede cambiar un producto que ya existe (si no, Editar producto queda solo para ver). Por defecto todos.</summary>
        public static bool EditaProductos(string? rol) => Puede(rol, PEditarProductos);

        /// <summary>Crea / cambia usuarios. Solo el dueño (así un encargado no se puede hacer admin).</summary>
        public static bool AdministraUsuarios(string? rol) => EsAdmin(rol);

        /// <summary>Dueño o encargado (para lo que no se configura por perfil, ej: crear códigos de barras).</summary>
        public static bool Gestiona(string? rol) => EsAdmin(rol) || EsEncargado(rol);

        /// <summary>Qué permiso necesita cada página (se compara el comienzo de la dirección).</summary>
        private static readonly (string Ruta, string Permiso)[] RutasPermiso =
        {
            ("/reportebasico", PRecaudacion),
            ("/reportedeudores", PFiado),
            ("/cobrar-deuda", PFiado),
            ("/compras-mayorista", PMercaderia),
            ("/calculadora-mayorista", PMercaderia),
            ("/listado-pedidos-mayorista", PMercaderia),
            ("/gestor-pedidos-proveedor", PMercaderia),
            ("/compra-mercaderia", PMercaderia),
            ("/cuentas-proveedores", PMercaderia),
            ("/precios-en-lote", PPrecios),
            ("/importar-excel", PPrecios),
            ("/producto/importar-excel", PPrecios),
            ("/gestion-venta-rapida", PVentaRapida),
            ("/gestion-combo", PVentaRapida),
            ("/reportes-productos", PReportes),
            ("/asistencia", PPersonal),
            ("/reporte-tareas", PPersonal),
            ("/configuracion", PConfiguracion),
        };

        /// <summary>Qué permiso necesita una dirección (null = ninguno).</summary>
        public static string? PermisoDeRuta(string? rutaRelativa)
        {
            var r = "/" + (rutaRelativa ?? "").Split('?', '#')[0].Trim('/').ToLowerInvariant();
            foreach (var (ruta, permiso) in RutasPermiso)
                if (r.StartsWith(ruta)) return permiso;
            return null;
        }

        public static Permiso? BuscarPermiso(string? clave) => Configurables.FirstOrDefault(p => p.Clave == clave);

        public static string Legible(string? rol) => N(rol) switch
        {
            "superadmin" => "Superadmin",
            "admin" => "Administrador/a",
            "vendedor" or "vendedora" or "vendedor/a" => "Vendedor/a",
            "cajero" or "cajera" or "cajero/a" => "Cajero/a",
            "encargado" or "encargada" or "encargado/a" => "Encargado/a",
            "" => "Vendedor/a",
            var otro => char.ToUpper(otro[0]) + otro[1..]
        };
    }
}
