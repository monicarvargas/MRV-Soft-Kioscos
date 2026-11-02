using System.Globalization;
using System.Text;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Dibuja códigos de barras (SVG) para imprimir etiquetas, sin librerías ni internet.
    ///   - 13 dígitos con dígito verificador correcto → EAN-13 (el de los productos del súper).
    ///   -  8 dígitos válidos → EAN-8.  12 dígitos válidos (UPC) → EAN-13 con un 0 adelante.
    ///   - Cualquier otro texto (letras, números de otro largo) → Code 128.
    /// También crea "códigos propios" para productos sueltos (gomitas, fiambre, etc.): empiezan con 29.
    /// </summary>
    public static class CodigoBarras
    {
        public sealed record Dibujo(string Tipo, string Texto, List<(int Desde, int Ancho)> Barras, int Modulos);

        // ---------------------------------------------------------------- EAN
        private static readonly string[] L = { "0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011" };
        private static readonly string[] R = { "1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100" };
        private static readonly string[] Paridad = { "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG", "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL" };
        private static string G(int d) => new string(R[d].Reverse().ToArray());

        /// <summary>Dígito verificador EAN/UPC de los dígitos dados (sin el verificador).</summary>
        public static int Verificador(string digitos)
        {
            int suma = 0;
            for (int i = 0; i < digitos.Length; i++)
            {
                int d = digitos[digitos.Length - 1 - i] - '0';
                suma += i % 2 == 0 ? d * 3 : d;
            }
            return (10 - suma % 10) % 10;
        }

        private static bool SoloDigitos(string s) => s.Length > 0 && s.All(char.IsDigit);
        private static bool EanValido(string s) => SoloDigitos(s) && Verificador(s[..^1]) == s[^1] - '0';

        private static string Ean13Bits(string c)
        {
            var sb = new StringBuilder("101");
            var par = Paridad[c[0] - '0'];
            for (int i = 1; i <= 6; i++) sb.Append(par[i - 1] == 'L' ? L[c[i] - '0'] : G(c[i] - '0'));
            sb.Append("01010");
            for (int i = 7; i <= 12; i++) sb.Append(R[c[i] - '0']);
            sb.Append("101");
            return sb.ToString();
        }

        private static string Ean8Bits(string c)
        {
            var sb = new StringBuilder("101");
            for (int i = 0; i < 4; i++) sb.Append(L[c[i] - '0']);
            sb.Append("01010");
            for (int i = 4; i < 8; i++) sb.Append(R[c[i] - '0']);
            sb.Append("101");
            return sb.ToString();
        }

        // ---------------------------------------------------------------- CODE 128 (juego B)
        private static readonly string[] C128 =
        {
            "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
            "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
            "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
            "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
            "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
            "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
            "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
            "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
            "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
            "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
            "114131","311141","411131","211412","211214","211232","2331112"
        };

        private static string? Code128Bits(string texto)
        {
            if (texto.Any(ch => ch < 32 || ch > 126)) return null;
            var valores = new List<int> { 104 };                     // START B
            valores.AddRange(texto.Select(ch => ch - 32));
            int suma = 104;
            for (int i = 1; i < valores.Count; i++) suma += valores[i] * i;
            valores.Add(suma % 103);                                  // verificador
            valores.Add(106);                                         // STOP

            var sb = new StringBuilder();
            foreach (var v in valores)
            {
                var anchos = C128[v];
                for (int i = 0; i < anchos.Length; i++)
                    sb.Append(i % 2 == 0 ? '1' : '0', anchos[i] - '0');
            }
            return sb.ToString();
        }

        // ---------------------------------------------------------------- DIBUJO
        /// <summary>Arma las barras para ese código. null = no se puede (vacío o con caracteres raros).</summary>
        public static Dibujo? Armar(string? codigo)
        {
            var c = (codigo ?? "").Trim();
            if (c == "") return null;

            string tipo; string? bits; string texto = c;
            if (c.Length == 13 && EanValido(c)) { tipo = "EAN-13"; bits = Ean13Bits(c); }
            else if (c.Length == 12 && EanValido(c)) { tipo = "EAN-13"; texto = "0" + c; bits = Ean13Bits(texto); }
            else if (c.Length == 8 && EanValido(c)) { tipo = "EAN-8"; bits = Ean8Bits(c); }
            else { tipo = "Code 128"; bits = Code128Bits(c); }
            if (bits == null) return null;

            var barras = new List<(int, int)>();
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] != '1') continue;
                int j = i;
                while (j < bits.Length && bits[j] == '1') j++;
                barras.Add((i, j - i));
                i = j - 1;
            }
            return new Dibujo(tipo, texto, barras, bits.Length);
        }

        /// <summary>
        /// SVG listo para poner en la página (se estira al ancho del contenedor).
        /// Deja 10 módulos de margen blanco a cada lado, que los lectores necesitan.
        /// </summary>
        public static string? Svg(string? codigo)
        {
            var d = Armar(codigo);
            if (d == null) return null;
            const int margen = 10;
            int ancho = d.Modulos + margen * 2;
            var sb = new StringBuilder();
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {ancho} 40\" preserveAspectRatio=\"none\" shape-rendering=\"crispEdges\" style=\"width:100%;height:100%;display:block\">");
            sb.Append($"<rect width=\"{ancho}\" height=\"40\" fill=\"#fff\"/>");
            foreach (var (desde, w) in d.Barras)
                sb.Append($"<rect x=\"{(desde + margen).ToString(CultureInfo.InvariantCulture)}\" width=\"{w}\" height=\"40\" fill=\"#000\"/>");
            sb.Append("</svg>");
            return sb.ToString();
        }

        /// <summary>Texto que va debajo de las barras (el EAN-13 de 12 dígitos lleva el 0 adelante).</summary>
        public static string TextoDebajo(string? codigo) => Armar(codigo)?.Texto ?? (codigo ?? "");

        // ---------------------------------------------------------------- CÓDIGO PROPIO
        /// <summary>
        /// Código propio para un producto que no trae código (EAN-13 que empieza con 29 = uso interno del negocio).
        /// 29 + 3 dígitos del negocio + 7 al azar + verificador. Hay que confirmar que no exista antes de usarlo.
        /// </summary>
        public static string NuevoCodigoPropio(long kioscoId)
        {
            var azar = Random.Shared.Next(0, 10_000_000).ToString("D7");
            var doce = "29" + (kioscoId % 1000).ToString("D3") + azar;
            return doce + Verificador(doce);
        }

        /// <summary>Crea un código propio que no esté usado por otro producto del negocio.</summary>
        public static async Task<string> CrearCodigoPropioAsync(Supabase.Client db, long kioscoId)
        {
            for (int intento = 0; intento < 10; intento++)
            {
                var codigo = NuevoCodigoPropio(kioscoId);
                var res = await db.From<Models.Producto>()
                    .Select("id")
                    .Where(p => p.KioscoId == kioscoId)
                    .Where(p => p.CodigoBarras == codigo)
                    .Get();
                if (!(res.Models?.Any() ?? false)) return codigo;
            }
            throw new Exception("No se pudo crear un código nuevo. Probá de nuevo.");
        }

        /// <summary>¿Es un código creado por el sistema (empieza con 29)?</summary>
        public static bool EsCodigoPropio(string? codigo) => (codigo ?? "").Trim() is { Length: 13 } c && c.StartsWith("29") && EanValido(c);
    }
}
