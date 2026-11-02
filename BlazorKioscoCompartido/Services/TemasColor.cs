namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Colores que el dueño puede elegir para su negocio (barra de arriba, login, botones principales).
    /// Se guarda la Clave en kioscos.color_tema.
    /// </summary>
    public static class TemasColor
    {
        public sealed record Tema(string Clave, string Nombre, string Principal, string Oscuro, string Suave);

        public const string PorDefecto = "azul";

        public static readonly IReadOnlyList<Tema> Todos = new List<Tema>
        {
            new("azul",     "Azul (el de siempre)", "#0d6efd", "#0a58ca", "#e7f0ff"),
            new("celeste",  "Celeste",              "#0284c7", "#0369a1", "#e0f2fe"),
            new("turquesa", "Turquesa",             "#0d9488", "#0f766e", "#ccfbf1"),
            new("verde",    "Verde",                "#16a34a", "#15803d", "#dcfce7"),
            new("rosa",     "Rosa",                 "#db2777", "#be185d", "#fce7f3"),
            new("fucsia",   "Fucsia",               "#c026d3", "#a21caf", "#fae8ff"),
            new("violeta",  "Violeta",              "#7c3aed", "#6d28d9", "#ede9fe"),
            new("naranja",  "Naranja",              "#ea580c", "#c2410c", "#ffedd5"),
            new("rojo",     "Rojo",                 "#dc2626", "#b91c1c", "#fee2e2"),
            new("grafito",  "Gris oscuro",          "#334155", "#1e293b", "#e2e8f0"),
        };

        public static Tema Buscar(string? clave) =>
            Todos.FirstOrDefault(t => string.Equals(t.Clave, clave?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? Todos[0];

        /// <summary>"#0d6efd" → "13, 110, 253" (lo que usa Bootstrap en sus variables -rgb).</summary>
        public static string Rgb(string hex)
        {
            hex = hex.TrimStart('#');
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
            return $"{r}, {g}, {b}";
        }
    }
}
