using PurificadoraApp.Services;
using Supabase;

namespace PurificadoraApp;

public static class MauiProgram
{
    public static MauiApp CurrentApp { get; private set; }

    public static T GetService<T>() where T : class
    {
        return CurrentApp?.Services.GetService<T>();
    }

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Registrar servicios
        builder.Services.AddSingleton<CustomSessionHandler>();
        builder.Services.AddSingleton<LocalDbService>();

        // Registrar el cliente de Supabase
        builder.Services.AddSingleton(provider =>
        {
            var sessionHandler = provider.GetRequiredService<CustomSessionHandler>();

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            var client = new Supabase.Client(
                SupabaseConfig.Url,
                SupabaseConfig.AnonKey,
                options
            );

            // ❌ ELIMINA esta línea: client.Initialize();

            // Cargar sesión guardada
            client.Auth.LoadSession();

            return client;
        });

        builder.Services.AddSingleton<SyncService>();

        CurrentApp = builder.Build();
        return CurrentApp;
    }
}