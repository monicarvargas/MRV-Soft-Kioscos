using BlazorKioscoCompartido.Models;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Constants;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Acceso a datos para las pantallas de configuración (dueño y superadmin).
    /// Registrar en Program.cs:  builder.Services.AddScoped<ConfiguracionService>();
    /// (asume que Supabase.Client ya está registrado en DI)
    /// </summary>
    public class ConfiguracionService
    {
        // ---- Datos base que TODO negocio tiene que tener ----
        public const string ProveedorDefault = "PROVEEDOR";
        public const string FamiliaDefault = "ARTICULOS";
        /// <summary>Familia de los combos: la página de combos la elige sola.</summary>
        public const string FamiliaCombos = "COMBOS";
        public static readonly string[] FormasPagoDefault = { "EFECTIVO", "TRANSFERENCIA", "FIADO" };

        private readonly Supabase.Client _db;
        public ConfiguracionService(Supabase.Client db) => _db = db;

        // ================= GENÉRICOS =================
        public async Task Guardar<T>(T item, bool esNuevo) where T : BaseModel, new()
        {
            if (esNuevo) await _db.From<T>().Insert(item);
            else await _db.From<T>().Update(item);
        }

        public async Task Eliminar<T>(T item) where T : BaseModel, new()
            => await _db.From<T>().Delete(item);

        /// <summary>Traduce errores de Postgres a algo entendible para el usuario.</summary>
        public static string MensajeError(Exception ex)
        {
            var msg = ex.Message ?? "";
            if (msg.Contains("23503") || msg.Contains("foreign key", StringComparison.OrdinalIgnoreCase))
                return "No se puede eliminar porque está siendo usado (por ejemplo, en productos o ventas). Podés renombrarlo o desactivarlo.";
            if (msg.Contains("23505") || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase))
                return "Ya existe un registro con ese nombre.";
            return "Ocurrió un error: " + msg;
        }

        // ================= PROVEEDORES =================
        public async Task<List<Proveedor>> GetProveedores(long kioscoId) =>
            (await _db.From<Proveedor>()
                .Where(x => x.KioscoId == kioscoId)
                .Order(x => x.Nombre, Ordering.Ascending)
                .Get()).Models;

        // ================= FAMILIAS =================
        public async Task<List<Familia>> GetFamilias(long kioscoId) =>
            (await _db.From<Familia>()
                .Where(x => x.KioscoId == kioscoId)
                .Order(x => x.Nombre, Ordering.Ascending)
                .Get()).Models;

        // ================= LISTAS DE PRECIOS =================
        public async Task<List<ListaPrecio>> GetListasPrecios(long kioscoId) =>
            (await _db.From<ListaPrecio>()
                .Where(x => x.KioscoId == kioscoId)
                .Order(x => x.Id, Ordering.Ascending)
                .Get()).Models;

        // ================= FORMAS DE PAGO =================
        public async Task<List<FormaPago>> GetFormasPago(long kioscoId) =>
            (await _db.From<FormaPago>()
                .Where(x => x.KioscoId == kioscoId)
                .Order(x => x.Orden, Ordering.Ascending)
                .Get()).Models;

        // ================= USUARIOS =================
        // La contraseña NO se puede leer desde la app (está encriptada y la base no la entrega):
        // por eso se piden solo estas columnas y se guarda sin "devolver" la fila.
        private const string ColumnasUsuario = "id, username, id_kiosco, rol";
        private static readonly Supabase.Postgrest.QueryOptions SinDevolver = new() { Returning = Supabase.Postgrest.QueryOptions.ReturnType.Minimal };

        public async Task<List<Usuario>> GetUsuarios(long kioscoId) =>
            (await _db.From<Usuario>()
                .Select(ColumnasUsuario)
                .Where(x => x.IdKiosco == kioscoId)
                .Order(x => x.Username!, Ordering.Ascending)
                .Get()).Models;

        /// <summary>
        /// ¿Ya hay un usuario con ese nombre EN ESE NEGOCIO? (script 39: el nombre es único dentro de cada negocio;
        /// dos negocios distintos pueden tener cada uno su "MARIA", porque al entrar se pone el código del negocio).
        /// Sin el script 39, se controla como antes (en todo el sistema).
        /// </summary>
        public async Task<bool> ExisteUsername(string username, long kioscoId, Guid? excluirId = null)
        {
            try
            {
                var res = await _db.Rpc("usuario_existe_en_negocio", new Dictionary<string, object>
                {
                    { "p_kiosco", kioscoId }, { "p_username", username }, { "p_excluir", (object)excluirId?.ToString()! }
                });
                return string.Equals(res.Content?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex.Message.Contains("usuario_existe_en_negocio"))
            {
                // todavía no se corrió el script 39
                var res = await _db.Rpc("usuario_existe", new Dictionary<string, object>
                {
                    { "p_username", username }, { "p_excluir", (object)excluirId?.ToString()! }
                });
                return string.Equals(res.Content?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public async Task CrearUsuario(Usuario u)
        {
            u.Kiosco = null; // no mandar el join
            await _db.From<Usuario>().Insert(u, SinDevolver);   // la base encripta la contraseña sola
        }

        /// <summary>
        /// Actualiza solo usuario y rol (y la contraseña si viene cargada),
        /// así nunca se pisa la contraseña con vacío.
        /// </summary>
        public async Task ActualizarUsuario(Usuario u, string? nuevaPassword)
        {
            var q = _db.From<Usuario>()
                .Where(x => x.Id == u.Id)
                .Set(x => x.Username!, u.Username)
                .Set(x => x.Rol!, u.Rol);

            if (!string.IsNullOrWhiteSpace(nuevaPassword))
                q = q.Set(x => x.Password!, nuevaPassword);

            await q.Update(SinDevolver);   // la base encripta la contraseña sola
        }

        public async Task EliminarUsuario(Usuario u) =>
            await _db.From<Usuario>().Where(x => x.Id == u.Id).Delete(SinDevolver);

        // ================= CLIENTES / EMPLEADOS (tabla personas) =================
        public async Task<List<Persona>> GetPersonas(long kioscoId) =>
            (await _db.From<Persona>()
                .Where(x => x.KioscoId == kioscoId)
                .Order(x => x.Nombre, Ordering.Ascending)
                .Get()).Models;

        /// <summary>true si la persona tiene ventas, fiados o pagos (entonces no se borra: se deshabilita).</summary>
        public async Task<bool> PersonaTieneMovimientos(long personaId)
        {
            var res = await _db.From<Venta>()
                .Select("id")
                .Where(x => x.PersonaId == personaId)
                .Limit(1)
                .Get();
            return res.Models.Any();
        }

        // ================= COLOR DEL NEGOCIO =================
        public async Task<string?> GetColorTema(long kioscoId)
        {
            var k = await _db.From<Kiosco>().Select("id, color_tema").Where(x => x.Id == kioscoId).Single();
            return k?.ColorTema;
        }

        public async Task GuardarColorTema(long kioscoId, string clave) =>
            await _db.From<Kiosco>()
                .Where(x => x.Id == kioscoId)
                .Set(x => x.ColorTema, clave)
                .Update();

        // ================= KIOSCOS (SUPERADMIN) =================
        public async Task<List<Kiosco>> GetKioscos() =>
            (await _db.From<Kiosco>().Order(x => x.Id, Ordering.Descending).Get()).Models;

        /// <summary>
        /// Da de alta el negocio, le crea los datos base y opcionalmente el usuario admin del dueño.
        /// </summary>
        public async Task<Kiosco> AltaNegocio(Kiosco k, string? adminUser, string? adminPass)
        {
            k.CodigoVinculacion = string.IsNullOrWhiteSpace(k.CodigoVinculacion) ? GenerarCodigo() : k.CodigoVinculacion;
            k.Estado ??= true;
            k.FechaAlta = DateTime.Now;

            var nuevo = (await _db.From<Kiosco>().Insert(k)).Model
                        ?? throw new Exception("No se pudo crear el negocio.");

            await AsegurarDatosBase(nuevo.Id);

            if (!string.IsNullOrWhiteSpace(adminUser) && !string.IsNullOrWhiteSpace(adminPass))
            {
                await CrearUsuario(new Usuario
                {
                    Username = adminUser.Trim(),
                    Password = adminPass,
                    IdKiosco = nuevo.Id,
                    Rol = "admin"
                });
            }
            return nuevo;
        }

        /// <summary>
        /// Crea lo que falte de los datos base (PROVEEDOR, familias ARTICULOS y COMBOS, formas de pago).
        /// Se puede llamar las veces que quieras: no duplica. Sirve para arreglar negocios viejos.
        /// Devuelve cuántos registros creó.
        /// </summary>
        public async Task<int> AsegurarDatosBase(long kioscoId)
        {
            int creados = 0;

            var provs = await GetProveedores(kioscoId);
            if (!provs.Any(p => Igual(p.Nombre, ProveedorDefault)))
            {
                await _db.From<Proveedor>().Insert(new Proveedor { KioscoId = kioscoId, Nombre = ProveedorDefault });
                creados++;
            }

            var fams = await GetFamilias(kioscoId);
            if (!fams.Any(f => Igual(f.Nombre, FamiliaDefault)))
            {
                await _db.From<Familia>().Insert(new Familia { KioscoId = kioscoId, Nombre = FamiliaDefault });
                creados++;
            }
            if (!fams.Any(EsFamiliaCombos))
            {
                await _db.From<Familia>().Insert(new Familia { KioscoId = kioscoId, Nombre = FamiliaCombos });
                creados++;
            }

            var formas = await GetFormasPago(kioscoId);
            int orden = formas.Count == 0 ? 0 : formas.Max(f => f.Orden);
            var faltantes = FormasPagoDefault
                .Where(n => !formas.Any(f => Igual(f.Nombre, n)))
                .Select(n => new FormaPago
                {
                    KioscoId = kioscoId,
                    Nombre = n,
                    Activo = true,
                    PideComprobante = n == "TRANSFERENCIA",
                    Orden = ++orden
                }).ToList();

            if (faltantes.Count > 0)
            {
                await _db.From<FormaPago>().Insert(faltantes);
                creados += faltantes.Count;
            }
            return creados;
        }

        public static bool EsProveedorDefault(Proveedor p) => Igual(p.Nombre, ProveedorDefault);
        /// <summary>Familias que no se pueden renombrar ni borrar (las usa el sistema).</summary>
        public static bool EsFamiliaDefault(Familia f) => Igual(f.Nombre, FamiliaDefault) || EsFamiliaCombos(f);

        /// <summary>La familia de los combos ("COMBOS"; se acepta también una vieja llamada "COMBO").</summary>
        public static bool EsFamiliaCombos(Familia f) => Igual(f.Nombre, FamiliaCombos) || Igual(f.Nombre, "COMBO");

        /// <summary>Devuelve la familia COMBOS del negocio; si no existe, la crea.</summary>
        public async Task<Familia?> AsegurarFamiliaCombos(long kioscoId)
        {
            var fams = await GetFamilias(kioscoId);
            var f = fams.FirstOrDefault(EsFamiliaCombos);
            if (f != null) return f;
            var res = await _db.From<Familia>().Insert(new Familia { KioscoId = kioscoId, Nombre = FamiliaCombos });
            return res.Models.FirstOrDefault();
        }

        private static bool Igual(string? a, string b) =>
            string.Equals(a?.Trim(), b, StringComparison.OrdinalIgnoreCase);

        public static string GenerarCodigo()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // sin 0/O/1/I/L
            return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
        }
    }
}
