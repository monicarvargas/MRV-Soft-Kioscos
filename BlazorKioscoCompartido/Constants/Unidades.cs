namespace BlazorKioscoCompartido.Constants
{
    /// <summary>
    /// Unidades en que se puede vender un producto. Cada negocio elige cuáles usa
    /// (Configuración → Unidades; columna kioscos.unidades, ver _sql\24.unidades_negocio.sql).
    /// "Unidad" está siempre.
    /// </summary>
    public static class Unidades
    {
        public const string Unidad = "UNIDAD";
        public const string Kilo = "KG";
        public const string Gramos = "GR";
        public const string Litros = "LITROS";
        public const string Metros = "METROS";
        public const string Bulto = "BULTO";

        /// <summary>Todas las que existen, en el orden en que se muestran.</summary>
        public static readonly List<string> Lista = new() { Unidad, Kilo, Gramos, Litros, Metros, Bulto };

        /// <summary>Las que tiene un negocio que todavía no eligió nada.</summary>
        public static readonly List<string> PorDefecto = new() { Unidad, Kilo, Gramos };

        private static bool Igual(string? a, string b) => string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);

        /// <summary>"Unidad" o vacío / "U" (el valor viejo por defecto de la base).</summary>
        public static bool EsUnidad(string? u) => string.IsNullOrWhiteSpace(u) || Igual(u, Unidad) || Igual(u, "U");

        /// <summary>Las habilitadas en el negocio (siempre con "Unidad"; si no eligió nada, las de por defecto).</summary>
        public static List<string> Habilitadas(IEnumerable<string>? delNegocio)
        {
            var elegidas = delNegocio?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (elegidas == null || elegidas.Count == 0) return PorDefecto.ToList();
            return Lista.Where(u => u == Unidad || elegidas.Any(e => Igual(e, u))).ToList();
        }

        /// <summary>
        /// Para el selector "Se vende por" de un producto: las habilitadas, y además la que ya tiene el producto
        /// (si el negocio la sacó, el producto no la pierde al editarlo).
        /// </summary>
        public static List<string> ParaElegir(IEnumerable<string>? delNegocio, string? actual)
        {
            var lista = Habilitadas(delNegocio);
            if (!EsUnidad(actual) && !lista.Any(u => Igual(actual, u))) lista.Add(actual!.Trim().ToUpper());
            return lista;
        }

        public static string Nombre(string? u) => (u ?? "").Trim().ToUpper() switch
        {
            "UNIDAD" or "U" or "" => "Unidad",
            "KG" => "Kilo",
            "GR" => "Gramos",
            "LITROS" => "Litros",
            "METROS" => "Metros",
            "BULTO" => "Bultos",
            var otro => otro
        };

        public static string Icono(string? u) => (u ?? "").Trim().ToUpper() switch
        {
            "UNIDAD" or "U" or "" => "bi-box",
            "KG" => "bi-basket-fill",
            "GR" => "bi-speedometer2",
            "LITROS" => "bi-droplet-fill",
            "METROS" => "bi-rulers",
            "BULTO" => "bi-boxes",
            _ => "bi-rulers"
        };

        public static string Ejemplo(string u) => u switch
        {
            Unidad => "Se cuenta de a uno (un alfajor, una gaseosa).",
            Kilo => "Por peso, el precio es por kilo (fiambres, verdura).",
            Gramos => "Por peso: el costo es por kilo y el precio se muestra cada 100 g (golosinas sueltas).",
            Litros => "Por litro (lavandina, productos de limpieza sueltos).",
            Metros => "Por metro (cable, cadena, manguera).",
            Bulto => "Por bulto o caja cerrada.",
            _ => ""
        };
    }
}
