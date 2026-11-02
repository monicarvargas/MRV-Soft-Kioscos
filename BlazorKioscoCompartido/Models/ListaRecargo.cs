using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>
    /// Un tramo del recargo de una lista (script 33). Ej. Lista 2 NOCHE: más de $500 hasta $900 → + $100.
    /// Se usa el tramo donde "precio &gt; Desde y precio &lt;= Hasta" (Hasta vacío = sin tope).
    /// </summary>
    [Table("listas_recargos")]
    public class ListaRecargo : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("lista_precio_id")]
        public long ListaPrecioId { get; set; }

        [Column("desde")]
        public decimal Desde { get; set; }

        [Column("hasta")]
        public decimal? Hasta { get; set; }

        [Column("monto")]
        public decimal Monto { get; set; }

        [Column("es_porcentaje")]
        public bool EsPorcentaje { get; set; }
    }
}
