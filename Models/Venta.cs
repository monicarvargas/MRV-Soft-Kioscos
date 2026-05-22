
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscos_v1.Models
{
    [Table("ventas")]
    public class Venta : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow.ToLocalTime();
        [Column("total")]
        public decimal Total { get; set; }
        [Column("usuario_id")]
        public long UsuarioId { get; set; }
        [Column("forma_pago_id")]
        public long FormaPagoId { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("cliente_id")]
        public long? ClienteId { get; set; } // Opcional, por si es venta al paso
        // Propiedad opcional para traer los datos de la forma de pago (Join)
        [Reference(typeof(Usuario))]
        public Usuario? Usuario { get; set; }
        // Propiedad opcional para traer los datos de la forma de pago (Join)
        [Reference(typeof(FormaPago))]
        public FormaPago? FormaPago { get; set; }
        // Propiedad opcional para traer los datos de la forma de pago (Join)
        [Reference(typeof(Kiosco))]
        public Kiosco? Kiosco { get; set; }

    }
}