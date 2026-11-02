using BlazorKioscoCompartido.Services;
using BlazorKioscoCompartido.Services.Reportes;
using BlazorKioscoCompartido.Services.SinInternet;
using BlazorKioscoDesktop.Components.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
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

#if WINDOWS
        // La X de la ventana (y Alt+F4) no cierra el sistema mientras hay alguien adentro:
        // se cierra con el botón "Cerrar el sistema" del menú (así se marca la salida del empleado).
        builder.ConfigureLifecycleEvents(eventos => eventos.AddWindows(w => w.OnWindowCreated(ventana =>
        {
            ventana.AppWindow.Closing += (s, e) =>
            {
                if (CierreSistema.Permitido || !CierreSistema.HaySesion()) return;
                e.Cancel = true;
                CierreSistema.AvisarIntento();
            };
        })));
#endif

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
        builder.Services.AddScoped<ProveedorService>();
        builder.Services.AddScoped<ConfiguracionService>();
        builder.Services.AddScoped<AsistenciaService>();     // turnos y entrada/salida de empleados   // Configuración (ABM) y color del negocio
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

        // =========================================================
        // ETAPA 7: vender sin internet. Guarda en la PC una copia para vender, las ventas hechas sin internet
        // y las sube solas cuando vuelve (necesita el script _sql\25.ventas_sin_internet.sql).
        // Carpeta: %LOCALAPPDATA%\...\sin_internet (FileSystem.AppDataDirectory)
        // =========================================================
        builder.Services.AddSingleton(sp => new VentaSinInternet(
            sp.GetRequiredService<Supabase.Client>(),
            sp.GetRequiredService<AppState>(),
            new OpcionesSinInternet
            {
                Carpeta = Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "sin_internet"),
                SupabaseUrl = supabaseUrl ?? "",
                AnonKey = supabaseKey ?? "",
                HayRed = () => Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess != Microsoft.Maui.Networking.NetworkAccess.None
            }));

        // Copia automática del Excel del negocio en una carpeta (ej. Google Drive). Se configura en Configuración → Copia de seguridad.
        builder.Services.AddSingleton(sp => new CopiaAutomatica(
            sp.GetRequiredService<Supabase.Client>(),
            sp.GetRequiredService<AppState>(),
            Path.Combine(Microsoft.Maui.Storage.FileSystem.AppDataDirectory, "copia_automatica")));

        var app = builder.Build();
        app.Services.GetRequiredService<VentaSinInternet>();
        app.Services.GetRequiredService<CopiaAutomatica>();   // se crea ya: revisa sola cada 30 minutos   // se crea ya, así revisa internet y sube lo pendiente desde el arranque
        return app;
    }
}