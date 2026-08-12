using BlazorKioscoCompartido.Models;

namespace BlazorKioscoCompartido.Services
{
    public class AppState
    {
        // 1. EL EVENTO MÁGICO QUE FALTABA
        public event Action? OnChange;
        public long KioscoId { get; set; }
        public Guid? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? UsuarioRol { get; set; }//admin, vendedor, cajero, etc.
        public string?NombreNegocio { get; set; }
        
        // --- NUEVAS PREFERENCIAS DINÁMICAS ---
        public bool UsarRedondeo { get; set; }
        public int MultiploRedondeo { get; set; } = 0;
        // Guardamos el total calculado para que CobrarDesktop sepa cuánto cobrar
        public decimal TotalVentaActual { get; set; }

        // Si mañana necesitás los productos en la pantalla de cobro (ej: para el ticket), los guardamos acá
        // Reemplazá 'ItemCarrito' por el nombre real de tu modelo de renglón de carrito
        public List<ItemCarrito> CarritoActual { get; set; } = new();
        // Valor inicial
        // 2. EL MÉTODO PARA AVISAR A LOS DEMÁS COMPONENTES
        public void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}