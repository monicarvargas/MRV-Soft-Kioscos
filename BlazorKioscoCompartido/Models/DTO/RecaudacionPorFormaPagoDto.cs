using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorKioscoCompartido.Models.DTO
{
    public class RecaudacionPorFormaPagoDto
    {
        public string FormaPago { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}
