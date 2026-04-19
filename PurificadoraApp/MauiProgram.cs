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

        // Registrar el cliente de Supabase (usando anon key para usuarios normales)
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

            client.Auth.LoadSession();
            return client;
        });

        // Cliente Admin (con service_role key) - solo para operaciones de administración
        builder.Services.AddSingleton(provider =>
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            };

            var client = new Supabase.Client(
                SupabaseConfig.Url,
                SupabaseConfig.ServiceRoleKey,
                options
            );

            return client;
        });

        builder.Services.AddSingleton<SyncService>();

        CurrentApp = builder.Build();
        return CurrentApp;
    }
}