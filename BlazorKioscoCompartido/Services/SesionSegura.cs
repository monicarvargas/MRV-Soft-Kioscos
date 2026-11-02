using Newtonsoft.Json;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Sesión segura con la base (ver _sql\15.seguridad_por_negocio.sql):
    ///  1) el equipo obtiene una credencial de Supabase (sesión anónima, sin mail);
    ///  2) iniciar_sesion ata esa credencial al negocio y al usuario;
    ///  3) la base solo deja ver/tocar filas de ese negocio (el superadmin, todas).
    /// </summary>
    public static class SesionSegura
    {
        public sealed class KioscoLogin
        {
            [JsonProperty("id")] public long Id { get; set; }
            [JsonProperty("nombre_negocio")] public string? NombreNegocio { get; set; }
            [JsonProperty("estado")] public bool? Estado { get; set; }
            [JsonProperty("usar_redondeo")] public bool? UsarRedondeo { get; set; }
            [JsonProperty("multiplo_redondeo")] public int? MultiploRedondeo { get; set; }
            [JsonProperty("color_tema")] public string? ColorTema { get; set; }
            [JsonProperty("unidades")] public List<string>? Unidades { get; set; }
        }

        public sealed class ResultadoLogin
        {
            /// <summary>ok · negocio · baja · clave · sin_credencial</summary>
            [JsonProperty("resultado")] public string Resultado { get; set; } = "";
            [JsonProperty("id")] public Guid Id { get; set; }
            [JsonProperty("username")] public string? Username { get; set; }
            [JsonProperty("rol")] public string? Rol { get; set; }
            [JsonProperty("id_kiosco")] public long IdKiosco { get; set; }
            [JsonProperty("kiosco")] public KioscoLogin? Kiosco { get; set; }
        }

        /// <summary>Si el equipo todavía no tiene credencial, pide una (anónima).</summary>
        public static async Task AsegurarCredencialAsync(this Supabase.Client db)
        {
            if (db.Auth.CurrentSession?.AccessToken != null && db.Auth.CurrentUser != null) return;
            await db.Auth.SignInAnonymously();
        }

        /// <summary>La base verifica negocio + usuario + contraseña y ata la credencial a ese negocio.</summary>
        public static async Task<ResultadoLogin> IniciarSesionAsync(this Supabase.Client db, string codigo, string usuario, string password)
        {
            await db.AsegurarCredencialAsync();
            var res = await db.Rpc("iniciar_sesion", new Dictionary<string, object>
            {
                { "p_codigo", codigo }, { "p_usuario", usuario }, { "p_password", password }
            });
            return string.IsNullOrWhiteSpace(res.Content)
                ? new ResultadoLogin { Resultado = "error" }
                : JsonConvert.DeserializeObject<ResultadoLogin>(res.Content) ?? new ResultadoLogin { Resultado = "error" };
        }

        /// <summary>
        /// Funciones habilitadas del negocio según su plan (SQL 20). Devuelve null si la base todavía
        /// no tiene los planes: en ese caso se muestra todo, como antes.
        /// </summary>
        public static async Task<HashSet<string>?> CargarFuncionesAsync(this Supabase.Client db, long kioscoId)
        {
            try
            {
                var res = await db.Rpc("funciones_negocio", new Dictionary<string, object> { { "p_kiosco", kioscoId } });
                if (string.IsNullOrWhiteSpace(res.Content)) return null;
                var lista = JsonConvert.DeserializeObject<List<string>>(res.Content);
                return lista == null ? null : new HashSet<string>(lista);
            }
            catch { return null; }
        }

        /// <summary>
        /// Lo que el dueño le dio o le sacó a cada perfil (Configuración → Ayuda y plan → Perfiles, script 38).
        /// Sin el script o sin conexión: los permisos de fábrica.
        /// </summary>
        public static async Task CargarPermisosRolesAsync(this Supabase.Client db, long kioscoId)
        {
            try
            {
                var k = await db.From<Models.Kiosco>().Select("id, permisos_roles").Where(x => x.Id == kioscoId).Single();
                Roles.CargarPermisos(k?.PermisosRoles);
            }
            catch { Roles.CargarPermisos(null); }
        }

        /// <summary>"Cambiar usuario": la credencial deja de estar atada al negocio.</summary>
        public static async Task CerrarSesionAsync(this Supabase.Client db)
        {
            try { await db.Rpc("cerrar_sesion", new Dictionary<string, object>()); } catch { }
        }

        /// <summary>Superadmin "Ver como": la base pasa a dejarle usar ese negocio (null = vuelve al suyo).</summary>
        public static async Task CambiarNegocioAsync(this Supabase.Client db, long? kioscoId) =>
            await db.Rpc("cambiar_kiosco_sesion", new Dictionary<string, object> { { "p_kiosco", (object)kioscoId! } });
    }
}
