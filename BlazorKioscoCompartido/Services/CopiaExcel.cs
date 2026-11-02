using Newtonsoft.Json.Linq;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Arma el Excel de "Copia de seguridad" de un negocio (lo usan el botón "Hacer copia" y la copia automática de la PC).
    /// Necesita _sql\29.exportar_negocio.sql.
    /// </summary>
    public static class CopiaExcel
    {
        public sealed record Resultado(byte[] Bytes, List<string> Resumen);

        public static async Task<Resultado> GenerarAsync(Supabase.Client db, long kioscoId, int dias)
        {
            var res = await db.Rpc("exportar_negocio", new Dictionary<string, object> { { "p_kiosco", kioscoId }, { "p_dias", dias } });
            var json = JObject.Parse(string.IsNullOrWhiteSpace(res.Content) ? "{}" : res.Content);

            var hojas = new List<ExcelSimple.Hoja>();
            var resumen = new List<string>();
            foreach (var prop in json.Properties())
            {
                var hoja = new ExcelSimple.Hoja { Nombre = prop.Name };
                var filas = prop.Value as JArray ?? new JArray();
                // columnas: las de la primera fila (vienen en orden); si no hay filas, la hoja queda vacía
                if (filas.FirstOrDefault() is JObject primera) hoja.Columnas = primera.Properties().Select(p => p.Name).ToList();
                foreach (var f in filas.OfType<JObject>())
                    hoja.Filas.Add(hoja.Columnas.Select(c => Valor(f[c])).ToArray());
                hojas.Add(hoja);
                resumen.Add($"{prop.Name}: {filas.Count}");
            }
            return new Resultado(ExcelSimple.Crear(hojas), resumen);
        }

        /// <summary>Nombre del archivo: "Copia KIOSCO X 2026-10-25 2130.xlsx".</summary>
        public static string NombreArchivo(string? negocio, DateTime cuando) =>
            $"Copia {Limpio(negocio ?? "negocio")} {cuando:yyyy-MM-dd HHmm}.xlsx";

        /// <summary>Mensaje entendible para el error de "falta el script".</summary>
        public static string Mensaje(Exception ex)
        {
            var msg = ex.Message ?? "";
            return msg.Contains("exportar_negocio") || msg.Contains("PGRST202")
                ? "Falta preparar la base: hay que correr el script 29.exportar_negocio.sql en Supabase."
                : msg;
        }

        private static object? Valor(JToken? t) => t?.Type switch
        {
            null or JTokenType.Null or JTokenType.Undefined => null,
            JTokenType.Integer or JTokenType.Float => t.Value<decimal>(),
            JTokenType.Boolean => t.Value<bool>() ? "SI" : "NO",
            _ => t.ToString()
        };

        private static string Limpio(string s) =>
            new string(s.Where(ch => !"\\/:*?\"<>|".Contains(ch) && ch >= ' ').ToArray()).Trim();
    }
}
