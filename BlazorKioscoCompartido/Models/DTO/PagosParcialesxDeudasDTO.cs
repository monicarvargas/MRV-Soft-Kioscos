using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models.DTO
{
    [Table("vw_pagos_parciales_x_deudas")]
    public class PagosParcialesxDeudasDTO:BaseModel
    {
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("persona_id")]
        public long PersonaId { get; set; }
        [Column("pago_entregado")]
        public decimal TotalPagoEntregado { get; set; }
        [Column("forma_pago_id")]
        public long FormaPagoId { get; set; }
        [Column("fp_nombre")]
        public string FormaPagoNombre { get; set; }
        [Column("numero_unico_identificatorio_movimiento")]
        public long NumeroUnicoIdentificatorioMovimiento { get; set; }
        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
