using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>Turno fijo del negocio (ej: Mañana 07:00 a 15:00). Si HoraFin es menor que HoraInicio, termina al día siguiente.</summary>
    [Table("turnos")]
    public class Turno : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("nombre")]
        public string Nombre { get; set; } = "";
        /// <summary>"07:00:00" (así lo devuelve la base)</summary>
        [Column("hora_inicio")]
        public string HoraInicio { get; set; } = "07:00";
        [Column("hora_fin")]
        public string HoraFin { get; set; } = "15:00";
        [Column("orden")]
        public int Orden { get; set; }
        [Column("activo")]
        public bool Activo { get; set; } = true;

        // Son métodos (no propiedades) para que NO se manden a la base al guardar
        public TimeSpan Inicio() => TimeSpan.TryParse(HoraInicio, out var t) ? t : TimeSpan.Zero;
        public TimeSpan Fin() => TimeSpan.TryParse(HoraFin, out var t) ? t : TimeSpan.Zero;
        public bool CruzaMedianoche() => Fin() <= Inicio();
        public string Rango() => $"{Inicio():hh\\:mm} a {Fin():hh\\:mm}";
    }
}
