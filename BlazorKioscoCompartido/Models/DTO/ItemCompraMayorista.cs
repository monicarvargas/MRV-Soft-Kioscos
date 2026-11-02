using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorKioscoCompartido.Models.DTO
{
    
        public class ItemCompraMayorista
        {
            public long? DetalleIdBD { get; set; } // ID del registro en compra_mayorista_detalles
            public long? ProductoId { get; set; }
            public string CodigoBarras { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public decimal PrecioCosto { get; set; }
            public int Cantidad { get; set; } = 1;
            public bool EsNuevoEnBD { get; set; }
            public decimal CostoAnteriorBD { get; set; }
            public decimal Subtotal => PrecioCosto * Cantidad;
        }
    
}
