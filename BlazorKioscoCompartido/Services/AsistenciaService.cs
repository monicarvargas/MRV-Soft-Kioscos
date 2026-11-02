using BlazorKioscoCompartido.Models;
using BlazorKioscoCompartido.Models.Vistas;
using static Supabase.Postgrest.Constants;

namespace BlazorKioscoCompartido.Services
{
    /// <summary>
    /// Entrada y salida de empleados. Las fechas/horas las pone la base (funciones fichar_entrada / fichar_salida)
    /// para no depender del reloj ni de la zona horaria de cada equipo.
    /// </summary>
    public class AsistenciaService
    {
        private readonly Supabase.Client _db;
        public AsistenciaService(Supabase.Client db) => _db = db;

        /// <summary>Los administradores no fichan (y si entran a ver algo no le cierran el turno a nadie).</summary>
        public static bool DebeFichar(string? rol) =>
            !string.Equals(rol, "admin", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(rol, "superadmin", StringComparison.OrdinalIgnoreCase);

        public async Task<long?> FicharEntradaAsync(long kioscoId, Guid usuarioId, string dispositivo)
        {
            var res = await _db.Rpc("fichar_entrada", new Dictionary<string, object>
            {
                { "p_kiosco", kioscoId }, { "p_usuario", usuarioId.ToString() }, { "p_dispositivo", dispositivo }
            });
            return long.TryParse(res.Content?.Trim().Trim('"'), out var id) ? id : null;
        }

        public async Task FicharSalidaAsync(long asistenciaId) =>
            await _db.Rpc("fichar_salida", new Dictionary<string, object> { { "p_id", asistenciaId } });

        /// <summary>Entradas entre dos días (incluidos). Antes cierra las que se olvidaron abiertas.</summary>
        public async Task<List<AsistenciaVista>> ObtenerAsync(long kioscoId, DateTime desde, DateTime hasta)
        {
            try { await _db.Rpc("cerrar_asistencias_vencidas", new Dictionary<string, object> { { "p_kiosco", kioscoId } }); } catch { }

            var res = await _db.From<AsistenciaVista>()
                .Where(a => a.KioscoId == kioscoId)
                .Filter("dia", Operator.GreaterThanOrEqual, desde.ToString("yyyy-MM-dd"))
                .Filter("dia", Operator.LessThanOrEqual, hasta.ToString("yyyy-MM-dd"))
                .Order("entrada_local", Ordering.Ascending)
                .Get();
            return res.Models ?? new();
        }

        /// <summary>Corrección del dueño. Formato "yyyy-MM-dd HH:mm" en hora argentina; salida vacía = sigue trabajando.</summary>
        public async Task EditarAsync(long id, string entradaLocal, string? salidaLocal) =>
            await _db.Rpc("editar_asistencia", new Dictionary<string, object>
            {
                { "p_id", id }, { "p_entrada", entradaLocal }, { "p_salida", salidaLocal ?? "" }
            });

        // ===================== TURNOS =====================
        public async Task<List<Turno>> GetTurnos(long kioscoId) =>
            ((await _db.From<Turno>().Where(t => t.KioscoId == kioscoId).Get()).Models ?? new())
            .OrderBy(t => t.Orden).ThenBy(t => t.Inicio()).ToList();

        public async Task GuardarTurno(Turno t, bool esNuevo)
        {
            if (esNuevo) await _db.From<Turno>().Insert(t);
            else await _db.From<Turno>().Update(t);
        }

        public async Task EliminarTurno(Turno t) => await _db.From<Turno>().Delete(t);

        /// <summary>
        /// En qué turno cae una hora local y a qué "jornada" pertenece
        /// (el turno noche de 23 a 07 cuenta para el día en que empezó).
        /// </summary>
        public static (Turno? turno, DateTime jornada) Ubicar(DateTime horaLocal, IEnumerable<Turno> turnos)
        {
            var h = horaLocal.TimeOfDay;
            foreach (var t in turnos.Where(x => x.Activo))
            {
                if (!t.CruzaMedianoche())
                {
                    if (h >= t.Inicio() && h < t.Fin()) return (t, horaLocal.Date);
                }
                else
                {
                    if (h >= t.Inicio()) return (t, horaLocal.Date);
                    if (h < t.Fin()) return (t, horaLocal.Date.AddDays(-1));
                }
            }
            return (null, horaLocal.Date);
        }
    }
}
