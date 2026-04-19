using PurificadoraApp.Models;
using PurificadoraApp.Services;

namespace PurificadoraApp.Views
{
    public partial class AdminDashboardPage : ContentPage
    {
        private readonly LocalDbService _localDbService;
        private List<EntregaLocal> _todasEntregas;

        public AdminDashboardPage()
        {
            InitializeComponent();
            _localDbService = MauiProgram.GetService<LocalDbService>();
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

            ListaEntregas.ItemsSource = filtradas.OrderByDescending(e => e.FechaHoraRegistro).ToList();
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

        private void OnFiltroChanged(object sender, EventArgs e)
        {
            AplicarFiltros();
            CalcularEstadisticas();
        }

        private async void OnEntregaSeleccionada(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is EntregaLocal entrega)
            {
                var action = await DisplayActionSheet($"Entrega: {entrega.ClienteNombre}", "Cancelar", null, "Editar", "Eliminar");

                if (action == "Editar")
                {
                    var nuevoCliente = await DisplayPromptAsync("Editar", "Nuevo nombre del cliente:", initialValue: entrega.ClienteNombre);
                    if (!string.IsNullOrEmpty(nuevoCliente) && nuevoCliente != entrega.ClienteNombre)
                    {
                        // ACTUALIZAR, no crear nuevo
                        entrega.ClienteNombre = nuevoCliente;
                        await _localDbService.ActualizarEntrega(entrega);  // Necesitas este método
                        CargarDatos();
                        await DisplayAlert("Éxito", "Entrega actualizada", "OK");
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
            }
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var entrega = button?.CommandParameter as EntregaLocal;
            if (entrega != null)
            {
                var nuevoCliente = await DisplayPromptAsync("Editar", "Nuevo nombre del cliente:", initialValue: entrega.ClienteNombre);
                if (!string.IsNullOrEmpty(nuevoCliente) && nuevoCliente != entrega.ClienteNombre)
                {
                    // ACTUALIZAR, no crear nuevo
                    entrega.ClienteNombre = nuevoCliente;
                    await _localDbService.ActualizarEntrega(entrega);
                    CargarDatos();
                    await DisplayAlert("Éxito", "Entrega actualizada", "OK");
                }
            }
        }

        private async void OnNuevoUsuarioClicked(object sender, EventArgs e)
        {
            // Aquí implementaremos la creación de usuarios más adelante
            await DisplayAlert("Info", "Funcionalidad para crear usuarios (próximamente)", "OK");
        }

        private async void OnGestionarUsuariosClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new GestionUsuariosPage());
        }

    }

}