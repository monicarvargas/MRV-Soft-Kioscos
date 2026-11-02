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

        /// <summary>Color del negocio (ver TemasColor). Lo pinta el componente TemaNegocio.</summary>
        public string ColorTema { get; set; } = TemasColor.PorDefecto;

        /// <summary>
        /// Funciones habilitadas por el plan del negocio (ver FuncionesPlan).
        /// null = todas (superadmin, o la base todavía no tiene el SQL 20).
        /// </summary>
        public HashSet<string>? FuncionesHabilitadas { get; set; }

        /// <summary>¿Este negocio tiene la función? (FuncionesPlan.Fiado, FuncionesPlan.Asistencia...)</summary>
        /// <summary>Unidades en que vende el negocio (null = Unidad, Kilo y Gramos). Ver Constants.Unidades.</summary>
        public List<string>? UnidadesNegocio { get; set; }

        public bool Tiene(string? clave) => clave == null || FuncionesHabilitadas == null || FuncionesHabilitadas.Contains(clave);

        /// <summary>Entrada del empleado logueado (asistencias.id). Al "Cambiar usuario" se marca la salida.</summary>
        public long? AsistenciaId { get; set; }
        // Guardamos el total calculado para que CobrarDesktop sepa cuánto cobrar
        public decimal TotalVentaActual { get; set; }

        // Si mañana necesitás los productos en la pantalla de cobro (ej: para el ticket), los guardamos acá
        // Reemplazá 'ItemCarrito' por el nombre real de tu modelo de renglón de carrito
        public List<ItemCarrito> CarritoActual { get; set; } = new();
        // Valor inicial
        // ---------------- MODO SOPORTE (superadmin "viendo como" otro negocio) ----------------
        /// <summary>Datos del superadmin guardados mientras mira un negocio como admin / encargado / vendedor.</summary>
        public sealed record SesionGuardada(long KioscoId, Guid? UsuarioId, string? UsuarioNombre, string? UsuarioRol,
                                            string? NombreNegocio, bool UsarRedondeo, int MultiploRedondeo, string ColorTema);

        public SesionGuardada? SesionSuperAdmin { get; set; }
        public bool EnModoSoporte => SesionSuperAdmin != null;

        /// <summary>El superadmin pasa a ver el negocio <paramref name="k"/> con el rol elegido.</summary>
        public void EntrarComo(Kiosco k, string rol)
        {
            // si ya estaba viendo otro negocio, se conserva la sesión ORIGINAL del superadmin
            SesionSuperAdmin ??= new SesionGuardada(KioscoId, UsuarioId, UsuarioNombre, UsuarioRol,
                                                    NombreNegocio, UsarRedondeo, MultiploRedondeo, ColorTema);
            KioscoId = k.Id;
            NombreNegocio = k.NombreKiosco;
            UsarRedondeo = k.UsarRedondeo ?? true;
            MultiploRedondeo = k.MultiploRedondeo ?? 0;
            ColorTema = TemasColor.Buscar(k.ColorTema).Clave;
            UsuarioRol = rol;
            UsuarioNombre = (SesionSuperAdmin.UsuarioNombre ?? "superadmin") + " (soporte)";
            FuncionesHabilitadas = null;      // las carga AdminNegocios apenas entra (las del plan de ese negocio)
            UnidadesNegocio = k.Unidades;
            AsistenciaId = null;              // el soporte no ficha
            CarritoActual = new();
            TotalVentaActual = 0;
            NotifyStateChanged();
        }

        /// <summary>Vuelve a ser el superadmin, en su propio negocio.</summary>
        public void VolverASuperAdmin()
        {
            var s = SesionSuperAdmin;
            if (s == null) return;
            KioscoId = s.KioscoId; UsuarioId = s.UsuarioId; UsuarioNombre = s.UsuarioNombre; UsuarioRol = s.UsuarioRol;
            NombreNegocio = s.NombreNegocio; UsarRedondeo = s.UsarRedondeo; MultiploRedondeo = s.MultiploRedondeo; ColorTema = s.ColorTema;
            SesionSuperAdmin = null;
            FuncionesHabilitadas = null;      // el superadmin ve todo
            UnidadesNegocio = null;
            CarritoActual = new();
            TotalVentaActual = 0;
            NotifyStateChanged();
        }

        // 2. EL MÉTODO PARA AVISAR A LOS DEMÁS COMPONENTES
        public void NotifyStateChanged()
        {
            OnChange?.Invoke();
        }
    }
}