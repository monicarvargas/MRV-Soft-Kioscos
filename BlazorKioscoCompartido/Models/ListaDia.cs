using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>
    /// Día en que se cobra con otra lista todo el día (script 36): una fecha puntual (feriado) o un día de la semana (domingos).
    /// </summary>
    [Table("listas_dias")]
    public class ListaDia : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("lista_precio_id")]
        public long ListaPrecioId { get; set; }

        /// <summary>Un día puntual (ej: un feriado).</summary>
        [Column("fecha")]
        public DateTime? Fecha { get; set; }

        /// <summary>Todas las semanas: 0 = domingo ... 6 = sábado.</summary>
        [Column("dia_semana")]
        public int? DiaSemana { get; set; }

        public bool Aplica(DateTime dia) =>
            (Fecha is DateTime f && f.Date == dia.Date) || (DiaSemana is int d && d == (int)dia.DayOfWeek);
    }
}
