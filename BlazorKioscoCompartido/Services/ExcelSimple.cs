using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Arma un Excel (.xlsx) con varias hojas, sin librerías (sirve en el celular y en la PC).
    /// Los números quedan como números (se pueden sumar en Excel); el resto como texto.
    /// </summary>
    public static class ExcelSimple
    {
        public sealed class Hoja
        {
            public string Nombre { get; set; } = "Hoja";
            public List<string> Columnas { get; set; } = new();
            public List<object?[]> Filas { get; set; } = new();
        }

        public static byte[] Crear(IEnumerable<Hoja> hojasEntrada)
        {
            var hojas = hojasEntrada.ToList();
            if (hojas.Count == 0) hojas.Add(new Hoja { Nombre = "Vacío" });
            var nombres = NombresUnicos(hojas.Select(h => h.Nombre).ToList());

            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
            {
                void Escribir(string ruta, string contenido)
                {
                    var e = zip.CreateEntry(ruta, CompressionLevel.Fastest);
                    using var w = new StreamWriter(e.Open(), new UTF8Encoding(false));
                    w.Write(contenido);
                }

                var ct = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                    "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                    "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                    "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
                for (int i = 1; i <= hojas.Count; i++)
                    ct.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
                ct.Append("</Types>");
                Escribir("[Content_Types].xml", ct.ToString());

                Escribir("_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                    "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");

                var wb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
                var rels = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
                for (int i = 1; i <= hojas.Count; i++)
                {
                    wb.Append($"<sheet name=\"{Xml(nombres[i - 1])}\" sheetId=\"{i}\" r:id=\"rId{i}\"/>");
                    rels.Append($"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
                }
                wb.Append("</sheets></workbook>");
                rels.Append($"<Relationship Id=\"rId{hojas.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
                Escribir("xl/workbook.xml", wb.ToString());
                Escribir("xl/_rels/workbook.xml.rels", rels.ToString());

                // estilos: 0 = normal, 1 = encabezado en negrita con fondo gris
                Escribir("xl/styles.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                    "<fonts count=\"2\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font><font><b/><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                    "<fills count=\"3\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
                    "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFE9ECF2\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
                    "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                    "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                    "<cellXfs count=\"2\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/>" +
                    "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\"/></cellXfs>" +
                    "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>");

                for (int i = 0; i < hojas.Count; i++)
                    Escribir($"xl/worksheets/sheet{i + 1}.xml", HojaXml(hojas[i]));
            }
            return ms.ToArray();
        }

        private static string HojaXml(Hoja h)
        {
            var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
            // encabezado fijo arriba
            sb.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
            if (h.Columnas.Count > 0)
            {
                sb.Append("<cols>");
                for (int c = 0; c < h.Columnas.Count; c++)
                {
                    int largo = Math.Max(h.Columnas[c].Length, h.Filas.Take(200).Select(f => c < f.Length ? Texto(f[c]).Length : 0).DefaultIfEmpty(0).Max());
                    sb.Append($"<col min=\"{c + 1}\" max=\"{c + 1}\" width=\"{Math.Clamp(largo + 2, 8, 60)}\" customWidth=\"1\"/>");
                }
                sb.Append("</cols>");
            }
            sb.Append("<sheetData>");
            sb.Append("<row r=\"1\">");
            for (int c = 0; c < h.Columnas.Count; c++)
                sb.Append($"<c r=\"{Ref(c, 1)}\" t=\"inlineStr\" s=\"1\"><is><t>{Xml(h.Columnas[c])}</t></is></c>");
            sb.Append("</row>");
            for (int r = 0; r < h.Filas.Count; r++)
            {
                int nro = r + 2;
                sb.Append($"<row r=\"{nro}\">");
                var fila = h.Filas[r];
                for (int c = 0; c < fila.Length; c++)
                {
                    var v = fila[c];
                    if (v == null) continue;
                    if (v is decimal or double or float or int or long)
                        sb.Append($"<c r=\"{Ref(c, nro)}\"><v>{Convert.ToDecimal(v).ToString(CultureInfo.InvariantCulture)}</v></c>");
                    else
                    {
                        var t = Texto(v);
                        if (t.Length > 32000) t = t[..32000];
                        sb.Append($"<c r=\"{Ref(c, nro)}\" t=\"inlineStr\"><is><t xml:space=\"preserve\">{Xml(t)}</t></is></c>");
                    }
                }
                sb.Append("</row>");
            }
            sb.Append("</sheetData>");
            if (h.Columnas.Count > 0) sb.Append($"<autoFilter ref=\"A1:{Ref(h.Columnas.Count - 1, Math.Max(1, h.Filas.Count + 1))}\"/>");
            sb.Append("</worksheet>");
            return sb.ToString();
        }

        private static string Texto(object? v) => v switch
        {
            null => "",
            bool b => b ? "SI" : "NO",
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            _ => v.ToString() ?? ""
        };

        private static string Ref(int col, int fila)
        {
            var s = "";
            for (int c = col + 1; c > 0; c = (c - 1) / 26) s = (char)('A' + (c - 1) % 26) + s;
            return s + fila;
        }

        /// <summary>Saca caracteres que Excel no acepta y escapa &lt; &gt; &amp;.</summary>
        private static string Xml(string s)
        {
            var limpio = new string((s ?? "").Where(ch => ch == '\t' || ch == '\n' || ch == '\r' || ch >= ' ').ToArray());
            return SecurityElement.Escape(limpio) ?? "";
        }

        /// <summary>Nombres de hoja: máximo 31 letras, sin []:*?/\ y sin repetir.</summary>
        private static List<string> NombresUnicos(List<string> nombres)
        {
            var usados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var res = new List<string>();
            foreach (var n0 in nombres)
            {
                var n = new string((n0 ?? "Hoja").Where(ch => !"[]:*?/\\".Contains(ch)).ToArray()).Trim();
                if (n == "") n = "Hoja";
                if (n.Length > 31) n = n[..31];
                var baseN = n; int i = 2;
                while (!usados.Add(n)) { var suf = $" {i++}"; n = (baseN.Length + suf.Length > 31 ? baseN[..(31 - suf.Length)] : baseN) + suf; }
                res.Add(n);
            }
            return res;
        }
    }
}
