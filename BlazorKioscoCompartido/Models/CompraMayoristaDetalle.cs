using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    [Table("compra_mayorista_detalles")]
    public class CompraMayoristaDetalle : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("compra_id")]
        public long CompraId { get; set; }

        [Column("producto_id")]
        public long? ProductoId { get; set; }

        [Column("codigo_barras")]
        public string? CodigoBarras { get; set; }

        [Column("nombre_producto")]
        public string NombreProducto { get; set; } = string.Empty;

        [Column("precio_costo")]
        public decimal PrecioCosto { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; } = 1;

        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        [Column("es_nuevo")]
        public bool EsNuevo { get; set; } = false;

        [Column("procesado")]
        public bool Procesado { get; set; } = false;

        // ---- Compras unificadas ----

        /// <summary>Código de barras que usa el proveedor para este producto (el de la boleta).</summary>
        [Column("codigo_barras_proveedor")]
        public string? CodigoBarrasProveedor { get; set; }

        /// <summary>Costo que tenía el producto antes de esta compra.</summary>
        [Column("costo_anterior")]
        public decimal? CostoAnterior { get; set; }

        /// <summary>Si vino de un pedido: la cantidad que se había pedido.</summary>
        [Column("cantidad_pedida")]
        public int? CantidadPedida { get; set; }

        /// <summary>Si se compró por bulto: unidades que trae cada bulto (Cantidad y PrecioCosto siempre quedan por unidad). 1 = suelto.</summary>
        [Column("unidades_por_bulto")]
        public int UnidadesPorBulto { get; set; } = 1;
    }
}