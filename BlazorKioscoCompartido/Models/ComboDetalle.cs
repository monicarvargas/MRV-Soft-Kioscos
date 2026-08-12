using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BlazorKioscoCompartido.Models
{
    [Table("combo_detalle")]
    public class ComboDetalle : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("combo_id")]
        public long ComboId { get; set; } // El ID del Producto "Padre" (que tiene es_combo = true)

        [Column("producto_hijo_id")]
        public long ProductoHijoId { get; set; } // El ID del producto que compone el combo (ej: la Coca)

        [Column("cantidad")]
        public int Cantidad { get; set; } // Cuántas unidades de este producto lleva el combo
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
    }
}
