using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlazorKioscoCompartido.Models.DTO
{
    public class ItemPedidoViewModelDTO
    {
        public Producto Producto { get; set; } = new();
        public bool IncluirEnPedido { get; set; } = true;
        public decimal StockActualLocal { get; set; }
        public decimal PrecioCostoLocal { get; set; }
        public decimal CantidadAComprar { get; set; }

        // Si el proveedor vende por bulto: CantidadAComprar son bultos y PrecioCostoLocal es el precio del bulto
        public int UnidadesPorBulto { get; set; } = 1;
        public string? DescripcionProveedor { get; set; }
        public bool PorBulto => UnidadesPorBulto > 1;
        /// <summary>CAJA, PACK, BULTO... (ver Constants/TiposBulto)</summary>
        public string? TipoBulto { get; set; }
        /// <summary>El costo estimado es el precio del bulto cargado en el producto (no costo x unidades).</summary>
        public bool CostoDelBulto { get; set; }
        /// <summary>No es del proveedor elegido: el "viene por" sale del último proveedor cargado (o del bulto del producto).</summary>
        public bool BultoDeOtroLado { get; set; }
        /// <summary>Viene por caja / pack / bulto (sepa o no cuántas unidades trae).</summary>
        public bool TieneTipo => !BlazorKioscoCompartido.Constants.TiposBulto.EsSuelto(TipoBulto, UnidadesPorBulto);

        /// <summary>Se vende por peso: el stock y el bulto van en GRAMOS y el costo es por KILO (ver Services/Bultos.cs).</summary>
        public bool EnKilos => BlazorKioscoCompartido.Services.Utilidades.EsVentaPorPeso(Producto);

        // Subtotal de costo acumulado (por peso sin bulto: la cantidad va en gramos y el costo es por kilo)
        public decimal SubtotalEstimado => EnKilos && !PorBulto
            ? CantidadAComprar / 1000m * PrecioCostoLocal
            : CantidadAComprar * PrecioCostoLocal;
    }
}
