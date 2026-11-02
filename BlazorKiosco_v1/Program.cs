using BlazorKioscoMobile;
using BlazorKioscoCompartido.Services;
using BlazorKioscoCompartido.Services.Reportes;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Supabase;


var culturaArg = new System.Globalization.CultureInfo("es-AR");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culturaArg;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culturaArg;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Estado global
builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<CarritoService>();
builder.Services.AddScoped<ReporteRecaudacionDiariaService>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<ConfiguracionService>();
builder.Services.AddScoped<AsistenciaService>();      // turnos y entrada/salida de empleados
//builder.Services.AddSingleton<PrecioService>();

// HttpClient
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Registrar un servicio de lógica de precios
builder.Services.AddScoped<PrecioService>();
builder.Services.AddScoped<ConsultasDB>();

builder.Services.AddLocalStorageServices();

// Configuración Supabase
var supabaseUrl = builder.Configuration["SupabaseUrl"];
var supabaseKey = builder.Configuration["SupabaseKey"];

// ?? Cliente Supabase CORREGIDO
builder.Services.AddScoped<Supabase.Client>(sp =>
{
    var options = new SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = false // ?? CLAVE: desactivar realtime
    };

    return new Supabase.Client(supabaseUrl, supabaseKey, options);
});

var host = builder.Build();

// ?? Inicializar Supabase correctamente (async real)
var supabase = host.Services.GetRequiredService<Supabase.Client>();
await supabase.InitializeAsync();

await host.RunAsync();