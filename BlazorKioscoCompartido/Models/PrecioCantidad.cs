using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>Precio por cantidad (pack): llevando <see cref="Cantidad"/> unidades se paga <see cref="Precio"/> en total. Script 27.</summary>
    [Table("precios_cantidad")]
    public class PrecioCantidad : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("producto_id")]
        public long ProductoId { get; set; }

        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Column("precio")]
        public decimal Precio { get; set; }
    }
}
