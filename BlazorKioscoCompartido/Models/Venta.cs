
using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
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
        [Column("usuario_operador_id")] 
        public Guid? UsuarioOperadorId { get; set; }
        [Column("forma_pago_id")]
        public long FormaPagoId { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("persona_id")]
        public long? PersonaId { get; set; } // Opcional, por si es venta al paso
        // Propiedad opcional para traer los datos de la forma de pago (Join)
        [Reference(typeof(Usuario))]
        public Usuario? UsuarioOperador { get; set; }
        // Propiedad opcional para traer los datos de la forma de pago (Join)
        [Reference(typeof(FormaPago))]
        public FormaPago? FormaPago { get; set; }
        // Propiedad opcional para traer los datos de la forma de pago (Join)
        [Reference(typeof(Kiosco))]
        public Kiosco? Kiosco { get; set; }
        [Column("numero_unico_identificatorio_movimiento")]
        public long NumeroUnicoIdentificatorioMovimiento { get; set; }
        [Column("pagado")]
        public bool? Pagado { get; set; } // Opcional, por si es venta al paso
    }
}