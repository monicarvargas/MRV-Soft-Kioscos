using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscos_v1.Models
{
    [Table("horarios_ventas")]
    public class HorarioVenta : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("hora_inicio")]
        public TimeSpan HoraInicio { get; set; }

        [Column("hora_fin")]
        public TimeSpan HoraFin { get; set; }

        [Column("tipo_ajuste")]
        public string? TipoAjuste { get; set; } // 'LISTA' o 'AJUSTE'
        [Column("lista_precio_id")]
        public int ListaPrecioId { get; set; }

        [Column("porcentaje_comision")]
        public decimal PorcentajeComision { get; set; }

        [Column("porcentaje_descuento")]
        public decimal PorcentajeDescuento { get; set; }
    }
}