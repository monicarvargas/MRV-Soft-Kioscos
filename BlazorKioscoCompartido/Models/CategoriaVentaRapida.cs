using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.ComponentModel.DataAnnotations;

namespace BlazorKioscoCompartido.Models
{
    [Table("categorias_venta_rapida")]
    public class CategoriaVentaRapida:BaseModel
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Column("icono")]
        [StringLength(50)]
        public string? Icono { get; set; } // Puede ser nulo si no le pones ícono

        [Column("orden")]
        public int Orden { get; set; } = 0;

        [Column("habilitado")]
        public bool Habilitado { get; set; } = true;
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        // Propiedad de navegación por si querés traer los productos asociados directamente desde C#
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
