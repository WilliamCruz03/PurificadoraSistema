using PurificadoraApp.Models;
using PurificadoraApp.Services;
using System.Text.Json;
using Supabase;

namespace PurificadoraApp.Views
{
    public partial class RepartidorPage : ContentPage
    {
        private readonly LocalDbService _localDbService;
        private readonly SyncService _syncService;
        private readonly Supabase.Client _supabaseClient;
        private string _repartidorId = string.Empty;
        private string _repartidorNombre = string.Empty;
        private Cliente _clienteSeleccionado;
        private List<Cliente> _clientesEncontrados;
        private readonly ConnectivityService _connectivityService;

        public RepartidorPage()
        {
            InitializeComponent();
            _localDbService = MauiProgram.GetService<LocalDbService>();
            _syncService = MauiProgram.GetService<SyncService>();
            _supabaseClient = MauiProgram.GetService<Supabase.Client>();
            _connectivityService = MauiProgram.GetService<ConnectivityService>();

            // Suscribirse a cambios de conectividad
            _connectivityService.ConnectivityChanged += async () => await OnConnectivityChanged();

            CargarDatosRepartidor();
            CargarEntregasPendientes();
            _ = VerificarConexion();
        }

        private void CargarDatosRepartidor()
        {
            var usuarioJson = Preferences.Get("usuario_actual", string.Empty);
            if (!string.IsNullOrEmpty(usuarioJson))
            {
                var usuario = JsonSerializer.Deserialize<UsuarioSesion>(usuarioJson);
                _repartidorId = usuario?.Id ?? string.Empty;
                _repartidorNombre = usuario?.Nombre ?? string.Empty;
                LblRepartidorNombre.Text = $"Bienvenido, {_repartidorNombre}";
            }
        }

        private async void CargarEntregasPendientes()
        {
            var pendientes = await _localDbService.GetEntregasPendientes();
            ListaPendientes.ItemsSource = pendientes;
        }

        private async Task VerificarConexion()
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

        private async void OnBuscarClienteChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue) || e.NewTextValue.Length < 2)
            {
                ListaClientesResultados.IsVisible = false;
                return;
            }

            try
            {
                var response = await _supabaseClient.Rpc("search_clientes", new { search_term = e.NewTextValue });
                if (response.Content != null)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _clientesEncontrados = JsonSerializer.Deserialize<List<Cliente>>(response.Content, options) ?? new List<Cliente>();
                    ListaClientesResultados.ItemsSource = _clientesEncontrados;
                    ListaClientesResultados.IsVisible = _clientesEncontrados.Any();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Error al buscar clientes: {ex.Message}", "OK");
            }
        }

        private void OnClienteSeleccionado(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Cliente cliente)
            {
                _clienteSeleccionado = cliente;
                LblClienteSeleccionado.Text = cliente.NombreCompleto;
                LblDireccionSeleccionada.Text = cliente.Direccion;
                ListaClientesResultados.IsVisible = false;
                SearchBarClientes.Text = string.Empty;
                BtnRegistrar.IsEnabled = true;
                FrameClienteSeleccionado.IsVisible = true;
            }
        }

        private void OnCambiarCliente(object sender, EventArgs e)
        {
            _clienteSeleccionado = null;
            BtnRegistrar.IsEnabled = false;
            LblClienteSeleccionado.Text = string.Empty;
            LblDireccionSeleccionada.Text = string.Empty;
            FrameClienteSeleccionado.IsVisible = false;
            SearchBarClientes.Focus();
        }

        private async void OnRegistrarClicked(object sender, EventArgs e)
        {
            if (_clienteSeleccionado == null)
            {
                await DisplayAlert("Error", "Debe seleccionar un cliente", "OK");
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
                ClienteId = _clienteSeleccionado.Id,
                ClienteNombre = _clienteSeleccionado.NombreCompleto,
                Direccion = _clienteSeleccionado.Direccion,
                RepartidorId = _repartidorId,
                RepartidorNombre = _repartidorNombre,
                CantidadGarrafones = cantidad,
                FechaHoraRegistro = DateTime.Now,
                EstadoSync = 0,
                Version = 1
            };

            await _localDbService.GuardarEntrega(entrega);

            // Limpiar campos
            TxtCantidad.Text = string.Empty;
            _clienteSeleccionado = null;
            BtnRegistrar.IsEnabled = false;
            LblClienteSeleccionado.Text = string.Empty;
            LblDireccionSeleccionada.Text = string.Empty;
            FrameClienteSeleccionado.IsVisible = false;

            CargarEntregasPendientes();

            await DisplayAlert("Éxito", "Entrega registrada correctamente", "OK");
        }
        // Evento para el botón - debe ser async void
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

            CargarEntregasPendientes();
            await VerificarConexion();

            await DisplayAlert("Sincronización Completa",
                $"Entregas subidas: {subidos}\nDatos descargados: {bajados}", "OK");
        }

        // Método para conectividad
        private async Task OnConnectivityChanged()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await VerificarConexion();

                if (_connectivityService.IsConnected)
                {
                    var pendientes = await _localDbService.GetEntregasPendientes();
                    if (pendientes.Any())
                    {
                        OnSincronizarClicked(null, null);  // Sin await, es async void
                    }
                }
            });
        }

        // Botón manual de sincronización
        private void OnSyncManualClicked(object sender, EventArgs e)
        {
            OnSincronizarClicked(sender, e);  // Llamada directa, sin asignación
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirmar = await DisplayAlert("Cerrar Sesión", "¿Está seguro que desea cerrar sesión?", "Sí", "No");
            if (confirmar)
            {
                Preferences.Remove("usuario_actual");
                Preferences.Remove("access_token");
                Application.Current.MainPage = new NavigationPage(new Views.LoginPage());
            }
        }
    }
}