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
            public string Username { get; set; } = string.Empty;
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
                IndicatorCarga.IsVisible = true;
                IndicatorCarga.IsRunning = true;

                var response = await _supabaseAdminClient.Rpc("get_all_users", new { });

                if (response.Content != null)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _usuarios = JsonSerializer.Deserialize<List<UserInfo>>(response.Content, options) ?? new List<UserInfo>();
                    ListaUsuarios.ItemsSource = _usuarios;
                }

                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;
            }
            catch (Exception ex)
            {
                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;
                await DisplayAlert("Error", $"Error al cargar usuarios: {ex.Message}", "OK");
                ListaUsuarios.ItemsSource = new List<UserInfo>();
            }
        }

        private async void OnNuevoUsuarioClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new FormularioUsuarioPage());
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var usuario = button?.CommandParameter as UserInfo;
            if (usuario == null) return;

            await Navigation.PushModalAsync(new FormularioEditarUsuarioPage(usuario));
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
                    IndicatorCarga.IsVisible = true;
                    IndicatorCarga.IsRunning = true;

                    await _supabaseAdminClient.Rpc("delete_user", new { p_user_id = usuario.Id });

                    IndicatorCarga.IsVisible = false;
                    IndicatorCarga.IsRunning = false;

                    await DisplayAlert("Éxito", "Usuario eliminado correctamente", "OK");
                    CargarUsuarios();
                }
                catch (Exception ex)
                {
                    IndicatorCarga.IsVisible = false;
                    IndicatorCarga.IsRunning = false;
                    await DisplayAlert("Error", $"No se pudo eliminar: {ex.Message}", "OK");
                }
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            CargarUsuarios(); // Recargar cada vez que se muestra la página
        }
    }
}