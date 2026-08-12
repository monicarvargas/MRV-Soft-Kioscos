using BlazorKioscoCompartido.Models;
using BlazorKioscoCompartido.Models.DTO;

namespace BlazorKioscoCompartido.Services
{
    public class FiadoService
    {
        // Evento para notificar a la interfaz Blazor que los datos del fiado cambiaron
        public event Action? OnChange;

        public long PersonaId { get; set; }
        public string NombrePersona { get; set; } = "";
        public long NumeroMovimiento { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>
        /// Importe que entrega el cliente en el momento del cobro parcial o total.
        /// </summary>
        public decimal ImporteRecibido { get; set; }

        /// <summary>
        /// Pagos o entregas a cuenta realizadas anteriormente.
        /// </summary>
        public decimal Pagado { get; set; }

        /// <summary>
        /// Indica si el cobro se calcula con el precio actualizado (true) o histórico (false).
        /// </summary>
        public bool CobrarPrecioActual { get; set; } = true;

        /// <summary>
        /// Productos fiados que integran la deuda actual.
        /// </summary>
        public List<DeudasPendientesDetalladoDTO> Items { get; set; } = new();

        // --- CÁLCULOS AUTOMÁTICOS ---

        /// <summary>
        /// Suma total de los ítems a precio histórico.
        /// </summary>
        public decimal TotalHistorico => Items.Sum(x => x.SubtotalHistorico);

        /// <summary>
        /// Suma total de los ítems a precio actualizado.
        /// </summary>
        public decimal TotalActual => Items.Sum(x => x.SubtotalActual);

        /// <summary>
        /// Total elegido según la modalidad seleccionada.
        /// </summary>
        public decimal TotalSeleccionado => CobrarPrecioActual ? TotalActual : TotalHistorico;

        /// <summary>
        /// Saldo total adeudado (Total Seleccionado - Pagos previos).
        /// </summary>
        public decimal Saldo => Math.Max(0, TotalSeleccionado - Pagado);

        /// <summary>
        /// Saldo restante después de aplicar la entregada actual (ImporteRecibido).
        /// </summary>
        public decimal NuevoFiado => Math.Max(0, Saldo - ImporteRecibido);

        /// <summary>
        /// Indica si con la entrega actual se cancela por completo la deuda.
        /// </summary>
        public bool DeudaCancelada => NuevoFiado <= 0;

        // --- MÉTODOS DE ACCIÓN (Similares a CarritoService) ---

        /// <summary>
        /// Inicializa el servicio con los datos de un deudor específico y sus ítems de la vista.
        /// </summary>
        public void CargarDeudor(long personaId, string nombrePersona, List<DeudasPendientesDetalladoDTO> items, decimal pagosAnteriores = 0)
        {
            PersonaId = personaId;
            NombrePersona = nombrePersona;
            Items = items ?? new List<DeudasPendientesDetalladoDTO>();
            Pagado = pagosAnteriores;
            ImporteRecibido = Saldo; // Por defecto sugiere cobrar el total del saldo
            CobrarPrecioActual = true;

            NotificarCambio();
        }

        /// <summary>
        /// Alterna entre cobrar con Precio Actual o Precio Histórico.
        /// </summary>
        public void CambiarModoPrecio(bool usarPrecioActual)
        {
            CobrarPrecioActual = usarPrecioActual;
            // Ajustamos el importe recibido sugerido si estaba igualado al saldo anterior
            ImporteRecibido = Saldo;
            NotificarCambio();
        }

        /// <summary>
        /// Permite cambiar el importe recibido/entrega actual.
        /// </summary>
        public void EstablecerImporteRecibido(decimal monto)
        {
            ImporteRecibido = Math.Max(0, monto);
            NotificarCambio();
        }

        /// <summary>
        /// Elimina un ítem específico de la lista antes de cobrar (si no se desea cobrar/saldar ese ítem).
        /// </summary>
        public void RemoverItem(DeudasPendientesDetalladoDTO item)
        {
            Items.Remove(item);
            if (ImporteRecibido > Saldo)
            {
                ImporteRecibido = Saldo;
            }
            NotificarCambio();
        }

        /// <summary>
        /// Vacía y reinicia el servicio.
        /// </summary>
        public void Vaciar()
        {
            PersonaId = 0;
            NombrePersona = "";
            NumeroMovimiento = 0;
            ImporteRecibido = 0;
            Pagado = 0;
            Items.Clear();
            CobrarPrecioActual = true;

            NotificarCambio();
        }

        private void NotificarCambio() => OnChange?.Invoke();
    }
}
