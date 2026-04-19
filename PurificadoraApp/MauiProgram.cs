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

        // Registrar el manejador de sesión como singleton
        builder.Services.AddSingleton<CustomSessionHandler>();

        // Registrar el cliente de Supabase
        builder.Services.AddSingleton(provider =>
        {
            var sessionHandler = provider.GetRequiredService<CustomSessionHandler>();

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,  // Refresca el token automáticamente [citation:8]
                AutoConnectRealtime = true,
                SessionHandler = sessionHandler
            };

            var client = new Supabase.Client(
                SupabaseConfig.Url,
                SupabaseConfig.AnonKey,
                options
            );

            // Cargar la sesión guardada (si existe) [citation:4]
            client.Auth.LoadSession();

            return client;
        });

        return builder.Build();
    }
}