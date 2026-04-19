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
        private List<EntregaLocal> _todasEntregas;


        public new event PropertyChangedEventHandler PropertyChanged;

        protected new virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public AdminDashboardPage()
        {
            InitializeComponent();
            _localDbService = MauiProgram.GetService<LocalDbService>();
            _supabaseClient = MauiProgram.GetService<Supabase.Client>();
            DateFechaInicio.Date = DateTime.Now.AddDays(-30);
            DateFechaFin.Date = DateTime.Now;
            CargarDatos();
        }

        private async void CargarDatos()
        {
            _todasEntregas = await _localDbService.GetAllEntregas();
            AplicarFiltros();
            CalcularEstadisticas();
            CargarRepartidores();
        }

        private void AplicarFiltros()
        {
            var filtradas = _todasEntregas.AsEnumerable();

            // Filtro por repartidor
            if (PickerFiltroRepartidor.SelectedIndex > 0)
            {
                var repartidorSeleccionado = PickerFiltroRepartidor.SelectedItem as string;
                filtradas = filtradas.Where(e => e.RepartidorNombre == repartidorSeleccionado);
            }

            // Filtro por fechas
            filtradas = filtradas.Where(e => e.FechaHoraRegistro.Date >= DateFechaInicio.Date);
            filtradas = filtradas.Where(e => e.FechaHoraRegistro.Date <= DateFechaFin.Date);

            var listaFiltrada = filtradas.OrderByDescending(e => e.FechaHoraRegistro).ToList();

            // Forzar refresco
            ListaEntregas.ItemsSource = null;
            ListaEntregas.ItemsSource = listaFiltrada;
        }

        private void CalcularEstadisticas()
        {
            var entregas = ListaEntregas.ItemsSource as List<EntregaLocal> ?? new List<EntregaLocal>();
            LblTotalEntregas.Text = entregas.Count.ToString();
            LblTotalGarrafones.Text = entregas.Sum(e => e.CantidadGarrafones).ToString();
            LblRepartidores.Text = _todasEntregas.Select(e => e.RepartidorId).Distinct().Count().ToString();
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
            await RecargarDatosCompletos();
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
                        await RecargarDatosCompletos();
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
                            await RecargarDatosCompletos();
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
                        await RecargarDatosCompletos(); 
                    }
                }

                // Limpiar selección
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
                    var resultado = await _localDbService.ActualizarEntrega(entrega);
                    Debug.WriteLine($"ActualizarEntrega resultado: {resultado}"); // Debería ser 1
                    await RecargarDatosCompletos();
                    await DisplayAlert("Éxito", "Cantidad actualizada localmente", "OK");
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
                        Debug.WriteLine($"Cliente seleccionado: {nuevoCliente.NombreCompleto}, ID: {nuevoCliente.Id}");

                        entrega.ClienteId = nuevoCliente.Id;
                        entrega.ClienteNombre = nuevoCliente.NombreCompleto;
                        entrega.Direccion = nuevoCliente.Direccion;

                        var resultado = await _localDbService.ActualizarEntrega(entrega);
                        Debug.WriteLine($"ActualizarEntrega resultado: {resultado}");

                        await RecargarDatosCompletos();
                        await DisplayAlert("Éxito", $"Cliente cambiado a: {nuevoCliente.NombreCompleto}", "OK");
                    }
                }
            }
        }

        private async void OnNuevoUsuarioClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GestionUsuariosPage());
        }

        private async void OnGestionarUsuariosClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GestionUsuariosPage());
        }

        private async void OnGestionarClientesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ClientesPage());
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Forzar sincronización al abrir el dashboard
            if (await _syncService.HasInternetConnection())
            {
                var syncService = MauiProgram.GetService<SyncService>();
                var (subidos, bajados, clientes) = await syncService.SyncAll();
                Debug.WriteLine($"Sincronización inicial: Subidos={subidos}, Bajados={bajados}, Clientes={clientes}");
            }

            CargarDatos();
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

        private async Task RecargarDatosCompletos()
        {
            Debug.WriteLine("RecargarDatosCompletos: Iniciando...");

            // Guardar filtros actuales
            var filtroRepartidor = PickerFiltroRepartidor.SelectedIndex > 0
                ? PickerFiltroRepartidor.SelectedItem as string
                : null;
            var fechaInicio = DateFechaInicio.Date;
            var fechaFin = DateFechaFin.Date;

            // Recargar desde la base de datos local
            _todasEntregas = await _localDbService.GetAllEntregas();
            Debug.WriteLine($"Entregas cargadas: {_todasEntregas.Count}");

            // Aplicar filtros
            var filtradas = _todasEntregas.AsEnumerable();

            if (!string.IsNullOrEmpty(filtroRepartidor))
            {
                filtradas = filtradas.Where(e => e.RepartidorNombre == filtroRepartidor);
            }

            filtradas = filtradas.Where(e => e.FechaHoraRegistro.Date >= fechaInicio);
            filtradas = filtradas.Where(e => e.FechaHoraRegistro.Date <= fechaFin);

            var listaFiltrada = filtradas.OrderByDescending(e => e.FechaHoraRegistro).ToList();

            // FORZAR REFRESCO VISUAL
            ListaEntregas.ItemsSource = null;
            await Task.Delay(50); // Pequeña pausa para asegurar el refresco
            ListaEntregas.ItemsSource = listaFiltrada;

            // Recalcular estadísticas
            CalcularEstadisticas();

            // Recargar lista de repartidores (solo si es necesario)
            if (PickerFiltroRepartidor.ItemsSource == null ||
                PickerFiltroRepartidor.SelectedIndex == 0)
            {
                CargarRepartidores();
            }

            Debug.WriteLine($"RecargarDatosCompletos: Completado - {listaFiltrada.Count} entregas mostradas");
        }

        private async void OnSyncChangesClicked(object sender, EventArgs e)
        {
            var syncService = MauiProgram.GetService<SyncService>();
            var (subidos, bajados) = await syncService.SyncAll();

            await ToastService.ShowSyncResult(subidos, bajados);
            await RecargarDatosCompletos();
        }

        private async void OnEstadisticasClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new EstadisticasPage());
        }

        private async void OnSyncNowClicked(object sender, EventArgs e)
        {
            var syncService = MauiProgram.GetService<SyncService>();
            var (subidos, bajados, clientes) = await syncService.SyncAll();

            await DisplayAlert("Sincronización",
                $"Entregas subidas: {subidos}\nEntregas descargadas: {bajados}\nClientes descargados: {clientes}",
                "OK");

            CargarDatos();
        }
    }
}