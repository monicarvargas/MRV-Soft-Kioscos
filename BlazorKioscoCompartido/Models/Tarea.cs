using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>Una tarea que cargó un empleado (ver _sql\23.tareas.sql).</summary>
    [Table("tareas")]
    public class Tarea : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("usuario_id")]
        public Guid? UsuarioId { get; set; }

        /// <summary>Nombre del usuario al momento de cargarla (queda aunque después se borre el usuario).</summary>
        [Column("usuario_nombre")]
        public string? UsuarioNombre { get; set; }

        /// <summary>Día de la tarea, como texto "yyyy-MM-dd" (así no se corre por la zona horaria).</summary>
        [Column("fecha")]
        public string Fecha { get; set; } = "";

        /// <summary>Hora en que se cargó ("HH:mm", hora del equipo).</summary>
        [Column("hora")]
        public string? Hora { get; set; }

        [Column("descripcion")]
        public string Descripcion { get; set; } = "";

        /// <summary>Cuánto tiempo le llevó, en minutos (opcional).</summary>
        [Column("minutos")]
        public int? Minutos { get; set; }
    }
}
