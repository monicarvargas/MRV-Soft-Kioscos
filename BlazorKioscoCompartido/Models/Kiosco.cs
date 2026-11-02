using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BlazorKioscoCompartido.Models
{
    [Table("kioscos")]
    public class Kiosco:BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("nombre_negocio")]
        public string? NombreKiosco { get; set; }
        [Column("cuit_dni")]
        public string? CuitDni { get; set; }
        [Column("fecha_alta")]
        public DateTime FechaAlta { get; set; } = DateTime.UtcNow.ToLocalTime();
        [Column("estado")]
        public bool? Estado { get; set; }
        [Column("fecha_vencimiento_abono")]
        public DateTime? FechaVencimientoAbono { get; set; }
        [Column("dias_anticipacion_aviso")]
        public int? DiasAnticipacionAviso { get; set; }
        [Column("notas_administrativas")]
        public string? NotasAdministrativas { get; set; }
        [Column("codigo_vinculacion")]
        public string? CodigoVinculacion { get; set; } 

        [Column("usar_redondeo")]
        public bool? UsarRedondeo { get; set; }

        [Column("multiplo_redondeo")]
        public int? MultiploRedondeo { get; set; }

        /// <summary>Color elegido por el dueño (clave de TemasColor: azul, rosa, verde...).</summary>
        [Column("color_tema", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? ColorTema { get; set; }

        // ---- Plan comercial (ver _sql\20.planes_funciones.sql). Solo los cambia el superadmin. ----
        /// <summary>basico | medio | avanzado (null = la base todavía no tiene la columna).</summary>
        [Column("plan_codigo", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? PlanCodigo { get; set; }

        /// <summary>Funciones que se le suman al plan, solo para este negocio.</summary>
        [Column("funciones_extra", Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<string>? FuncionesExtra { get; set; }

        /// <summary>Funciones del plan que este negocio NO tiene.</summary>
        [Column("funciones_quitadas", Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<string>? FuncionesQuitadas { get; set; }

        /// <summary>Unidades en que vende (UNIDAD, KG, GR, LITROS, METROS, BULTO). null = Unidad, Kilo y Gramos. Ver Constants.Unidades.</summary>
        [Column("unidades", Newtonsoft.Json.NullValueHandling.Ignore)]
        public List<string>? Unidades { get; set; }

        /// <summary>
        /// Qué puede hacer cada perfil, si el dueño lo cambió: { "encargado": ["mercaderia", ...], "vendedor": [...] }.
        /// null = lo de fábrica. Ver Services\Roles.cs y _sql\38.permisos_perfiles.sql.
        /// </summary>
        [Column("permisos_roles", Newtonsoft.Json.NullValueHandling.Ignore)]
        public Dictionary<string, List<string>>? PermisosRoles { get; set; }

    }
}
