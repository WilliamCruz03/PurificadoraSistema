using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supabase;
using PurificadoraApp.Models;
using PurificadoraApp.Services;
using System.Text.Json;
using System.Diagnostics;

namespace PurificadoraApp.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly LocalDbService _localDbService;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _mensajeError = string.Empty;
        private bool _isLoading;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string MensajeError
        {
            get => _mensajeError;
            set => SetProperty(ref _mensajeError, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public LoginViewModel()
        {
            _supabaseClient = null!;
            _localDbService = null!;
        }

        public LoginViewModel(Supabase.Client supabaseClient, LocalDbService localDbService)
        {
            _supabaseClient = supabaseClient;
            _localDbService = localDbService;

            // Diagnóstico
            Debug.WriteLine("LoginViewModel: Constructor llamado");
            Debug.WriteLine($"Supabase Client es null? {_supabaseClient == null}");
            Debug.WriteLine($"LocalDbService es null? {_localDbService == null}");
        }

        [RelayCommand]
        private async Task IniciarSesion()
        {
            try
            {
                Debug.WriteLine("IniciarSesion: Iniciando...");

                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    MensajeError = "Por favor ingrese email y contraseña";
                    Debug.WriteLine("IniciarSesion: Email o Password vacíos");
                    return;
                }

                IsLoading = true;
                MensajeError = string.Empty;

                // Verificar que el cliente no sea null
                if (_supabaseClient == null)
                {
                    MensajeError = "Error de conexión: Cliente no inicializado";
                    Debug.WriteLine("IniciarSesion: _supabaseClient es NULL");
                    return;
                }

                if (_supabaseClient.Auth == null)
                {
                    MensajeError = "Error de conexión: Auth no disponible";
                    Debug.WriteLine("IniciarSesion: _supabaseClient.Auth es NULL");
                    return;
                }

                Debug.WriteLine($"IniciarSesion: Intentando login con {Email}");

                // Intentar autenticar con Supabase
                var session = await _supabaseClient.Auth.SignIn(Email, Password);

                Debug.WriteLine($"IniciarSesion: Session obtenida - Es null? {session == null}");

                if (session != null && session.User != null)
                {
                    Debug.WriteLine($"IniciarSesion: Usuario ID: {session.User.Id}");

                    var rol = session.User.UserMetadata?.TryGetValue("rol", out var rolObj) == true
                        ? rolObj?.ToString() ?? "Repartidor"
                        : "Repartidor";

                    var nombre = session.User.UserMetadata?.TryGetValue("nombre", out var nombreObj) == true
                        ? nombreObj?.ToString() ?? session.User.Email!
                        : session.User.Email!;

                    var usuario = new UsuarioSesion
                    {
                        Id = session.User.Id!,
                        Email = session.User.Email!,
                        Nombre = nombre,
                        Rol = rol,
                        FechaInicio = DateTime.Now
                    };

                    Preferences.Set("usuario_actual", JsonSerializer.Serialize(usuario));
                    Preferences.Set("access_token", session.AccessToken);

                    Debug.WriteLine($"IniciarSesion: Usuario guardado - Rol: {rol}");

                    // Cambiar la navegación aquí
                    Application.Current.MainPage = new MainPage();
                }
                else
                {
                    MensajeError = "Error: No se pudo obtener la sesión del usuario";
                    Debug.WriteLine("IniciarSesion: Session o User es null");
                }
            }
            catch (Exception ex)
            {
                MensajeError = $"Error al iniciar sesión: {ex.Message}";
                Debug.WriteLine($"IniciarSesion EXCEPCIÓN: {ex.Message}");
                Debug.WriteLine($"StackTrace: {ex.StackTrace}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}