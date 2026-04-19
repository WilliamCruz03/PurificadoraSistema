using PurificadoraApp.Models;
using PurificadoraApp.Services;
using System.ComponentModel;
using System.Text.Json;
using System.Diagnostics;

namespace PurificadoraApp.Views
{
    public partial class AdminDashboardPage : ContentPage
    {
        private readonly LocalDbService _localDbService;
        private readonly Supabase.Client _supabaseClient;
        private readonly SyncService _syncService;
        private List<EntregaLocal> _todasEntregas;
        private bool _isNavigating = false;

        public AdminDashboardPage()
        {
            InitializeComponent();
            _localDbService = MauiProgram.GetService<LocalDbService>();
            _supabaseClient = MauiProgram.GetService<Supabase.Client>();
            _syncService = MauiProgram.GetService<SyncService>();
            DateFechaInicio.Date = DateTime.Now.AddDays(-30);
            DateFechaFin.Date = DateTime.Now;
            CargarDatos();
        }

        private async void CargarDatos()
        {
            Debug.WriteLine("CargarDatos: Iniciando carga desde DB local...");
            _todasEntregas = await _localDbService.GetAllEntregas();
            Debug.WriteLine($"CargarDatos: Se encontraron {_todasEntregas.Count} entregas");

            AplicarFiltros();
            CalcularEstadisticas();
            CargarRepartidores();
            // DibujarGrafico();  // COMENTADO TEMPORALMENTE

            ListaEntregas.ItemsSource = null;
            ListaEntregas.ItemsSource = _todasEntregas.OrderByDescending(e => e.FechaHoraRegistro).ToList();
            Debug.WriteLine("CargarDatos: Finalizado");
        }

        private void DibujarGrafico()
        {
            try
            {
                GraficoBarras.Children.Clear();

                var ultimos7Dias = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Now.AddDays(-i).Date)
                    .Reverse()
                    .ToList();

                var entregasPorDia = ultimos7Dias
                    .Select(d => _todasEntregas.Count(e => e.FechaHoraRegistro.Date == d))
                    .ToList();

                var maxEntregas = entregasPorDia.Max() > 0 ? entregasPorDia.Max() : 1;

                for (int i = 0; i < entregasPorDia.Count; i++)
                {
                    var altura = (entregasPorDia[i] * 120) / maxEntregas;
                    altura = altura < 20 && entregasPorDia[i] > 0 ? 20 : altura;

                    var barra = new VerticalStackLayout
                    {
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        VerticalOptions = LayoutOptions.End,
                        Spacing = 5
                    };

                    barra.Children.Add(new BoxView
                    {
                        HeightRequest = altura,
                        WidthRequest = 30,
                        BackgroundColor = Color.FromArgb("#3498db"),
                        CornerRadius = 5,
                        HorizontalOptions = LayoutOptions.Center
                    });

                    barra.Children.Add(new Label
                    {
                        Text = ultimos7Dias[i].ToString("dd/MM"),
                        FontSize = 10,
                        TextColor = App.Current.UserAppTheme == AppTheme.Dark ? Colors.White : Colors.Black,
                        HorizontalOptions = LayoutOptions.Center
                    });

                    barra.Children.Add(new Label
                    {
                        Text = entregasPorDia[i].ToString(),
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Colors.Green,
                        HorizontalOptions = LayoutOptions.Center
                    });

                    GraficoBarras.Children.Add(barra);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en gráfico: {ex.Message}");
            }
        }

        private void AplicarFiltros()
        {
            var filtradas = _todasEntregas.AsEnumerable();

            if (PickerFiltroRepartidor.SelectedIndex > 0)
            {
                var repartidorSeleccionado = PickerFiltroRepartidor.SelectedItem as string;
                filtradas = filtradas.Where(e => e.RepartidorNombre == repartidorSeleccionado);
            }

            filtradas = filtradas.Where(e => e.FechaHoraRegistro.Date >= DateFechaInicio.Date);
            filtradas = filtradas.Where(e => e.FechaHoraRegistro.Date <= DateFechaFin.Date);

            ListaEntregas.ItemsSource = filtradas.OrderByDescending(e => e.FechaHoraRegistro).ToList();
        }

