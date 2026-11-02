using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    [Table("lista_pedido_detalles")]
    public class ListaPedidoDetalle : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("pedido_id")]
        public long PedidoId { get; set; }

        [Column("producto_id")]
        public long ProductoId { get; set; }

        [Column("codigo_barras_proveedor")]
        public string? CodigoBarrasProveedor { get; set; }

        [Column("nombre_producto")]
        public string NombreProducto { get; set; } = string.Empty;

        [Column("stock_actual")]
        public decimal StockActual { get; set; }

        [Column("precio_costo")]
        public decimal PrecioCosto { get; set; }

        [Column("cantidad_comprar")]
        public decimal CantidadComprar { get; set; } = 1;

        [Column("subtotal_estimado")]
        public decimal SubtotalEstimado { get; set; }

        /// <summary>
        /// Unidades que trae cada bulto de este proveedor. Si es mayor a 1, CantidadComprar son BULTOS y PrecioCosto es el precio del BULTO
        /// (así se le pide al proveedor). Al recibir el pedido se convierte a unidades para el stock. 1 = se pide suelto.
        /// </summary>
        [Column("unidades_por_bulto")]
        public int UnidadesPorBulto { get; set; } = 1;

        /// <summary>CAJA, PACK, BULTO... con que se pidió (script 34). null = bulto / suelto.</summary>
        [Column("tipo_bulto", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? TipoBulto { get; set; }
    }
}