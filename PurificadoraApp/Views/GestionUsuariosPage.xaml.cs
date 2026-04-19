using System.Text.Json;
using PurificadoraApp.Services;
using Supabase;

namespace PurificadoraApp.Views
{
    public partial class GestionUsuariosPage : ContentPage
    {
        private readonly Supabase.Client _supabaseAdminClient;
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
            _supabaseAdminClient = MauiProgram.GetService<Supabase.Client>();
            CargarUsuarios();
        }

        private async void CargarUsuarios()
        {
            try
            {
                var response = await _supabaseAdminClient.Rpc("get_all_users", new { });

                if (response.Content != null)
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _usuarios = JsonSerializer.Deserialize<List<UserInfo>>(response.Content, options) ?? new List<UserInfo>();
                    ListaUsuarios.ItemsSource = _usuarios;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error detallado: {ex.Message}");
                await DisplayAlert("Error", $"Error al cargar usuarios: {ex.Message}", "OK");
                ListaUsuarios.ItemsSource = new List<UserInfo>();
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
                var response = await _supabaseAdminClient.Rpc("create_user", new
                {
                    p_email = email,
                    p_password = password,
                    p_nombre = nombre,
                    p_rol = rol
                });

                await DisplayAlert("Éxito", $"Usuario {email} creado correctamente", "OK");
                CargarUsuarios(); // Recargar lista
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo crear el usuario: {ex.Message}", "OK");
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
                    await _supabaseAdminClient.Rpc("update_user", new
                    {
                        p_user_id = usuario.Id,
                        p_nombre = nuevoNombre ?? "",
                        p_rol = nuevoRol ?? ""
                    });

                    await DisplayAlert("Éxito", "Usuario actualizado correctamente", "OK");
                    CargarUsuarios();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"No se pudo actualizar: {ex.Message}", "OK");
                }
            }
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var usuario = button?.CommandParameter as UserInfo;
            if (usuario == null) return;

            var confirmar = await DisplayAlert("Confirmar", $"¿Eliminar usuario {usuario.Email}?\nEsta acción no se puede deshacer.", "Sí, eliminar", "Cancelar");
            if (confirmar)
            {
                try
                {
                    await _supabaseAdminClient.Rpc("delete_user", new { p_user_id = usuario.Id });
                    await DisplayAlert("Éxito", "Usuario eliminado correctamente", "OK");
                    CargarUsuarios();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
                }
            }
        }
    }
}