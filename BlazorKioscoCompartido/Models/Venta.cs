
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
        // (Antes había acá un "join" con usuarios: se sacó porque esa tabla tiene la contraseña y la base no deja leerla.
        //  El nombre del vendedor se busca aparte, solo con id, username y rol.)
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

        /// <summary>
        /// true = renglón de "ajuste por aumento de precios" que se suma a una deuda de fiado
        /// cuando el dueño decide cobrarla a precio de hoy. En las ventas normales queda null
        /// (no se manda) y la base pone false.
        /// </summary>
        [Column("es_ajuste_precio", Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? EsAjustePrecio { get; set; }

        /// <summary>Fecha y hora exacta de la venta. La pone sola la base (default now()); la app no la manda.</summary>
        [Column("hora_venta", Newtonsoft.Json.NullValueHandling.Ignore)]
        public DateTime? HoraVenta { get; set; }
    }
}