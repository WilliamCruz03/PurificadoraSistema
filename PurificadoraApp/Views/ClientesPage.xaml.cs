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
            }
            catch (Exception ex)
            {
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
            var nombre = await DisplayPromptAsync("Nuevo Cliente", "Nombre:", keyboard: Keyboard.Text);
            if (string.IsNullOrWhiteSpace(nombre)) return;

            var apellidos = await DisplayPromptAsync("Nuevo Cliente", "Apellidos (opcional):", keyboard: Keyboard.Text);
            var direccion = await DisplayPromptAsync("Nuevo Cliente", "Dirección:");
            if (string.IsNullOrWhiteSpace(direccion)) return;

            var telefono = await DisplayPromptAsync("Nuevo Cliente", "Teléfono (opcional):", keyboard: Keyboard.Telephone);
            var email = await DisplayPromptAsync("Nuevo Cliente", "Email (opcional):", keyboard: Keyboard.Email);

            try
            {
                await _supabaseAdminClient.Rpc("create_cliente", new
                {
                    p_nombre = nombre,
                    p_apellidos = apellidos ?? "",
                    p_direccion = direccion,
                    p_telefono = telefono ?? "",
                    p_email = email ?? ""
                });

                await DisplayAlert("Éxito", "Cliente creado correctamente", "OK");
                CargarClientes();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo crear: {ex.Message}", "OK");
            }
        }

        private async void OnEditarClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var cliente = button?.CommandParameter as Cliente;
            if (cliente == null) return;

            var nuevoNombre = await DisplayPromptAsync("Editar", "Nombre:", initialValue: cliente.Nombre);
            var nuevoApellidos = await DisplayPromptAsync("Editar", "Apellidos:", initialValue: cliente.Apellidos ?? "");
            var nuevaDireccion = await DisplayPromptAsync("Editar", "Dirección:", initialValue: cliente.Direccion);
            var nuevoTelefono = await DisplayPromptAsync("Editar", "Teléfono:", initialValue: cliente.Telefono ?? "");
            var nuevoEmail = await DisplayPromptAsync("Editar", "Email:", initialValue: cliente.Email ?? "");

            try
            {
                await _supabaseAdminClient.Rpc("update_cliente", new
                {
                    p_id = cliente.Id,
                    p_nombre = nuevoNombre ?? cliente.Nombre,
                    p_apellidos = nuevoApellidos,
                    p_direccion = nuevaDireccion ?? cliente.Direccion,
                    p_telefono = nuevoTelefono,
                    p_email = nuevoEmail
                });

                await DisplayAlert("Éxito", "Cliente actualizado", "OK");
                CargarClientes();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
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
                    await _supabaseAdminClient.Rpc("delete_cliente", new { p_id = cliente.Id });
                    await DisplayAlert("Éxito", "Cliente eliminado", "OK");
                    CargarClientes();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }
    }
}