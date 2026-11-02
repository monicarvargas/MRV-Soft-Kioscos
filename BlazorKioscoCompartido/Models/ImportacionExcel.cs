using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    public static class ImportacionTipo
    {
        public const string Factura = "factura";   // trae cantidades: arma una compra para confirmar en "Recibir mercadería"
        public const string Precios = "precios";   // lista de precios: solo actualiza costos y precios
        public const string Carga = "carga";       // alta masiva / carga inicial de productos
    }

    [Table("importaciones_excel")]
    public class ImportacionExcel : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        /// <summary>La misma fecha leída como texto ("2026-10-15 02:38:00+00"), para mostrar la hora bien.
        /// Solo viene si la consulta pide "fecha_txt:fecha::text"; nunca se guarda.</summary>
        [Column("fecha_txt", ignoreOnInsert: true, ignoreOnUpdate: true)]
        public string? FechaTexto { get; set; }

        [Column("nombre_archivo")]
        public string NombreArchivo { get; set; } = string.Empty;

        [Column("hash_archivo")]
        public string? HashArchivo { get; set; }

        [Column("tipo")]
        public string Tipo { get; set; } = ImportacionTipo.Factura;

        [Column("proveedor_id")]
        public long? ProveedorId { get; set; }

        [Column("filas_total")]
        public int FilasTotal { get; set; }

        [Column("creados")]
        public int Creados { get; set; }

        [Column("actualizados")]
        public int Actualizados { get; set; }

        [Column("omitidos")]
        public int Omitidos { get; set; }

        [Column("errores")]
        public int Errores { get; set; }

        [Column("stock_modo")]
        public string? StockModo { get; set; }

        [Column("regla_precios")]
        public string? ReglaPrecios { get; set; }

        [Column("compra_id")]
        public long? CompraId { get; set; }

        [Column("mapeo")]
        public string? Mapeo { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "ok";

        [Column("observaciones")]
        public string? Observaciones { get; set; }

        /// <summary>Contenido del archivo tal como quedó al importar (JSON con una entrada por fila). Se ve con el ojito del historial.</summary>
        [Column("detalle")]
        public string? Detalle { get; set; }
    }
}
