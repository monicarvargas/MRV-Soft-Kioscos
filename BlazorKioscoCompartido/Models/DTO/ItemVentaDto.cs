using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorKioscoCompartido.Models.DTO
{
    public class ItemVentaDto
    {
        public int? ProductoId { get; set; } // Null si es un ítem rápido ("Varios")
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; } = 1;
        public bool EsVario { get; set; } = false;

        public decimal Subtotal => PrecioUnitario * Cantidad;
    }
}
