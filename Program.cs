using BlazorKioscos_v1;
using BlazorKioscos_v1.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Estado global
builder.Services.AddSingleton<AppState>();
builder.Services.AddSingleton<CarritoService>();

// HttpClient
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

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