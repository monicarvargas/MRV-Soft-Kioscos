using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Text.Json.Serialization;

namespace BlazorKioscoCompartido.Models
{
    [Table("venta_pagos")]
    public class VentaPago : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("venta_id")]
        public long VentaId { get; set; }

        [Column("forma_pago_id")]
        public long FormaPagoId { get; set; }

        [Column("monto")]
        public decimal Monto { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    // =================================================================
    // PROPIEDADES EXTENDIDAS
    // Usamos [JsonIgnore] para que Supabase NO la busque en la base de datos
    // =================================================================
    [JsonIgnore]
        public string NombreFormaPago { get; set; } = string.Empty;
    }
}
