using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using BlazorKioscoCompartido.Models;
using Newtonsoft.Json;
using static Supabase.Postgrest.Constants;

namespace BlazorKioscoCompartido.Services.SinInternet
{
    /// <summary>
    /// ETAPA 7 · Vender sin internet (solo la app de escritorio la crea; en el celular es null).
    ///
    ///  * Mientras hay internet guarda en la PC una copia de lo necesario para vender
    ///    (productos y precios, combos, venta rápida, formas de pago, listas por horario) y los usuarios que entraron.
    ///  * Sin internet: se puede entrar (usuarios que ya entraron en esta PC), buscar y vender.
    ///    Cada venta queda en un archivo en "pendientes".
    ///  * Cuando vuelve internet las sube solas (subir_venta_offline, _sql\25.ventas_sin_internet.sql):
    ///    sin duplicarse, con su hora real, y descuentan el stock.
    ///  * Sin internet NO se puede: fiar, cambiar precios, reportes, deudas, compras, importar, configuración.
    ///
    /// Se usa desde cualquier pantalla con <c>VentaSinInternet.Actual</c> (null si la app no lo tiene).
    /// </summary>
    public sealed class VentaSinInternet : IDisposable
    {
        /// <summary>La instancia de la app (null en el celular / navegador).</summary>
        public static VentaSinInternet? Actual { get; private set; }

        private readonly Supabase.Client db;
        private readonly AppState state;
        private readonly OpcionesSinInternet op;
        private readonly HttpClient http;
        private readonly System.Threading.Timer timer;
        private readonly SemaphoreSlim trabajando = new(1, 1);
        private readonly object llaveArchivos = new();
        private CopiaNegocio? copia;               // la del negocio actual, en memoria
        private bool disposed;

        private static readonly JsonSerializerSettings Json = new()
        {
            NullValueHandling = NullValueHandling.Include,
            DateParseHandling = DateParseHandling.DateTimeOffset,
            Formatting = Formatting.None
        };

