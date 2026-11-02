using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models.Vistas
{
    /// <summary>Una entrada/salida de un empleado (vista vw_asistencias: horas ya en hora argentina).</summary>
    [Table("vw_asistencias")]
    public class AsistenciaVista : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("usuario_id")]
        public Guid UsuarioId { get; set; }
        [Column("username")]
        public string? Username { get; set; }
        [Column("rol")]
        public string? Rol { get; set; }
        [Column("dispositivo")]
        public string? Dispositivo { get; set; }
        [Column("cierre")]
        public string? Cierre { get; set; }
        /// <summary>"2026-10-12"</summary>
        [Column("dia")]
        public string Dia { get; set; } = "";
        /// <summary>"2026-10-12 07:05"</summary>
        [Column("entrada_local")]
        public string EntradaLocal { get; set; } = "";
        [Column("salida_local")]
        public string? SalidaLocal { get; set; }
        [Column("abierta")]
        public bool Abierta { get; set; }
        [Column("minutos")]
        public int Minutos { get; set; }

        public string HoraEntrada => EntradaLocal.Length >= 16 ? EntradaLocal[11..16] : EntradaLocal;
        public string HoraSalida => SalidaLocal is { Length: >= 16 } s ? s[11..16] : "";
        /// <summary>true si salió otro día (turno noche)</summary>
        public bool SalioOtroDia => SalidaLocal is { Length: >= 10 } s && s[..10] != Dia;
    }
}
