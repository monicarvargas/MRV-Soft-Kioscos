using Newtonsoft.Json;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Copia automática del Excel del negocio en una carpeta de la PC (solo la app de escritorio la crea).
    /// Si la carpeta es de Google Drive (o OneDrive / Dropbox), sube sola a la nube.
    /// Se configura en Configuración → Copia de seguridad. La configuración queda guardada en la PC, por negocio.
    /// Mientras la app está abierta y hay alguien adentro, revisa cada 30 minutos si ya toca hacer la copia.
    /// </summary>
    public sealed class CopiaAutomatica : IDisposable
    {
        public static CopiaAutomatica? Actual { get; private set; }

        public sealed class Config
        {
            public bool Activa { get; set; }
            public string Carpeta { get; set; } = "";
            public int CadaDias { get; set; } = 1;       // 1 = todos los días, 7 = una vez por semana
            public int Guardar { get; set; } = 30;       // cuántas copias se guardan (las más viejas se borran)
            public int DiasVentas { get; set; } = 90;    // ventas de los últimos N días
            public DateTime? Ultima { get; set; }
            public string? UltimoArchivo { get; set; }
            public string? UltimoError { get; set; }
        }

        public const string Subcarpeta = "Copias MRV Soft";

        private readonly Supabase.Client db;
        private readonly AppState state;
        private readonly string archivoConfig;
        private readonly System.Threading.Timer timer;
        private readonly SemaphoreSlim trabajando = new(1, 1);
        private readonly object llave = new();

        public event Action? OnCambio;

        public CopiaAutomatica(Supabase.Client db, AppState state, string carpetaApp)
        {
            this.db = db;
            this.state = state;
            Directory.CreateDirectory(carpetaApp);
            archivoConfig = Path.Combine(carpetaApp, "copia_automatica.json");
            Actual = this;
            timer = new System.Threading.Timer(_ => _ = RevisarAsync(), null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(30));
        }

        // ============================================================ CONFIGURACIÓN (por negocio)
        private Dictionary<long, Config> LeerTodas()
        {
            try
            {
                lock (llave)
                {
                    if (!File.Exists(archivoConfig)) return new();
                    return JsonConvert.DeserializeObject<Dictionary<long, Config>>(File.ReadAllText(archivoConfig)) ?? new();
                }
            }
            catch { return new(); }
        }

        public Config Leer(long kioscoId) => LeerTodas().TryGetValue(kioscoId, out var c) ? c : new Config();

        public void Guardar(long kioscoId, Config c)
        {
            lock (llave)
            {
                var todas = LeerTodas();
                todas[kioscoId] = c;
                var tmp = archivoConfig + ".tmp";
                File.WriteAllText(tmp, JsonConvert.SerializeObject(todas, Formatting.Indented));
                File.Move(tmp, archivoConfig, overwrite: true);
            }
            try { OnCambio?.Invoke(); } catch { }
        }

        /// <summary>Busca la carpeta de Google Drive de la PC (Google Drive para escritorio). null si no la encuentra.</summary>
        public static string? BuscarGoogleDrive()
        {
            var perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var candidatos = new List<string>
            {
                Path.Combine(perfil, "Google Drive", "Mi unidad"),
                Path.Combine(perfil, "Google Drive", "My Drive"),
                Path.Combine(perfil, "Google Drive"),
                Path.Combine(perfil, "Mi unidad"),
                Path.Combine(perfil, "My Drive"),
            };
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    candidatos.Add(Path.Combine(d.Name, "Mi unidad"));
                    candidatos.Add(Path.Combine(d.Name, "My Drive"));
                }
            }
            catch { }
            return candidatos.FirstOrDefault(Directory.Exists);
        }

        // ============================================================ HACER LA COPIA
        /// <summary>¿Ya toca hacer la copia?</summary>
        private static bool Toca(Config c) =>
            c.Activa && !string.IsNullOrWhiteSpace(c.Carpeta)
            && (c.Ultima == null || DateTime.Now - c.Ultima.Value >= TimeSpan.FromDays(Math.Max(1, c.CadaDias)) - TimeSpan.FromHours(1));

        private async Task RevisarAsync()
        {
            long k = state.KioscoId;
            if (k == 0 || state.SesionSuperAdmin != null || Roles.EsSuperAdmin(state.UsuarioRol)) return;
            if (SinInternet.VentaSinInternet.Actual?.ModoLimitado == true) return;   // sin internet no se puede leer la base
            var c = Leer(k);
            if (!Toca(c)) return;
            await HacerAhoraAsync(k, state.NombreNegocio);
        }

        /// <summary>Hace la copia ya (también lo usa el botón "Probar ahora"). Devuelve la ruta del archivo o tira el error.</summary>
        public async Task<string> HacerAhoraAsync(long kioscoId, string? negocio)
        {
            if (!await trabajando.WaitAsync(0)) throw new InvalidOperationException("Ya se está haciendo una copia. Esperá un momento.");
            var c = Leer(kioscoId);
            try
            {
                if (string.IsNullOrWhiteSpace(c.Carpeta)) throw new InvalidOperationException("Elegí la carpeta donde guardar las copias.");
                if (!Directory.Exists(c.Carpeta)) throw new DirectoryNotFoundException($"No existe la carpeta: {c.Carpeta}");

                var destino = Path.Combine(c.Carpeta, Subcarpeta);
                Directory.CreateDirectory(destino);

                var r = await CopiaExcel.GenerarAsync(db, kioscoId, c.DiasVentas);
                var ruta = Path.Combine(destino, CopiaExcel.NombreArchivo(negocio, DateTime.Now));
                await File.WriteAllBytesAsync(ruta, r.Bytes);

                // se guardan las últimas N (las más viejas se borran)
                foreach (var viejo in Directory.GetFiles(destino, "Copia *.xlsx").OrderByDescending(File.GetLastWriteTime).Skip(Math.Max(1, c.Guardar)))
                {
                    try { File.Delete(viejo); } catch { }
                }

                c.Ultima = DateTime.Now;
                c.UltimoArchivo = ruta;
                c.UltimoError = null;
                Guardar(kioscoId, c);
                return ruta;
            }
            catch (Exception ex)
            {
                c.UltimoError = CopiaExcel.Mensaje(ex);
                Guardar(kioscoId, c);
                throw new InvalidOperationException(c.UltimoError, ex);
            }
            finally { trabajando.Release(); }
        }

        public void Dispose()
        {
            timer.Dispose();
            if (Actual == this) Actual = null;
        }
    }
}