        public VentaSinInternet(Supabase.Client db, AppState state, OpcionesSinInternet opciones)
        {
            this.db = db;
            this.state = state;
            op = opciones;
            http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            Directory.CreateDirectory(CarpetaPendientes);
            Directory.CreateDirectory(CarpetaSubidas);
            Actual = this;
            // primera revisión a los 5 s; después cada 20 s
            timer = new System.Threading.Timer(_ => _ = TicAsync(), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
        }

        // ============================================================ ESTADO
        /// <summary>Avisa a las pantallas que cambió algo (se cortó / volvió internet, se subieron ventas...).</summary>
        public event Action? OnCambio;

        /// <summary>No hay internet (o no se llega a la base).</summary>
        public bool SinConexion { get; private set; }

        /// <summary>El usuario entró SIN internet: hasta que vuelva a entrar con internet, solo puede vender.</summary>
        public bool SesionSinInternet { get; private set; }

        /// <summary>Solo se puede vender (sin internet, o entró sin internet).</summary>
        public bool ModoLimitado => SinConexion || SesionSinInternet;

        /// <summary>Ventas guardadas en esta PC que todavía no se subieron.</summary>
        public int Pendientes => Directory.Exists(CarpetaPendientes) ? Directory.GetFiles(CarpetaPendientes, "*.json").Length : 0;

        /// <summary>Por qué no se pudieron subir (null si anda todo bien).</summary>
        public string? ErrorSubida { get; private set; }

        /// <summary>Cuántas se subieron en la última tanda y cuándo (para el cartel "Se subieron N ventas").</summary>
        public int UltimasSubidas { get; private set; }
        public DateTime? CuandoSeSubieron { get; private set; }

        /// <summary>De cuándo es la copia de datos del negocio actual (null = todavía no hay).</summary>
        public DateTime? FechaCopia => CopiaActual()?.Fecha;

        private void Avisar() { try { OnCambio?.Invoke(); } catch { } }

        /// <summary>Una pantalla detectó que no hay internet (falló una consulta).</summary>
        public void MarcarSinConexion()
        {
            if (SinConexion) return;
            SinConexion = true;
            Avisar();
        }

        /// <summary>¿Este error es porque no hay internet / no se llega al servidor?</summary>
        public static bool EsErrorDeRed(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is HttpRequestException || e is TaskCanceledException || e is TimeoutException
                    || e is System.Net.Sockets.SocketException)
                    return true;
                var m = e.Message ?? "";
                if (m.Contains("No such host", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("network", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("No se puede establecer", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("host desconocido", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("connection", StringComparison.OrdinalIgnoreCase) && m.Contains("refused", StringComparison.OrdinalIgnoreCase)
                    || m.Contains("Failed to fetch", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        // ============================================================ CARPETAS
        private string CarpetaPendientes => Path.Combine(op.Carpeta, "pendientes");
        private string CarpetaSubidas => Path.Combine(op.Carpeta, "subidas");
        private string ArchivoCopia(long k) => Path.Combine(op.Carpeta, $"copia_negocio_{k}.json");
        private string ArchivoUsuarios => Path.Combine(op.Carpeta, "usuarios.json");
        private string ArchivoLlaves => Path.Combine(op.Carpeta, "llaves_equipo.json");

        private void EscribirSeguro(string ruta, string contenido)
        {
            lock (llaveArchivos)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
                var tmp = ruta + ".tmp";
                File.WriteAllText(tmp, contenido, Encoding.UTF8);
                File.Move(tmp, ruta, overwrite: true);   // así nunca queda un archivo a medio escribir
            }
        }

        private T? Leer<T>(string ruta) where T : class
        {
            try
            {
                lock (llaveArchivos)
                {
                    if (!File.Exists(ruta)) return null;
                    return JsonConvert.DeserializeObject<T>(File.ReadAllText(ruta, Encoding.UTF8), Json);
                }
            }
            catch { return null; }
        }

        // ============================================================ REVISIÓN PERIÓDICA
        private async Task TicAsync()
        {
            if (disposed || !await trabajando.WaitAsync(0)) return;   // si la vuelta anterior sigue, no se pisa
            try
            {
                bool hay = await HayInternetAsync();
                if (hay == SinConexion) { SinConexion = !hay; Avisar(); }
                if (!hay) return;

                if (Pendientes > 0) await SubirPendientesAsync();

                // la copia se refresca cada 15 minutos mientras hay alguien adentro con internet
                if (state.KioscoId != 0 && !SesionSinInternet && state.SesionSuperAdmin == null
                    && !Roles.EsSuperAdmin(state.UsuarioRol)
                    && (FechaCopia == null || DateTime.Now - FechaCopia.Value > TimeSpan.FromMinutes(15)))
                {
                    await GuardarCopiaAsync(state.KioscoId);
                }
            }
            catch { }
            finally { trabajando.Release(); }
        }

        private async Task<bool> HayInternetAsync()
        {
            try { if (op.HayRed != null && !op.HayRed()) return false; } catch { }
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, op.SupabaseUrl.TrimEnd('/') + "/auth/v1/health");
                req.Headers.Add("apikey", op.AnonKey);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                using var resp = await http.SendAsync(req, cts.Token);
                return true;   // respondió algo: hay internet
            }
            catch { return false; }
        }

        /// <summary>Revisa ya mismo (ej: después de vender sin internet, o al tocar "Reintentar").</summary>
        public Task RevisarAhoraAsync() => TicAsync();

        // ============================================================ COPIA DE DATOS
        /// <summary>Baja y guarda en la PC lo necesario para vender sin internet. Necesita estar logueado con internet.</summary>
        public async Task GuardarCopiaAsync(long kioscoId)
        {
            if (kioscoId == 0) return;
            try
            {
                long k = kioscoId;
                var c = new CopiaNegocio { KioscoId = k, Fecha = DateTime.Now };

                const int tam = 1000;
                for (int desde = 0; desde < 100000; desde += tam)
                {
                    var res = await db.From<Producto>()
                        .Select("id, kiosco_id, codigo_barras, nombre, marca, precio_venta, precio_lista2, precio_lista3, precio_lista4, precio_lista5, " +
                                "precio_venta_por_mayor, cant_por_mayor, venta_por_peso, unidad, activo, es_combo, fecha_vigencia_desde_combo, " +
                                "fecha_vigencia_hasta_combo, limite_unidades_combo, categoria_venta_rapida_id, mostrar_en_venta_rapida, orden_venta_rapida, admite_importe")
                        .Where(p => p.KioscoId == k)
                        .Where(p => p.Activo == true)
                        .Order("id", Ordering.Ascending)
                        .Range(desde, desde + tam - 1)
                        .Get();
                    var tanda = res.Models ?? new();
                    c.Productos.AddRange(tanda);
                    if (tanda.Count < tam) break;
                }

                var ids = c.Productos.Where(p => p.EsCombo == true).Select(p => (object)p.Id).ToList();
                foreach (var t in ids.Chunk(100))
                {
                    var res = await db.From<ComboDetalle>()
                        .Select("id, combo_id, producto_hijo_id, cantidad, kiosco_id")
                        .Filter("combo_id", Operator.In, t.ToList())
                        .Get();
                    c.Combos.AddRange(res.Models ?? new());
                }

                c.Categorias = (await db.From<CategoriaVentaRapida>().Where(x => x.KioscoId == k).Where(x => x.Habilitado == true).Get()).Models ?? new();
                c.FormasPago = (await db.From<FormaPago>().Where(x => x.KioscoId == k).Where(x => x.Activo == true).Get()).Models ?? new();
                c.Horarios = (await db.From<HorarioVenta>().Where(x => x.KioscoId == k).Get()).Models ?? new();
                c.Listas = (await db.From<ListaPrecio>().Where(x => x.KioscoId == k).Get()).Models ?? new();
                try { c.Escalas = (await db.From<PrecioCantidad>().Where(x => x.KioscoId == k).Get()).Models ?? new(); }
                catch (Exception ex) when (!EsErrorDeRed(ex)) { /* falta el script 27 */ }
                try { c.Recargos = (await db.From<ListaRecargo>().Where(x => x.KioscoId == k).Get()).Models ?? new(); }
                catch (Exception ex) when (!EsErrorDeRed(ex)) { /* falta el script 33 */ }
                try { c.Dias = (await db.From<ListaDia>().Where(x => x.KioscoId == k).Get()).Models ?? new(); }
                catch (Exception ex) when (!EsErrorDeRed(ex)) { /* falta el script 36 */ }

                foreach (var p in c.Productos) p.Familia = null;
                EscribirSeguro(ArchivoCopia(k), JsonConvert.SerializeObject(c, Json));
                copia = c;
                Avisar();
            }
            catch (Exception ex) when (EsErrorDeRed(ex)) { MarcarSinConexion(); }
            catch { /* si falla, queda la copia anterior */ }
        }

        private CopiaNegocio? CopiaActual()
        {
            long k = state.KioscoId;
            if (k == 0) return null;
            if (copia?.KioscoId == k) return copia;
            copia = Leer<CopiaNegocio>(ArchivoCopia(k));
            return copia;
        }

        /// <summary>¿Hay datos guardados en esta PC para vender sin internet en el negocio actual?</summary>
        public bool TieneCopia => CopiaActual() != null;

        // ---- consultas sobre la copia (las usan Vender, el buscador y Cobrar) ----
        public List<Producto> Buscar(string texto, int maximo = 20)
        {
            var c = CopiaActual();
            if (c == null) return new();
            var limpio = (texto ?? "").Trim();
            if (limpio == "") return new();

            // código exacto primero (lector de códigos)
            var exacto = c.Productos.Where(p => string.Equals(p.CodigoBarras?.Trim(), limpio, StringComparison.OrdinalIgnoreCase)).ToList();
            if (exacto.Any()) return exacto.Take(maximo).ToList();

            var palabras = SinAcentos(limpio).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var codigo = string.Join("", palabras);
            return c.Productos
                .Where(p => ContieneEnOrden(SinAcentos(p.Nombre ?? ""), palabras)
                            || (!string.IsNullOrEmpty(p.CodigoBarras) && p.CodigoBarras.Contains(codigo, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => p.Nombre)
                .Take(maximo)
                .ToList();
        }

        private static bool ContieneEnOrden(string texto, string[] palabras)
        {
            int desde = 0;
            foreach (var w in palabras)
            {
                int i = texto.IndexOf(w, desde, StringComparison.Ordinal);
                if (i < 0) return false;
                desde = i + w.Length;
            }
            return true;
        }

        private static string SinAcentos(string s)
        {
            var d = (s ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
            return new string(d.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray())
                .Normalize(NormalizationForm.FormC);
        }

        public List<CategoriaVentaRapida> Categorias() =>
            (CopiaActual()?.Categorias ?? new()).Where(c => c.Habilitado).OrderBy(c => c.Orden).ToList();

        public List<Producto> ProductosDeCategoria(long categoriaId) =>
            (CopiaActual()?.Productos ?? new())
                .Where(p => p.CategoriaVentaRapidaId == categoriaId && p.MostrarEnVentaRapida)
                .OrderBy(p => p.OrdenVentaRapida)
                .ToList();

        /// <summary>Formas de pago para cobrar sin internet (sin FIADO: sin internet no se fía).</summary>
        public List<FormaPago> FormasPago() =>
            (CopiaActual()?.FormasPago ?? new())
                .Where(f => f.Activo != false && !string.Equals(f.Nombre?.Trim(), "FIADO", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Orden)
                .ToList();

        /// <summary>Misma regla que PrecioService.ObtenerColumnaPrecioActivaAsync, pero con la copia.</summary>
        public string ColumnaPrecioActiva(TimeSpan hora)
        {
            var c = CopiaActual();
            if (c == null) return "precio_venta";
            return PrecioService.ColumnaSegunMomento(c.Horarios, c.Listas, c.Dias, DateTime.Today.Add(hora));   // feriados + horario
        }

        /// <summary>Horarios, listas y tramos de recargo guardados en la PC (para calcular la Lista 2 sin internet).</summary>
        public PrecioService.ReglasListas? ReglasGuardadas(long kioscoId)
        {
            var c = CopiaActual();
            if (c == null || c.KioscoId != kioscoId) return null;
            return new PrecioService.ReglasListas(DateTime.UtcNow, c.Horarios ?? new(), c.Listas ?? new(), c.Recargos ?? new(), c.Dias ?? new());
        }

        /// <summary>Precios por cantidad guardados en la PC.</summary>
        public List<PrecioCantidad> Escalas() => CopiaActual()?.Escalas ?? new();

        /// <summary>Combos y sus productos guardados en la PC (para que se armen solos también sin internet).</summary>
        public (List<Producto> Combos, List<ComboDetalle> Detalles) CombosGuardados()
        {
            var c = CopiaActual();
            if (c == null) return (new(), new());
            return (c.Productos.Where(p => p.EsCombo == true).ToList(), c.Combos ?? new());
        }

        public string NombreLista(string columna) =>
            CopiaActual()?.Listas.FirstOrDefault(l => l.NombreColumnaProductos == columna)?.NombreLista ?? "";

        // ============================================================ USUARIOS (entrar sin internet)
        /// <summary>
        /// Después de entrar CON internet: recuerda al usuario en esta PC (la contraseña no se guarda, solo un hash),
        /// pide la llave del equipo y guarda la copia de datos.
        /// </summary>
        public void AlEntrarConInternet(string codigo, string usuario, string password, SesionSegura.ResultadoLogin r, IEnumerable<string>? funciones)
        {
            SesionSinInternet = false;
            SinConexion = false;
            Avisar();
            if (Roles.EsSuperAdmin(r.Rol)) return;   // el superadmin no vende sin internet

            try
            {
                var lista = Leer<List<UsuarioGuardado>>(ArchivoUsuarios) ?? new();
                var cod = (codigo ?? "").Trim().ToUpperInvariant();
                var usu = (usuario ?? "").Trim().ToLowerInvariant();
                lista.RemoveAll(x => x.Codigo == cod && x.Usuario == usu);
                var sal = RandomNumberGenerator.GetBytes(16);
                lista.Add(new UsuarioGuardado
                {
                    Codigo = cod,
                    Usuario = usu,
                    Sal = Convert.ToBase64String(sal),
                    Hash = Convert.ToBase64String(Hash(password, sal)),
                    LoginJson = JsonConvert.SerializeObject(r),
                    Funciones = funciones?.ToList(),
                    Fecha = DateTime.Now
                });
                EscribirSeguro(ArchivoUsuarios, JsonConvert.SerializeObject(lista.OrderByDescending(x => x.Fecha).Take(50).ToList(), Json));
            }
            catch { }

            long k = r.IdKiosco;
            _ = Task.Run(async () =>
            {
                await RegistrarEquipoAsync(k);
                await GuardarCopiaAsync(k);
                if (Pendientes > 0) await SubirPendientesAsync();
            });
        }

        private static byte[] Hash(string password, byte[] sal) =>
            Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password ?? ""), sal, 100_000, HashAlgorithmName.SHA256, 32);

        /// <summary>
        /// Entrar sin internet: solo usuarios que ya entraron con internet en esta PC y con la misma contraseña.
        /// Devuelve los datos de login guardados (null si no coincide).
        /// </summary>
        public (SesionSegura.ResultadoLogin Login, List<string>? Funciones)? EntrarSinInternet(string codigo, string usuario, string password)
        {
            var lista = Leer<List<UsuarioGuardado>>(ArchivoUsuarios);
            var cod = (codigo ?? "").Trim().ToUpperInvariant();
            var usu = (usuario ?? "").Trim().ToLowerInvariant();
            var g = lista?.FirstOrDefault(x => x.Codigo == cod && x.Usuario == usu);
            if (g == null) return null;
            try
            {
                var esperado = Convert.FromBase64String(g.Hash);
                var calculado = Hash(password, Convert.FromBase64String(g.Sal));
                if (!CryptographicOperations.FixedTimeEquals(esperado, calculado)) return null;
                var r = JsonConvert.DeserializeObject<SesionSegura.ResultadoLogin>(g.LoginJson);
                if (r == null) return null;
                SesionSinInternet = true;
                SinConexion = true;
                Avisar();
                return (r, g.Funciones);
            }
            catch { return null; }
        }

        /// <summary>Al salir ("Cambiar usuario").</summary>
        public void AlSalir()
        {
            SesionSinInternet = false;
            Avisar();
        }

        // ============================================================ LLAVE DEL EQUIPO
        private async Task RegistrarEquipoAsync(long kioscoId)
        {
            try
            {
                var llaves = Leer<Dictionary<long, string>>(ArchivoLlaves) ?? new();
                if (llaves.ContainsKey(kioscoId)) return;
                var res = await db.Rpc("registrar_equipo", new Dictionary<string, object> { { "p_kiosco", kioscoId } });
                var token = JsonConvert.DeserializeObject<string>(res.Content ?? "");
                if (string.IsNullOrWhiteSpace(token)) return;
                llaves[kioscoId] = token;
                EscribirSeguro(ArchivoLlaves, JsonConvert.SerializeObject(llaves));
            }
            catch { /* sin el SQL 25 no hay llave: las ventas quedan pendientes hasta que se corra */ }
        }

        private string? LlaveDe(long kioscoId) =>
            (Leer<Dictionary<long, string>>(ArchivoLlaves) ?? new()).TryGetValue(kioscoId, out var t) ? t : null;

        // ============================================================ VENTAS PENDIENTES
        /// <summary>Guarda una venta en la PC (sin internet). Tira error solo si no se pudo escribir el archivo.</summary>
        public void GuardarPendiente(VentaPendiente v)
        {
            var nombre = $"{v.Hora:yyyyMMdd-HHmmss}_{v.Uuid}.json";
            EscribirSeguro(Path.Combine(CarpetaPendientes, nombre), JsonConvert.SerializeObject(v, Json));
            Avisar();
            _ = Task.Run(RevisarAhoraAsync);   // por si en realidad hay internet
        }

        /// <summary>Las ventas pendientes, de la más vieja a la más nueva (para mostrarlas).</summary>
        public List<VentaPendiente> ListarPendientes() =>
            Directory.GetFiles(CarpetaPendientes, "*.json").OrderBy(f => f)
                .Select(f => Leer<VentaPendiente>(f)).Where(v => v != null).Select(v => v!).ToList();

        /// <summary>Sube las ventas pendientes (de a una, en orden). Se llama sola cuando hay internet.</summary>
        public async Task<int> SubirPendientesAsync()
        {
            int subidas = 0;
            string? error = null;
            foreach (var archivo in Directory.GetFiles(CarpetaPendientes, "*.json").OrderBy(f => f))
            {
                var v = Leer<VentaPendiente>(archivo);
                if (v == null) continue;

                var llave = LlaveDe(v.KioscoId);
                if (llave == null)
                {
                    error = "Esta PC todavía no tiene permiso para subir ventas: entrá una vez con internet (y fijate que esté corrido el script 25.ventas_sin_internet.sql).";
                    continue;
                }

                try
                {
                    var cuerpo = JsonConvert.SerializeObject(new { p_token = llave, p_venta = v }, Json);
                    using var req = new HttpRequestMessage(HttpMethod.Post, op.SupabaseUrl.TrimEnd('/') + "/rest/v1/rpc/subir_venta_offline")
                    {
                        Content = new StringContent(cuerpo, Encoding.UTF8, "application/json")
                    };
                    req.Headers.Add("apikey", op.AnonKey);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", op.AnonKey);
                    using var resp = await http.SendAsync(req);
                    var texto = await resp.Content.ReadAsStringAsync();

                    if (resp.IsSuccessStatusCode)
                    {
                        lock (llaveArchivos)
                        {
                            File.Move(archivo, Path.Combine(CarpetaSubidas, Path.GetFileName(archivo)), overwrite: true);
                        }
                        subidas++;
                    }
                    else
                    {
                        error = resp.StatusCode == System.Net.HttpStatusCode.NotFound
                            ? "Falta preparar la base: hay que correr el script 25.ventas_sin_internet.sql en Supabase."
                            : "La base no aceptó una venta: " + MensajeDe(texto);
                        // si la llave ya no sirve (ej: base reseteada), se pide otra la próxima vez que alguien entre con internet
                        if (texto.Contains("no autorizado", StringComparison.OrdinalIgnoreCase))
                        {
                            var llaves = Leer<Dictionary<long, string>>(ArchivoLlaves) ?? new();
                            if (llaves.Remove(v.KioscoId)) EscribirSeguro(ArchivoLlaves, JsonConvert.SerializeObject(llaves));
                        }
                    }
                }
                catch (Exception ex) when (EsErrorDeRed(ex))
                {
                    MarcarSinConexion();
                    break;   // se cortó otra vez: se sigue en la próxima vuelta
                }
                catch (Exception ex)
                {
                    error = "No se pudo subir una venta: " + ex.Message;
                }
            }

            ErrorSubida = error;
            if (subidas > 0)
            {
                UltimasSubidas = subidas;
                CuandoSeSubieron = DateTime.Now;
                LimpiarSubidasViejas();
            }
            Avisar();
            return subidas;
        }

        private static string MensajeDe(string json)
        {
            try
            {
                var o = Newtonsoft.Json.Linq.JObject.Parse(json);
                return o["message"]?.ToString() ?? json;
            }
            catch { return json; }
        }

        /// <summary>Las ventas ya subidas se guardan 30 días en la carpeta "subidas" (por las dudas) y después se borran.</summary>
        private void LimpiarSubidasViejas()
        {
            try
            {
                foreach (var f in Directory.GetFiles(CarpetaSubidas, "*.json"))
                    if (File.GetLastWriteTime(f) < DateTime.Now.AddDays(-30)) File.Delete(f);
            }
            catch { }
        }

        public void Dispose()
        {
            disposed = true;
            timer.Dispose();
            http.Dispose();
            if (Actual == this) Actual = null;
        }
    }
}