        private void CalcularEstadisticas()
        {
            var entregas = ListaEntregas.ItemsSource as List<EntregaLocal> ?? new List<EntregaLocal>();
            LblTotalEntregas.Text = entregas.Count.ToString();
            LblTotalGarrafones.Text = entregas.Sum(e => e.CantidadGarrafones).ToString();
            LblRepartidores.Text = _todasEntregas.Select(e => e.RepartidorId).Distinct().Count().ToString();

            var diasActivos = entregas.Select(e => e.FechaHoraRegistro.Date).Distinct().Count();
            var promedioDiario = diasActivos > 0 ? entregas.Count / diasActivos : 0;
            LblPromedioDiario.Text = promedioDiario.ToString();
        }

        private void CargarRepartidores()
        {
            var repartidores = _todasEntregas.Select(e => e.RepartidorNombre).Distinct().ToList();
            repartidores.Insert(0, "Todos");
            PickerFiltroRepartidor.ItemsSource = repartidores;
            PickerFiltroRepartidor.SelectedIndex = 0;
        }

        private async void OnFiltroChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
            CalcularEstadisticas();
        }

        private void OnFiltroHoyClicked(object sender, EventArgs e)
        {
            DateFechaInicio.Date = DateTime.Now.Date;
            DateFechaFin.Date = DateTime.Now.Date;
            OnFiltroChanged(null, null);
        }

        private void OnFiltroSemanaClicked(object sender, EventArgs e)
        {
            DateFechaInicio.Date = DateTime.Now.AddDays(-7).Date;
            DateFechaFin.Date = DateTime.Now.Date;
            OnFiltroChanged(null, null);
        }

        private void OnFiltroMesClicked(object sender, EventArgs e)
        {
            DateFechaInicio.Date = DateTime.Now.AddDays(-30).Date;
            DateFechaFin.Date = DateTime.Now.Date;
            OnFiltroChanged(null, null);
        }

        private void OnFiltroTodosClicked(object sender, EventArgs e)
        {
            DateFechaInicio.Date = new DateTime(2000, 1, 1);
            DateFechaFin.Date = DateTime.Now.Date;
            OnFiltroChanged(null, null);
        }

