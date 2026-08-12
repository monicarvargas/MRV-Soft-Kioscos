using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models.Vistas
{
    [Table("vw_resumen_deudas_personas ")]
    public class ResumenDeudores
    {
        [Column("kiosco_id")]
        public long KioscoId { get; set; }

    }
}
