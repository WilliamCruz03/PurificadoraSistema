using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supabase;
using PurificadoraApp.Models;
using PurificadoraApp.Services;

namespace PurificadoraApp.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly LocalDbService _localDbService;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string mensajeError = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public LoginViewModel(Supabase.Client supabaseClient, LocalDbService localDbService)
        {
            _supabaseClient = supabaseClient;
            _localDbService = localDbService;
        }

        [RelayCommand]
        private async Task IniciarSesion()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                MensajeError = "Por favor ingrese email y contraseña";
                return;
            }

            IsLoading = true;
            MensajeError = string.Empty;

            try
            {
                // Intentar autenticar con Supabase
                var session = await _supabaseClient.Auth.SignIn(Email, Password);

                if (session != null && session.User != null)
                {
                    // Obtener el rol del usuario desde los metadatos
                    var rol = session.User.UserMetadata?.TryGetValue("rol", out var rolObj) == true
                        ? rolObj?.ToString() ?? "Repartidor"
                        : "Repartidor";

                    // Guardar información de la sesión localmente
                    var usuario = new UsuarioSesion
                    {
                        Id = session.User.Id!,
                        Email = session.User.Email!,
                        Nombre = session.User.UserMetadata?.TryGetValue("nombre", out var nombre) == true
                            ? nombre?.ToString() ?? session.User.Email!
                            : session.User.Email!,
                        Rol = rol,
                        FechaInicio = DateTime.Now
                    };

                    // Guardar en preferencias locales
                    Preferences.Set("usuario_actual", JsonSerializer.Serialize(usuario));
                    Preferences.Set("access_token", session.AccessToken);

                    // Redirigir según el rol
                    if (rol == "Admin")
                    {
                        await Shell.Current.GoToAsync("//admin/dashboard");
                    }
                    else
                    {
                        await Shell.Current.GoToAsync("//repartidor/inicio");
                    }
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al iniciar sesión: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task IrARegistro()
        {
            // Implementar registro de usuarios (solo para administradores)
            await Shell.Current.DisplayAlert("Info", "Contacte al administrador para crear una cuenta", "OK");
        }
    }
}