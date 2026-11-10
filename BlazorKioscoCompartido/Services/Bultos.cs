using System.Globalization;
using BlazorKioscoCompartido.Models;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// TODO lo del bulto en un solo lugar (cuentas, textos y lo que se escribe en los campos).
    /// El tamaño del bulto se guarda siempre como número entero:
    ///   - producto por unidad: UNIDADES que trae (caja de 48 alfajores = 48)
    ///   - producto por peso (Kilo / Gramos): GRAMOS que trae (bolsa de 15 kg = 15000), igual que el stock.
    /// 1 (o menos) = sin bulto. El costo del producto es por unidad, o por KILO si es por peso.
    /// Se usa en: productos.bulto_unidades, productos_proveedores.unidades_por_bulto,
    /// listas_pedido_detalle.unidades_por_bulto y compras_mayorista_detalle.unidades_por_bulto.
    /// Para escribirlo en pantalla: componente &lt;InputBulto&gt;.
    /// </summary>
    public static class Bultos
    {
        private static readonly CultureInfo Ar = CultureInfo.GetCultureInfo("es-AR");

        /// <summary>¿El bulto de este producto va en kilos? (se vende por Kilo o por Gramos)</summary>
        public static bool EnKilos(Producto? producto) => producto.EsVentaPorPeso();

        /// <summary>Lo mínimo que es un bulto: 2 unidades o 100 g (menos es un dato viejo o sin cargar).</summary>
        public static int Minimo(bool enKilos) => enKilos ? 100 : 2;

        /// <summary>¿Tiene un bulto cargado que sirva?</summary>
        public static bool Tiene(int? guardado, bool enKilos) => (guardado ?? 0) >= Minimo(enKilos);

        /// <summary>Lo que trae el bulto en la unidad del costo: unidades, o KILOS. 0 = sin bulto.</summary>
        public static decimal Factor(int? guardado, bool enKilos) =>
            (guardado ?? 0) <= 1 ? 0m : enKilos ? guardado!.Value / 1000m : guardado!.Value;

        /// <summary>Costo del bulto = costo por unidad (o por kilo) × lo que trae.</summary>
        public static decimal CostoDelBulto(decimal costoUnidad, int? guardado, bool enKilos, int decimales = 2) =>
            Math.Round(costoUnidad * Factor(guardado, enKilos), decimales);

        /// <summary>Costo por unidad (o por kilo) desde el costo del bulto. 0 si no hay bulto.</summary>
        public static decimal CostoPorUnidad(decimal costoBulto, int? guardado, bool enKilos, int decimales = 2)
        {
            var f = Factor(guardado, enKilos);
            return f > 0 ? Math.Round(costoBulto / f, decimales) : 0m;
        }

        /// <summary>"kg" o "u."</summary>
        public static string Unidad(bool enKilos) => enKilos ? "kg" : "u.";

        /// <summary>"por kilo" / "por unidad"</summary>
        public static string PorUnidad(bool enKilos) => enKilos ? "por kilo" : "por unidad";

        /// <summary>Gramos en kilos, sin "kg": 15000 → "15", 4350 → "4,35".</summary>
        public static string Kilos(int gramos) => (gramos / 1000m).ToString("#,##0.###", Ar);

        /// <summary>Una cantidad (unidades, o gramos si va en kilos) para leer: "48 u" / "30 kg".</summary>
        public static string TextoCantidad(int cantidad, bool enKilos) =>
            enKilos ? Kilos(cantidad) + " kg" : $"{cantidad} u";

        /// <summary>El tamaño del bulto para leer: "48 u" / "15 kg" / "4,35 kg" ("" si no tiene).</summary>
        public static string Texto(int? guardado, bool enKilos) =>
            (guardado ?? 0) <= 1 ? "" : TextoCantidad(guardado!.Value, enKilos);

        /// <summary>Lo que va en el campo para escribir: "48" / "15" / "4,35" ("" si no tiene).</summary>
        public static string ParaCampo(int? guardado, bool enKilos) =>
            (guardado ?? 0) <= 1 ? "" : enKilos ? (guardado!.Value / 1000m).ToString("0.###", Ar) : guardado!.Value.ToString();

        /// <summary>Lo escrito → lo que se guarda (unidades, o gramos). Acepta 4,35 o 4.35. null = vacío o no es un número.</summary>
        public static int? DesdeTexto(object? valor, bool enKilos)
        {
            var t = (valor?.ToString() ?? "").Trim().Replace(" ", "");
            if (t.Length == 0) return null;
            if (enKilos)
            {
                t = t.Replace(',', '.');
                return decimal.TryParse(t, NumberStyles.Number, CultureInfo.InvariantCulture, out var kg) && kg > 0
                    ? (int)Math.Round(kg * 1000m) : null;
            }
            return int.TryParse(t, out int n) && n > 0 ? n : null;
        }
    }
}
