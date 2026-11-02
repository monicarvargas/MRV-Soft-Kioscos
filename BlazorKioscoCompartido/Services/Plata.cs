using System.Globalization;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Plata como se escribe en Argentina: punto de miles y coma de centavos (3.000 · 1.500,50).
    /// Va junto con los campos que tienen la clase "js-monto" (el punto se pone solo mientras se escribe;
    /// ver el script en index.html) y con el componente InputMonto.
    /// </summary>
    public static class Plata
    {
        public static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

        /// <summary>3000 → "3.000" · 1500,5 → "1.500,50" · null → "".</summary>
        public static string Texto(decimal? v) =>
            v is decimal d ? d.ToString(d == Math.Truncate(d) ? "#,0" : "#,0.00", Ar) : "";

        /// <summary>Lee "3.000", "1.500,50", "1500,5" o "$ 3.000" (el punto es de miles; la coma, de centavos).</summary>
        public static bool TryLeer(object? valor, out decimal resultado)
        {
            var s = (valor?.ToString() ?? "").Replace("$", "").Replace(" ", "").Trim();
            s = s.Replace(".", "").Replace(',', '.');
            return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out resultado);
        }
    }
}
