using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BlazorKioscoCompartido.Models
{
    [Table("formas_pago")]
    public class FormaPago : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("nombre")]
        public string? Nombre { get; set; }
        [Column("activo")]
        public bool? Activo { get; set; }
        [Column("pide_comprobante")]
        public bool? PideComprobante { get; set; }
        [Column("orden")]
        public int Orden { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("created_at")]
        public DateTime FechaCreacion { get; set; } =  DateTime.UtcNow.ToLocalTime();
    }
}
