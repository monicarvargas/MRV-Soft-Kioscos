using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>De dónde viene la compra.</summary>
    public static class CompraOrigen
    {
        public const string Calculadora = "calculadora";   // estimación hecha en el mayorista
        public const string Pedido = "pedido";             // se recibió un pedido a proveedor
        public const string Boleta = "boleta";             // se cargó desde la boleta (papel o foto)
        public const string Excel = "excel";               // factura o remito que mandó el proveedor en Excel
    }

    /// <summary>En qué etapa está. Solo "confirmada" tocó stock y precios.</summary>
    public static class CompraEstado
    {
        public const string EnCurso = "en_curso";          // se está armando (calculadora)
        public const string Cerrada = "cerrada";           // lista terminada, falta confirmarla
        public const string Confirmada = "confirmada";     // stock y precios ya actualizados
    }

    [Table("compras_mayoristas")]
    public class CompraMayorista : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        /// <summary>Cuándo se creó el registro en el sistema.</summary>
        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Column("total_compra")]
        public decimal TotalCompra { get; set; }

        /// <summary>Se mantiene por compatibilidad: es true cuando Estado == "confirmada".</summary>
        [Column("procesado")]
        public bool Procesado { get; set; } = false;

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        // ---- Compras unificadas ----

        [Column("origen")]
        public string Origen { get; set; } = CompraOrigen.Calculadora;

        [Column("estado")]
        public string Estado { get; set; } = CompraEstado.EnCurso;

        [Column("proveedor_id")]
        public long? ProveedorId { get; set; }

        /// <summary>Si salió de un pedido a proveedor, el id de ese pedido.</summary>
        [Column("pedido_id")]
        public long? PedidoId { get; set; }

        [Column("numero_comprobante")]
        public string? NumeroComprobante { get; set; }

        /// <summary>Fecha que figura en la boleta.</summary>
        [Column("fecha_compra")]
        public DateTime? FechaCompra { get; set; }

        /// <summary>Total impreso en la boleta (para controlar lo cargado).</summary>
        [Column("total_boleta")]
        public decimal? TotalBoleta { get; set; }

        [Column("fecha_confirmada")]
        public DateTime? FechaConfirmada { get; set; }

        [Column("foto_url")]
        public string? FotoUrl { get; set; }
    }
}
