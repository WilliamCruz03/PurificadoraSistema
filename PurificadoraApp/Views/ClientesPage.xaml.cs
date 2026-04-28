using System.Text.Json;
using PurificadoraApp.Models;
using PurificadoraApp.Services;
using Supabase;

namespace PurificadoraApp.Views
{
    public partial class ClientesPage : ContentPage
    {
        private readonly Supabase.Client _supabaseAdminClient;
        private List<Cliente> _clientes;

        public ClientesPage()
        {
            InitializeComponent();
            _supabaseAdminClient = MauiProgram.GetService<Supabase.Client>();
            CargarClientes();
        }

        private async void CargarClientes(string busqueda = null)
        {
            try
            {
                IndicatorCarga.IsVisible = true;
                IndicatorCarga.IsRunning = true;

                object parameters;
                string rpcFunction;

                if (!string.IsNullOrWhiteSpace(busqueda))
                {
                    rpcFunction = "search_clientes";
                    parameters = new { search_term = busqueda };
                }
                else
                {
                    rpcFunction = "get_all_clientes";
                    parameters = new { };
                }

                var response = await _supabaseAdminClient.Rpc(rpcFunction, parameters);

                if (response.Content != null)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _clientes = JsonSerializer.Deserialize<List<Cliente>>(response.Content, options) ?? new List<Cliente>();
                    ListaClientes.ItemsSource = _clientes;
                }

                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;
            }
            catch (Exception ex)
            {
                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;
                await DisplayAlert("Error", $"No se pudieron cargar clientes: {ex.Message}", "OK");
            }
        }

        private void OnBuscarTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                CargarClientes();
            }
        }

        private void OnBuscarClicked(object sender, EventArgs e)
        {
            CargarClientes(TxtBuscar.Text);
        }

        private async void OnNuevoClienteClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new FormularioClientePage());
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var cliente = button?.CommandParameter as Cliente;
            if (cliente == null) return;

            await Navigation.PushModalAsync(new FormularioClientePage(cliente));
        }

        private async void OnEliminarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var cliente = button?.CommandParameter as Cliente;
            if (cliente == null) return;

            var confirmar = await DisplayAlert("Confirmar", $"¿Eliminar cliente {cliente.NombreCompleto}?", "Sí", "No");
            if (confirmar)
            {
                try
                {
                    IndicatorCarga.IsVisible = true;
                    IndicatorCarga.IsRunning = true;

                    await _supabaseAdminClient.Rpc("delete_cliente", new { p_id = cliente.Id });

                    IndicatorCarga.IsVisible = false;
                    IndicatorCarga.IsRunning = false;

                    await DisplayAlert("Éxito", "Cliente eliminado", "OK");
                    CargarClientes();
                }
                catch (Exception ex)
                {
                    IndicatorCarga.IsVisible = false;
                    IndicatorCarga.IsRunning = false;
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            CargarClientes(); // Recargar cada vez que se muestra la página
        }
    }
}