using BlazorKioscoCompartido.Services;
using BlazorKioscoCompartido.Services.Reportes;
using BlazorKioscoDesktop.Components.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase;
using System.Reflection;

namespace BlazorKioscoDesktop;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {

        // 1. Forzamos la cultura de Argentina de forma global para hilos y UI
        //var culturaArg = new System.Globalization.CultureInfo("es-AR");
        //System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culturaArg;
        //System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culturaArg;


        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Configuración de Blazor WebView interna de MAUI
        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // ==========================================
        // 1. ESTADO GLOBAL Y SERVICIOS COMPARTIDOS
        // ==========================================
        // En MAUI, para servicios globales que persisten en toda la app, 
        // usamos AddSingleton igual que en el celular.
        builder.Services.AddSingleton<AppState>();
        builder.Services.AddSingleton<CarritoService>();
        builder.Services.AddSingleton<PrecioService>();
        builder.Services.AddScoped<ReporteRecaudacionDiariaService>();
        builder.Services.AddScoped<ConsultasDB>();
        builder.Services.AddSingleton<UserStateService>();
        // ==========================================
        // 2. HTTP CLIENT (Ajuste para Escritorio)
        // ==========================================
        // Como no estamos en la web, no hay una URL base del navegador. 
        // Le ponemos una por defecto por si alguna librería la pide.
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost") });

        // ==========================================
        // =========================================================
        // 3. Cargar la configuración desde appsettings.json
        // =========================================================
        var assembly = Assembly.GetExecutingAssembly();
        // Asegúrate de que el namespace coincida con el de tu proyecto
        using var stream = assembly.GetManifestResourceStream("BlazorKioscoDesktop.wwwroot.appsettings.json");

        if (stream != null)
        {
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();

            builder.Configuration.AddConfiguration(config);
        }

        // =========================================================
        // 2. Obtener los valores de Supabase desde la configuración
        // =========================================================
        var supabaseUrl = builder.Configuration["Supabase:Url"];
        var supabaseKey = builder.Configuration["Supabase:AnonKey"];

        
        builder.Services.AddSingleton<Supabase.Client>(sp =>
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            var client = new Supabase.Client(supabaseUrl, supabaseKey, options);

            // TRUCO MAUI: Disparamos la inicialización acá adentro en segundo plano
            // para que no trabe el arranque de la ventana de Windows
            Task.Run(async () => await client.InitializeAsync());

            return client;
        });

        return builder.Build();
    }
}