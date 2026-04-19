using PurificadoraApp.Models;
using PurificadoraApp.Services;

namespace PurificadoraApp.Views
{
    public partial class RepartidorPage : ContentPage
    {
        private readonly LocalDbService _localDbService;
        private readonly SyncService _syncService;
        private string _repartidorId = string.Empty;
        private string _repartidorNombre = string.Empty;

        public RepartidorPage()
        {
            InitializeComponent();
            _localDbService = MauiProgram.GetService<LocalDbService>();
            _syncService = MauiProgram.GetService<SyncService>();

            CargarDatosRepartidor();
            CargarEntregasPendientes();
            VerificarConexion();
        }

        private void CargarDatosRepartidor()
        {
            var usuarioJson = Preferences.Get("usuario_actual", string.Empty);
            if (!string.IsNullOrEmpty(usuarioJson))
            {
                var usuario = System.Text.Json.JsonSerializer.Deserialize<UsuarioSesion>(usuarioJson);
                _repartidorId = usuario?.Id ?? string.Empty;
                _repartidorNombre = usuario?.Nombre ?? string.Empty;
            }
        }

        private async void CargarEntregasPendientes()
        {
            var pendientes = await _localDbService.GetEntregasPendientes();
            ListaPendientes.ItemsSource = pendientes;
        }

        private async void VerificarConexion()
        {
            var tieneInternet = await _syncService.HasInternetConnection();
            if (tieneInternet)
            {
                ImgConexion.Source = "cloud.png";
                LblEstadoConexion.Text = "Conectado";
                LblEstadoConexion.TextColor = Colors.Green;
            }
            else
            {
                ImgConexion.Source = "cloud_off.png";
                LblEstadoConexion.Text = "Sin conexión (modo offline)";
                LblEstadoConexion.TextColor = Colors.Red;
            }
        }

        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            // Validar campos
            if (string.IsNullOrWhiteSpace(TxtCliente.Text))
            {
                await DisplayAlert("Error", "Ingrese el nombre del cliente", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtDireccion.Text))
            {
                await DisplayAlert("Error", "Ingrese la dirección", "OK");
                return;
            }

            if (!int.TryParse(TxtCantidad.Text, out int cantidad) || cantidad <= 0)
            {
                await DisplayAlert("Error", "Ingrese una cantidad válida de garrafones", "OK");
                return;
            }

            // Crear entrega local
            var entrega = new EntregaLocal
            {
                RepartidorId = _repartidorId,
                RepartidorNombre = _repartidorNombre,
                ClienteNombre = TxtCliente.Text,
                Direccion = TxtDireccion.Text,
                CantidadGarrafones = cantidad,
                FechaHoraRegistro = DateTime.Now,
                EstadoSync = 0, // Pendiente de sincronizar
                Version = 1
            };

            // Guardar localmente
            await _localDbService.GuardarEntrega(entrega);

            // Limpiar campos
            TxtCliente.Text = string.Empty;
            TxtDireccion.Text = string.Empty;
            TxtCantidad.Text = string.Empty;

            // Recargar lista
            CargarEntregasPendientes();

            await DisplayAlert("Éxito", "Entrega registrada correctamente (offline)", "OK");
        }

        private async void OnSincronizarClicked(object sender, EventArgs e)
        {
            var tieneInternet = await _syncService.HasInternetConnection();
            if (!tieneInternet)
            {
                await DisplayAlert("Sin conexión",
                    "No hay internet. Las entregas se sincronizarán cuando haya conexión.", "OK");
                return;
            }

            BtnSincronizar.IsEnabled = false;
            BtnSincronizar.Text = "Sincronizando...";

            var (subidos, bajados) = await _syncService.SyncAll();

            BtnSincronizar.IsEnabled = true;
            BtnSincronizar.Text = "Sincronizar Entregas";

            // Recargar lista
            CargarEntregasPendientes();
            VerificarConexion();

            await DisplayAlert("Sincronización Completa",
                $"Entregas subidas: {subidos}\nDatos descargados: {bajados}", "OK");
        }
    }
}