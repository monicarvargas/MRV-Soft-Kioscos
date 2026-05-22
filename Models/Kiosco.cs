using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BlazorKioscos_v1.Models
{
    [Table("kioscos")]
    public class Kiosco:BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("nombre_negocio")]
        public string? NombreKiosco { get; set; }
        [Column("cuit_dni")]
        public string? CuitDni { get; set; }
        [Column("fecha_alta")]
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow.ToLocalTime();
        [Column("estado")]
        public bool? Estado { get; set; }
        [Column("fecha_vencimiento_abono")]
        public DateTime FechaVencimientoAbono { get; set; }
        [Column("dias_anticipacion_aviso")]
        public int? DiasAnticipacionAviso { get; set; }
        [Column("notas_administrativas")]
        public string? NotasAdministrativas { get; set; }
        [Column("codigo_vinculacion")]
        public string CodigoVinculacion { get; set; } = "";
        
    }
}
