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

            // Verificar si hay sesión guardada al iniciar
            VerificarSesionGuardada();
        }

        private async void VerificarSesionGuardada()
        {
            var token = Preferences.Get("access_token", string.Empty);
            var usuarioJson = Preferences.Get("usuario_actual", string.Empty);
            var ultimoLogin = Preferences.Get("ultimo_login_exitoso", string.Empty);

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(usuarioJson))
            {
                // Verificar si estamos offline
                var tieneInternet = await TieneInternet();

                if (!tieneInternet)
                {
                    // Modo offline - usar sesión guardada
                    var usuario = JsonSerializer.Deserialize<UsuarioSesion>(usuarioJson);
                    if (usuario != null)
                    {
                        IsOfflineMode = true;
                        MensajeError = "⚠️ Modo offline - Usando sesión guardada";

                        // Navegar directamente
                        await Task.Delay(500); // Pequeña pausa para mostrar el mensaje
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
            catch
            {
                return false;
            }
        }

        [RelayCommand]
        private async Task IniciarSesion()
        {
            try
            {
                Debug.WriteLine("IniciarSesion: Iniciando...");

                if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                {
                    MensajeError = "Por favor ingrese usuario/email y contraseña";
                    return;
                }

                IsLoading = true;
                MensajeError = string.Empty;
                IsOfflineMode = false;

                // Verificar conexión a internet
                var tieneInternet = await TieneInternet();

                if (!tieneInternet)
                {
                    // Intentar login offline con credenciales guardadas
                    var exito = await LoginOffline();
                    if (exito)
                    {
                        IsLoading = false;
                        return;
                    }
                    else
                    {
                        MensajeError = "Sin conexión a internet. No hay sesión guardada disponible.";
                        IsLoading = false;
                        return;
                    }
                }

                if (_supabaseClient?.Auth == null)
                {
                    MensajeError = "Error de conexión";
                    return;
                }

                Debug.WriteLine($"Intentando login con: {Email}");

                // Intentar login con email
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
                // Si falla con email, intentar buscar por username
                try
                {
                    var tieneInternet = await TieneInternet();
                    if (!tieneInternet)
                    {
                        var exito = await LoginOffline();
                        if (exito) return;
                    }

                    var response = await _supabaseClient.Rpc("find_user_by_login", new { p_login = Email });
                    if (response.Content != null)
                    {
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var usuarios = JsonSerializer.Deserialize<List<dynamic>>(response.Content, options);

                        if (usuarios != null && usuarios.Any())
                        {
                            var emailEncontrado = usuarios.First().GetProperty("email").GetString();
                            var session = await _supabaseClient.Auth.SignIn(emailEncontrado, Password);

                            if (session?.User != null)
                            {
                                await ProcesarLoginExitoso(session);
                                return;
                            }
                        }
                    }
                    MensajeError = $"Credenciales incorrectas";
                }
                catch (Exception ex2)
                {
                    MensajeError = $"Error: {ex2.Message}";
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<bool> LoginOffline()
        {
            // Verificar credenciales guardadas localmente
            var emailGuardado = Preferences.Get("ultimo_email", string.Empty);
            var passwordHash = Preferences.Get("ultimo_password_hash", string.Empty);

            if (string.IsNullOrEmpty(emailGuardado) || string.IsNullOrEmpty(passwordHash))
                return false;

            // Verificar si la contraseña ingresada coincide con el hash guardado
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
            // Hash simple para verificación offline (no es seguro para producción)
            // En producción usaría bcrypt, pero para este caso es suficiente
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async Task ProcesarLoginExitoso(Supabase.Gotrue.Session session)
        {
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

            // Guardar datos para login offline
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
            if (rol == "Admin")
                Application.Current.MainPage = new NavigationPage(new Views.AdminDashboardPage());
            else
                Application.Current.MainPage = new NavigationPage(new Views.RepartidorPage());
        }
    }
}