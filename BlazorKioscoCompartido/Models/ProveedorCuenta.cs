using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>Movimiento de la cuenta con un proveedor: "deuda" (le debo) o "pago" (le entregué).</summary>
    [Table("proveedores_cuenta")]
    public class ProveedorCuenta : BaseModel
    {
        public const string Deuda = "deuda";
        public const string Pago = "pago";

        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("proveedor_id")]
        public long ProveedorId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Column("tipo")]
        public string Tipo { get; set; } = Deuda;

        [Column("monto")]
        public decimal Monto { get; set; }

        [Column("medio")]
        public string? Medio { get; set; }

        [Column("nota")]
        public string? Nota { get; set; }

        [Column("usuario_id")]
        public Guid? UsuarioId { get; set; }

        // ---- historial (script 35) ----
        [Column("usuario_nombre", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? UsuarioNombre { get; set; }

        /// <summary>Cuándo se cargó (lo pone la base).</summary>
        [Column("creado", Newtonsoft.Json.NullValueHandling.Ignore)]
        public DateTime? Creado { get; set; }

        /// <summary>Anulado: no cuenta para el saldo, pero queda en el historial.</summary>
        [Column("anulado", Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? Anulado { get; set; }

        [Column("anulado_por", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? AnuladoPor { get; set; }

        [Column("anulado_el", Newtonsoft.Json.NullValueHandling.Ignore)]
        public DateTime? AnuladoEl { get; set; }

        [Column("anulado_motivo", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? AnuladoMotivo { get; set; }

        public bool EsPago() => Tipo == Pago;
        public bool EstaAnulado() => Anulado == true;
    }

    /// <summary>Saldo por proveedor (vista vw_saldo_proveedores).</summary>
    [Table("vw_saldo_proveedores")]
    public class SaldoProveedor : BaseModel
    {
        [Column("kiosco_id")] public long KioscoId { get; set; }
        [Column("proveedor_id")] public long ProveedorId { get; set; }
        [Column("total_deuda")] public decimal TotalDeuda { get; set; }
        [Column("total_pagado")] public decimal TotalPagado { get; set; }
        [Column("saldo")] public decimal Saldo { get; set; }
        [Column("ultimo_movimiento")] public DateTime? UltimoMovimiento { get; set; }
    }
}
