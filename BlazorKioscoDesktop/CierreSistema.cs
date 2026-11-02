namespace BlazorKioscoDesktop
{
    /// <summary>
    /// Cierre del programa de escritorio. La X de la ventana (y Alt+F4) no cierra mientras hay alguien adentro:
    /// se cierra con "Cerrar el sistema" del menú, que marca la salida del empleado. En el login sí cierra.
    /// </summary>
    public static class CierreSistema
    {
        /// <summary>true = el botón "Cerrar el sistema" ya autorizó cerrar.</summary>
        public static bool Permitido { get; set; }

        /// <summary>¿Hay alguien logueado? Lo define el MainLayout.</summary>
        public static Func<bool> HaySesion { get; set; } = () => false;

        /// <summary>Se tocó la X con alguien adentro (el MainLayout muestra un aviso).</summary>
        public static event Action? IntentoCerrar;

        public static void AvisarIntento() => IntentoCerrar?.Invoke();
    }
}
