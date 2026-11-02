using BlazorKioscoCompartido.Models;
using Newtonsoft.Json;

namespace BlazorKioscoCompartido.Services.SinInternet
{
    /// <summary>Lo que necesita la app de escritorio para funcionar sin internet (la arma VentaSinInternet).</summary>
    public sealed class OpcionesSinInternet
    {
        /// <summary>Carpeta de la PC donde se guarda todo (copia de datos, ventas pendientes, usuarios).</summary>
        public string Carpeta { get; set; } = "";
        public string SupabaseUrl { get; set; } = "";
        public string AnonKey { get; set; } = "";
        /// <summary>¿La PC tiene red? (en el escritorio lo dice Windows). null = se averigua probando.</summary>
        public Func<bool>? HayRed { get; set; }
    }

    /// <summary>Copia de los datos de un negocio para vender sin internet.</summary>
    public sealed class CopiaNegocio
    {
        public long KioscoId { get; set; }
        public DateTime Fecha { get; set; }
        public List<Producto> Productos { get; set; } = new();
        public List<ComboDetalle> Combos { get; set; } = new();
        public List<CategoriaVentaRapida> Categorias { get; set; } = new();
        public List<FormaPago> FormasPago { get; set; } = new();
        public List<HorarioVenta> Horarios { get; set; } = new();
        public List<ListaPrecio> Listas { get; set; } = new();
        public List<PrecioCantidad> Escalas { get; set; } = new();   // precios por cantidad (script 27)
        public List<ListaRecargo> Recargos { get; set; } = new();    // tramos de recargo de las listas (script 33)
        public List<ListaDia> Dias { get; set; } = new();            // domingos / feriados con otra lista (script 36)
    }

    /// <summary>Un usuario que ya entró con internet en esta PC (para poder entrar sin internet).</summary>
    public sealed class UsuarioGuardado
    {
        public string Codigo { get; set; } = "";       // código del negocio (en mayúsculas)
        public string Usuario { get; set; } = "";      // nombre de usuario (en minúsculas)
        public string Sal { get; set; } = "";
        public string Hash { get; set; } = "";         // la contraseña NO se guarda: solo un hash con sal
        public string LoginJson { get; set; } = "";    // lo que devolvió iniciar_sesion (sin contraseña)
        public List<string>? Funciones { get; set; }   // funciones del plan
        public DateTime Fecha { get; set; }
    }

    /// <summary>Una venta hecha sin internet, esperando para subirse (mismo formato que espera subir_venta_offline).</summary>
    public sealed class VentaPendiente
    {
        [JsonProperty("uuid")] public string Uuid { get; set; } = Guid.NewGuid().ToString();
        [JsonProperty("kiosco_id")] public long KioscoId { get; set; }
        [JsonProperty("usuario_id")] public Guid? UsuarioId { get; set; }
        [JsonProperty("usuario_nombre")] public string? UsuarioNombre { get; set; }
        /// <summary>Día de la venta (medianoche, hora argentina), igual que guarda una venta normal.</summary>
        [JsonProperty("fecha")] public DateTimeOffset Fecha { get; set; }
        /// <summary>Hora real de la venta.</summary>
        [JsonProperty("hora")] public DateTimeOffset Hora { get; set; }
        [JsonProperty("total")] public decimal Total { get; set; }
        [JsonProperty("pagos")] public List<PagoPendiente> Pagos { get; set; } = new();
        [JsonProperty("items")] public List<ItemPendiente> Items { get; set; } = new();
        [JsonProperty("varios")] public List<VarioPendiente> Varios { get; set; } = new();
    }

    public sealed class PagoPendiente
    {
        [JsonProperty("forma_pago_id")] public long FormaPagoId { get; set; }
        [JsonProperty("nombre")] public string? Nombre { get; set; }
        [JsonProperty("monto")] public decimal Monto { get; set; }
    }

    public sealed class ItemPendiente
    {
        [JsonProperty("producto_id")] public long ProductoId { get; set; }
        [JsonProperty("nombre")] public string? Nombre { get; set; }
        [JsonProperty("cantidad")] public int Cantidad { get; set; }
        [JsonProperty("precio_unitario")] public decimal PrecioUnitario { get; set; }
        [JsonProperty("subtotal")] public decimal Subtotal { get; set; }
    }

    public sealed class VarioPendiente
    {
        [JsonProperty("nombre")] public string? Nombre { get; set; }
        [JsonProperty("cantidad")] public int Cantidad { get; set; }
        [JsonProperty("precio_unitario")] public decimal PrecioUnitario { get; set; }
        [JsonProperty("subtotal")] public decimal Subtotal { get; set; }
    }
}
