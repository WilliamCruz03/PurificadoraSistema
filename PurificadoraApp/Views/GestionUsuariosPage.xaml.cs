using Supabase;
using System.Text.Json;

namespace PurificadoraApp.Views
{
    public partial class GestionUsuariosPage : ContentPage
    {
        private readonly Supabase.Client _supabaseClient;
        private List<UserInfo> _usuarios;

        public class UserInfo
        {
            public string Id { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Rol { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }

        public GestionUsuariosPage()
        {
            InitializeComponent();
            _supabaseClient = MauiProgram.GetService<Supabase.Client>();
            CargarUsuarios();
        }

        private async void CargarUsuarios()
        {
            try
            {
                // Obtener usuarios usando Admin API
                var response = await _supabaseClient.Auth.Admin.GetUsers();

                _usuarios = new List<UserInfo>();
                foreach (var user in response.Users)
                {
                    var metadata = user.UserMetadata;
                    _usuarios.Add(new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email ?? string.Empty,
                        Nombre = metadata?.TryGetValue("nombre", out var nombre) == true ? nombre?.ToString() ?? "" : "",
                        Rol = metadata?.TryGetValue("rol", out var rol) == true ? rol?.ToString() ?? "Repartidor" : "Repartidor",
                        CreatedAt = user.CreatedAt ?? DateTime.Now
                    });
                }

                ListaUsuarios.ItemsSource = _usuarios;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudieron cargar los usuarios: {ex.Message}", "OK");
            }
        }

        private async void OnNuevoUsuarioClicked(object sender, EventArgs e)
        {
            var email = await DisplayPromptAsync("Nuevo Usuario", "Email:", keyboard: Keyboard.Email);
            if (string.IsNullOrEmpty(email)) return;

            var password = await DisplayPromptAsync("Nuevo Usuario", "Contraseña:", keyboard: Keyboard.Text);
            if (string.IsNullOrEmpty(password)) return;

            var nombre = await DisplayPromptAsync("Nuevo Usuario", "Nombre completo:");
            if (string.IsNullOrEmpty(nombre)) return;

            var rol = await DisplayActionSheet("Rol del usuario", "Cancelar", null, "Repartidor", "Admin");

            try
            {
                var response = await _supabaseClient.Auth.SignUp(email, password, new Supabase.Gotrue.Admin.UserAttributes
                {
                    Data = new Dictionary<string, object>
                    {
                        { "nombre", nombre },
                        { "rol", rol }
                    }
                });

                if (response.User != null)
                {
                    await DisplayAlert("Éxito", $"Usuario {email} creado correctamente", "OK");
                    CargarUsuarios();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo crear: {ex.Message}", "OK");
            }
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var usuario = button?.CommandParameter as UserInfo;
            if (usuario == null) return;

            var nuevoNombre = await DisplayPromptAsync("Editar Usuario", "Nombre:", initialValue: usuario.Nombre);
            var nuevoRol = await DisplayActionSheet("Rol del usuario", "Cancelar", null, "Repartidor", "Admin");

            if (!string.IsNullOrEmpty(nuevoNombre) || !string.IsNullOrEmpty(nuevoRol))
            {
                try
                {
                    var metadata = new Dictionary<string, object>();
                    if (!string.IsNullOrEmpty(nuevoNombre))
                        metadata["nombre"] = nuevoNombre;
                    if (!string.IsNullOrEmpty(nuevoRol))
                        metadata["rol"] = nuevoRol;

                    await _supabaseClient.Auth.Admin.UpdateUserById(usuario.Id, new Supabase.Gotrue.Admin.UserAttributes
                    {
                        Data = metadata
                    });

                    await DisplayAlert("Éxito", "Usuario actualizado", "OK");
                    CargarUsuarios();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var usuario = button?.CommandParameter as UserInfo;
            if (usuario == null) return;

            var confirmar = await DisplayAlert("Confirmar", $"¿Eliminar usuario {usuario.Email}?", "Sí", "No");
            if (confirmar)
            {
                try
                {
                    await _supabaseClient.Auth.Admin.DeleteUserById(usuario.Id);
                    await DisplayAlert("Éxito", "Usuario eliminado", "OK");
                    CargarUsuarios();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }
    }
}