        private void OnBuscarTextoChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                AplicarFiltros();
            }
            else
            {
                var filtradas = _todasEntregas
                    .Where(x => x.ClienteNombre.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase) ||
                               x.RepartidorNombre.Contains(e.NewTextValue, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.FechaHoraRegistro)
                    .ToList();
                ListaEntregas.ItemsSource = filtradas;
            }
        }

        private async void OnEntregaSeleccionada(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is EntregaLocal entrega)
            {
                var action = await DisplayActionSheet($"Entrega: {entrega.ClienteNombre}", "Cancelar", null, "Editar cantidad", "Cambiar cliente", "Eliminar");

                if (action == "Editar cantidad")
                {
                    var nuevaCantidad = await DisplayPromptAsync("Editar Cantidad", "Nueva cantidad de garrafones:",
                        initialValue: entrega.CantidadGarrafones.ToString(), keyboard: Keyboard.Numeric);

                    if (!string.IsNullOrEmpty(nuevaCantidad) && int.TryParse(nuevaCantidad, out int cantidad) && cantidad > 0 && cantidad != entrega.CantidadGarrafones)
                    {
                        entrega.CantidadGarrafones = cantidad;
                        await _localDbService.ActualizarEntrega(entrega);
                        CargarDatos();
                        await DisplayAlert("Éxito", "Cantidad actualizada", "OK");
                    }
                }
                else if (action == "Cambiar cliente")
                {
                    var nuevoClienteId = await SeleccionarCliente();
                    if (!string.IsNullOrEmpty(nuevoClienteId) && nuevoClienteId != entrega.ClienteId)
                    {
                        var nuevoCliente = await ObtenerClientePorId(nuevoClienteId);
                        if (nuevoCliente != null)
                        {
                            entrega.ClienteId = nuevoCliente.Id;
                            entrega.ClienteNombre = nuevoCliente.NombreCompleto;
                            entrega.Direccion = nuevoCliente.Direccion;
                            await _localDbService.ActualizarEntrega(entrega);
                            CargarDatos();
                            await DisplayAlert("Éxito", "Cliente cambiado correctamente", "OK");
                        }
                    }
                }
                else if (action == "Eliminar")
                {
                    var confirmar = await DisplayAlert("Confirmar", $"¿Eliminar entrega de {entrega.ClienteNombre}?", "Sí", "No");
                    if (confirmar)
                    {
                        await _localDbService.EliminarEntrega(entrega.IdLocal);
                        CargarDatos();
                    }
                }
                ListaEntregas.SelectedItem = null;
            }
        }

        private async Task<string> SeleccionarCliente()
        {
            var response = await _supabaseClient.Rpc("get_all_clientes", new { });
            if (response.Content != null)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var listaClientes = JsonSerializer.Deserialize<List<Cliente>>(response.Content, options) ?? new List<Cliente>();
                var opciones = listaClientes.Select(c => c.NombreCompleto).ToArray();
                var resultado = await DisplayActionSheet("Seleccionar Cliente", "Cancelar", null, opciones);
                var clienteSeleccionado = listaClientes.FirstOrDefault(c => c.NombreCompleto == resultado);
                return clienteSeleccionado?.Id ?? string.Empty;
            }
            return string.Empty;
        }

        private async Task<Cliente> ObtenerClientePorId(string id)
        {
            var response = await _supabaseClient.Rpc("get_all_clientes", new { });
            if (response.Content != null)
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var clientes = JsonSerializer.Deserialize<List<Cliente>>(response.Content, options) ?? new List<Cliente>();
                return clientes.FirstOrDefault(c => c.Id == id);
            }
            return null;
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var entrega = button?.CommandParameter as EntregaLocal;
            if (entrega == null) return;

            var action = await DisplayActionSheet($"Editar: {entrega.ClienteNombre}", "Cancelar", null, "Cantidad", "Cliente");

            if (action == "Cantidad")
            {
                var nuevaCantidad = await DisplayPromptAsync("Editar", "Nueva cantidad:",
                    initialValue: entrega.CantidadGarrafones.ToString(), keyboard: Keyboard.Numeric);

                if (!string.IsNullOrEmpty(nuevaCantidad) && int.TryParse(nuevaCantidad, out int cantidad) && cantidad > 0 && cantidad != entrega.CantidadGarrafones)
                {
                    entrega.CantidadGarrafones = cantidad;
                    await _localDbService.ActualizarEntrega(entrega);
                    CargarDatos();
                    await DisplayAlert("Éxito", "Cantidad actualizada", "OK");
                }
            }
            else if (action == "Cliente")
            {
                var nuevoClienteId = await SeleccionarCliente();
                if (!string.IsNullOrEmpty(nuevoClienteId) && nuevoClienteId != entrega.ClienteId)
                {
                    var nuevoCliente = await ObtenerClientePorId(nuevoClienteId);
                    if (nuevoCliente != null)
                    {
                        entrega.ClienteId = nuevoCliente.Id;
                        entrega.ClienteNombre = nuevoCliente.NombreCompleto;
                        entrega.Direccion = nuevoCliente.Direccion;
                        await _localDbService.ActualizarEntrega(entrega);
                        CargarDatos();
                        await DisplayAlert("Éxito", $"Cliente cambiado a: {nuevoCliente.NombreCompleto}", "OK");
                    }
                }
            }
        }

        private async void OnGestionarUsuariosClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GestionUsuariosPage());
        }

        private async void OnGestionarClientesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ClientesPage());
        }

        private async void OnEstadisticasClicked(object sender, EventArgs e)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try
            {
                await Navigation.PushModalAsync(new EstadisticasPage());
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async void OnSyncClicked(object sender, EventArgs e)
        {
            var boton = sender as Button;
            IndicatorCarga.IsVisible = true;
            IndicatorCarga.IsRunning = true;

            if (boton != null)
            {
                boton.IsEnabled = false;
                boton.Text = "🔄 Sincronizando...";
            }

            try
            {
                var subidos = await _syncService.SyncUpdatedDeliveries();
                var bajados = await _syncService.SyncAdminDeliveries();

                if (subidos > 0 && bajados > 0)
                    await DisplayAlert("Sincronización", $"✅ Subidos: {subidos}\n📥 Descargados: {bajados}", "OK");
                else if (subidos > 0)
                    await DisplayAlert("Sincronización", $"✅ Subidos: {subidos} cambios", "OK");
                else if (bajados > 0)
                    await DisplayAlert("Sincronización", $"📥 Descargados: {bajados} registros", "OK");
                else
                    await DisplayAlert("Sincronización", "📭 No hay cambios pendientes", "OK");

                CargarDatos();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"❌ Error: {ex.Message}", "OK");
            }
            finally
            {
                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;
                if (boton != null)
                {
                    boton.IsEnabled = true;
                    boton.Text = "🔄 Sincronizar";
                }
            }
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

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (await _syncService.HasInternetConnection())
            {
                var bajados = await _syncService.SyncAdminDeliveries();
                Debug.WriteLine($"Descargadas {bajados} entregas desde Supabase");
            }
            CargarDatos();
        }
    }
}