
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using static Supabase.Postgrest.Attributes.ReferenceAttribute;


namespace BlazorKioscoCompartido.Models
{
    [Table("productos")]
    public class Producto: BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("kiosco_id")]
        public long? KioscoId { get; set; }
        [Column("familia_id")]
        public long? FamiliaId { get; set; }
        [Column("proveedor_id")]
        public long? ProveedorId { get; set; }
        [Column("codigo_barras")]
        public string? CodigoBarras { get; set; }
        [Column("nombre")]
        public string? Nombre { get; set; }
        [Column("marca")]
        public string? Marca { get; set; }
        [Column("precio_costo")]
        public decimal? PrecioCosto { get; set; }
        [Column("precio_venta")]
        public decimal? PrecioVenta { get; set; }
        [Column("utilidad_pv")]
        public decimal? UtilidadPV { get; set; }
        [Column("precio_lista2")]
        public decimal? PrecioLista2 { get; set; }
        [Column("utilidad_lista2")]
        public decimal? UtilidadLista2 { get; set; }
        [Column("precio_lista3")]
        public decimal? PrecioLista3 { get; set; }
        [Column("utilidad_lista3")]
        public decimal? UtilidadLista3 { get; set; }
        [Column("precio_lista4")]
        public decimal? PrecioLista4 { get; set; }
        [Column("utilidad_lista4")]
        public decimal? UtilidadLista4 { get; set; }
        [Column("precio_lista5")]
        public decimal? PrecioLista5 { get; set; }
        [Column("utilidad_lista5")]
        public decimal? UtilidadLista5 { get; set; }
        [Column("precio_venta_por_mayor")]
        public decimal? PrecioVentaxMayor { get; set; }
        [Column("utilidad_x_mayor")]
        public decimal? UtilidadVentaxMayor { get; set; }
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
        public DateTime? FechaActualizacion { get; set; } = DateTime.UtcNow.ToLocalTime();
        [Column("activo")]
        public bool Activo { get; set; } = true;
        [Column("es_combo")]
        public bool? EsCombo { get; set; } 
        [Column("fecha_vigencia_desde_combo")]
        public DateTime? FechaVigenciaDesdeCombo { get; set; }
        [Column("fecha_vigencia_hasta_combo")]
        public DateTime? FechaVigenciaHastaCombo { get; set; }
        [Column("limite_unidades_combo")]
        public int? LimiteUnidadesCombo { get; set; }

        //******************************************

        [Column("categoria_venta_rapida_id")]
        public long? CategoriaVentaRapidaId { get; set; }
        [Column("mostrar_en_venta_rapida")]
        public bool MostrarEnVentaRapida { get; set; } = false;

        [Column("orden_venta_rapida")]
        public int OrdenVentaRapida { get; set; } = 0;

        [Column("admite_importe")]
        public bool AdmiteImporte { get; set; } = false;

        /// <summary>true = este producto NO usa el redondeo del negocio (cosas chicas: gomitas a $30, caramelos...). Script 26.</summary>
        [Column("sin_redondeo", Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? SinRedondeo { get; set; }

        /// <summary>Subfamilia dentro de la familia (ej: GOLOSINAS → GOMITAS). Opcional. Script 26.</summary>
        [Column("subfamilia_id", Newtonsoft.Json.NullValueHandling.Ignore)]
        public long? SubfamiliaId { get; set; }

        // Último bulto cargado en "Lo compro por bulto" (para ver si aumentó). Script 32.
        [Column("bulto_precio", Newtonsoft.Json.NullValueHandling.Ignore)]
        public decimal? BultoPrecio { get; set; }

        [Column("bulto_unidades", Newtonsoft.Json.NullValueHandling.Ignore)]
        public int? BultoUnidades { get; set; }

        [Column("bulto_fecha", Newtonsoft.Json.NullValueHandling.Ignore)]
        public DateTime? BultoFecha { get; set; }

        /// <summary>¿A este producto se le aplica el redondeo del negocio?</summary>
        public bool UsaRedondeo(BlazorKioscoCompartido.Services.AppState? state) =>
            state != null && state.UsarRedondeo && SinRedondeo != true;

        [Reference(typeof(Familia), foreignKey: "fk_familia_producto", joinType: JoinType.Left)]
        public Familia? Familia { get; set; }
    }
}
