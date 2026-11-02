using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>Subfamilia dentro de una familia (ej: GOLOSINAS → ALFAJORES, CHICLES, GOMITAS). Script 26.</summary>
    [Table("subfamilias")]
    public class Subfamilia : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("familia_id")]
        public long FamiliaId { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = "";

        /// <summary>% de utilidad propio (ej: GOMITAS 45). null = el de la familia. Script 37.</summary>
        [Column("utilidad", Newtonsoft.Json.NullValueHandling.Ignore)]
        public decimal? Utilidad { get; set; }

        /// <summary>Utilidad que corresponde a un producto: la de la subfamilia, si no la de la familia, si no 40%.</summary>
        public static decimal Para(IEnumerable<Familia> familias, IEnumerable<Subfamilia> subfamilias, long? familiaId, long? subfamiliaId)
        {
            var s = subfamiliaId is long sid ? subfamilias.FirstOrDefault(x => x.Id == sid) : null;
            if (s?.Utilidad is decimal us) return us;
            var f = familiaId is long fid ? familias.FirstOrDefault(x => x.Id == fid) : null;
            if (f?.Utilidad is decimal uf) return uf;
            return UtilidadGeneral;
        }

        /// <summary>La que se usa si la familia no tiene una propia.</summary>
        public const decimal UtilidadGeneral = 40m;
    }
}
