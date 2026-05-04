
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscos_v1.Models
{
    [Table("productos")]
    public class Producto: BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }
        [Column("kiosco_id")]
        public int? KioscoId { get; set; }
        [Column("familia_id")]
        public int FamiliaId { get; set; }
        [Column("proveedor_id")]
        public int? ProveedorId { get; set; }
        [Column("codigo_barras")]
        public string? CodigoBarras { get; set; }
        [Column("nombre")]
        public string? Nombre { get; set; }
        [Column("marca")]
        public string? Marca { get; set; }
        [Column("precio_costo")]
        public double? PrecioCosto { get; set; }
        [Column("precio_venta")]
        public double? PrecioVenta { get; set; }
        [Column("utilidad_pv")]
        public double? UtilidadPV { get; set; }
        [Column("precio_lista2")]
        public double? PrecioLista2 { get; set; }
        [Column("utilidad_lista2")]
        public double? UtilidadLista2 { get; set; }
        [Column("precio_lista3")]
        public double? PrecioLista3 { get; set; }
        [Column("utilidad_lista3")]
        public double? UtilidadLista3 { get; set; }
        [Column("precio_lista4")]
        public double? PrecioLista4 { get; set; }
        [Column("utilidad_lista4")]
        public double? UtilidadLista4 { get; set; }
        [Column("precio_lista5")]
        public double? PrecioLista5 { get; set; }
        [Column("utilidad_lista5")]
        public double? UtilidadLista5 { get; set; }
        [Column("precio_venta_por_mayor")]
        public double? PrecioVentaxMayor { get; set; }
        [Column("utilidad_x_mayor")]
        public double? UtilidadVentaxMayor { get; set; }
        [Column("cant_por_mayor")]
        public int? CantxMayor { get; set; }
        [Column("venta_por_peso")]
        public bool? VentaxPeso { get; set; }
        [Column("unidad")]
        public string? UnidadMedida { get; set; }
        [Column("stock_actual")]
        public int? StockActual { get; set; }
        [Column("stock_ideal")]
        public int? StockIdeal { get; set; }
        [Column("stock_minimo")]
        public int? StockMinimo { get; set; }
        [Column("nota_producto")]
        public string? NotaProducto { get; set; }
        [Column("imagen_url")]
        public  string? ImagenUrl { get; set; }
        [Column("fecha_actualizacion")]
        public DateTime FechaActualizacion { get; set; }
        [Column("activo")]
        public bool Activo { get; set; }
    }
}
