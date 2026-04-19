using PurificadoraApp.Services;

namespace PurificadoraApp
{
    public partial class MainPage : ContentPage
    {
        private readonly SyncService _syncService;

        public MainPage()
        {
            InitializeComponent();
            _syncService = MauiProgram.GetService<SyncService>();
            CargarDatosUsuario();
        }

        private async void CargarDatosUsuario()
        {
            var usuarioJson = Preferences.Get("usuario_actual", string.Empty);
            if (!string.IsNullOrEmpty(usuarioJson))
            {
                var usuario = System.Text.Json.JsonSerializer.Deserialize<Models.UsuarioSesion>(usuarioJson);
                // Usar DisplayAlert para mostrar el nombre temporalmente
                await DisplayAlert("Bienvenido", $"Hola {usuario?.Nombre}", "OK");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            Preferences.Remove("usuario_actual");
            Preferences.Remove("access_token");
            Application.Current.MainPage = new NavigationPage(new Views.LoginPage());
        }

        private async void OnSyncClicked(object sender, EventArgs e)
        {
            if (_syncService == null)
            {
                await DisplayAlert("Error", "Servicio de sincronización no disponible", "OK");
                return;
            }

            var (subidos, bajados) = await _syncService.SyncAll();
            await DisplayAlert("Sincronización",
                $"Entregas subidas: {subidos}\nEntregas descargadas: {bajados}",
                "OK");
        }
    }
}