using PurificadoraApp.Services;
using Supabase;

namespace PurificadoraApp;

public static class MauiProgram
{
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

        // ✅ Registrar servicios (TODOS fuera del lambda)
        builder.Services.AddSingleton<CustomSessionHandler>();
        builder.Services.AddSingleton<LocalDbService>();  // ✅ MOVER AQUÍ

        // Registrar el cliente de Supabase
        builder.Services.AddSingleton(provider =>
        {
            var sessionHandler = provider.GetRequiredService<CustomSessionHandler>();
            // ❌ ELIMINAR esta línea de aquí: builder.Services.AddSingleton<LocalDbService>();

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true,
                SessionHandler = sessionHandler
            };

            var client = new Supabase.Client(
                SupabaseConfig.Url,
                SupabaseConfig.AnonKey,
                options
            );

            client.Auth.LoadSession();
            return client;
        });

        builder.Services.AddSingleton<SyncService>();

        return builder.Build();
    }
}