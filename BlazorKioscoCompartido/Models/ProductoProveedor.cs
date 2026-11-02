using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Supabase.Postgrest.Attributes.ReferenceAttribute;

namespace BlazorKioscoCompartido.Models
{
    [Table("productos_proveedores")]
    public class ProductoProveedor : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("id_producto")]
        public long IdProducto { get; set; }

        [Column("id_proveedor")]
        public long IdProveedor { get; set; }

        [Column("id_kiosco")]
        public long IdKiosco { get; set; }

        [Column("habilitado")]
        public bool Habilitado { get; set; } = true;
        [Column("es_principal")]
        public bool EsPrincipal { get; set; } = true;

        [Column("codigo_barras_proveedor")]
        public string? CodigoBarrasProveedor { get; set; }

        /// <summary>Cuántas unidades de venta trae lo que vende este proveedor (bulto, caja, pack). 1 = suelto.</summary>
        [Column("unidades_por_bulto")]
        public int UnidadesPorBulto { get; set; } = 1;

        /// <summary>Cómo viene: BULTO, CAJA, PACK, BOLSA... (null = suelto o "bulto"). Script 34. Ver Constants/TiposBulto.</summary>
        [Column("tipo_bulto", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? TipoBulto { get; set; }

        /// <summary>Cómo nombra el proveedor a este producto (ej: "ABRAZADERA 16MM X 100 UNID"). El nombre del producto en el sistema puede ser otro.</summary>
        [Column("descripcion_proveedor")]
        public string? DescripcionProveedor { get; set; }

        // Relación opcional para cargar los datos del proveedor
        //[Reference(typeof(Proveedor), joinType: Supabase.Postgrest.Constants.JoinType.Left)]


        [Reference(typeof(Proveedor), foreignKey: "productos_proveedores_id_proveedor_fkey", joinType: JoinType.Left)]
        public Proveedor? Proveedor { get; set; }
    }
}
