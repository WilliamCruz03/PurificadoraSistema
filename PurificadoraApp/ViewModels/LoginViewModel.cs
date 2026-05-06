using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Supabase;
using PurificadoraApp.Models;
using PurificadoraApp.Services;
using System.Text.Json;
using System.Diagnostics;
using Supabase.Gotrue;
using Microsoft.Maui.Networking;

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
        private bool _isOfflineMode;

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

        public bool IsOfflineMode
        {
            get => _isOfflineMode;
            set => SetProperty(ref _isOfflineMode, value);
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
            Debug.WriteLine("LoginViewModel: Constructor iniciado");
            Debug.WriteLine($"Supabase URL: {SupabaseConfig.Url}");
            Debug.WriteLine($"AnonKey presente: {!string.IsNullOrEmpty(SupabaseConfig.AnonKey)}");
            VerificarSesionGuardada();
        }

        private async void VerificarSesionGuardada()
        {
            Debug.WriteLine("VerificarSesionGuardada: Iniciando");
            var token = Preferences.Get("access_token", string.Empty);
            var usuarioJson = Preferences.Get("usuario_actual", string.Empty);

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(usuarioJson))
            {
                var tieneInternet = await TieneInternet();
                Debug.WriteLine($"Tiene internet: {tieneInternet}");

                if (!tieneInternet)
                {
                    var usuario = JsonSerializer.Deserialize<UsuarioSesion>(usuarioJson);
                    if (usuario != null)
                    {
                        IsOfflineMode = true;
                        MensajeError = "⚠️ Modo offline - Usando sesión guardada";
                        await Task.Delay(500);
                        await NavegarSegunRol(usuario.Rol);
                    }
                }
            }
        }

        private async Task<bool> TieneInternet()
        {
            try
            {
                var current = Connectivity.Current;
                return current.NetworkAccess == NetworkAccess.Internet;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error TieneInternet: {ex.Message}");
                return false;
            }
        }

        [RelayCommand]
        private async Task IniciarSesion()
        {
            try
            {
                Debug.WriteLine("=== INICIANDO LOGIN ===");

                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    MensajeError = "Por favor ingrese email y contraseña";
                    return;
                }

                IsLoading = true;
                MensajeError = string.Empty;

                if (_supabaseClient?.Auth == null)
                {
                    MensajeError = "Error de conexión";
                    return;
                }

                // Login directo con email y contraseña
                var session = await _supabaseClient.Auth.SignIn(Email, Password);

                if (session?.User != null)
                {
                    await ProcesarLoginExitoso(session);
                }
                else
                {
                    MensajeError = "Credenciales incorrectas";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error login: {ex.Message}");
                MensajeError = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        } 

        private async Task<bool> LoginOffline()
        {
            var emailGuardado = Preferences.Get("ultimo_email", string.Empty);
            var passwordHash = Preferences.Get("ultimo_password_hash", string.Empty);

            if (string.IsNullOrEmpty(emailGuardado) || string.IsNullOrEmpty(passwordHash))
                return false;

            var hashIngresado = HashPassword(Password);

            if (emailGuardado == Email && hashIngresado == passwordHash)
            {
                var usuarioJson = Preferences.Get("usuario_actual", string.Empty);
                if (!string.IsNullOrEmpty(usuarioJson))
                {
                    var usuario = JsonSerializer.Deserialize<UsuarioSesion>(usuarioJson);
                    if (usuario != null)
                    {
                        IsOfflineMode = true;
                        await ToastService.Info("⚠️ Modo offline - Acceso con sesión guardada");
                        await NavegarSegunRol(usuario.Rol);
                        return true;
                    }
                }
            }
            return false;
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async Task ProcesarLoginExitoso(Supabase.Gotrue.Session session)
        {
            Debug.WriteLine("ProcesarLoginExitoso: Iniciando");

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
            Preferences.Set("ultimo_email", Email);
            Preferences.Set("ultimo_password_hash", HashPassword(Password));
            Preferences.Set("ultimo_login_exitoso", DateTime.Now.ToString());

            Debug.WriteLine($"Login exitoso: {usuario.Nombre} - Rol: {rol}");

            await ToastService.Success($"Bienvenido {nombre}");
            await NavegarSegunRol(rol);
        }

        private async Task NavegarSegunRol(string rol)
        {
            Debug.WriteLine($"NavegarSegunRol: {rol}");
            if (rol == "Admin")
                Application.Current.MainPage = new NavigationPage(new Views.AdminDashboardPage());
            else
                Application.Current.MainPage = new NavigationPage(new Views.RepartidorPage());
        }
    }
}