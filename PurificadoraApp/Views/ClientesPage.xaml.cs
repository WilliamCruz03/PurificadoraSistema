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

            bool hayCambios = false;
            string nuevoNombre = null;
            string nuevoApellidos = null;
            string nuevaDireccion = null;
            string nuevoTelefono = null;
            string nuevoEmail = null;

            // Editar nombre - validar cancelar
            var nombreResult = await DisplayPromptAsync("Editar Cliente", "Nombre:",
                initialValue: cliente.Nombre, cancel: "Cancelar");

            if (nombreResult != null)
            {
                if (string.IsNullOrWhiteSpace(nombreResult))
                {
                    await DisplayAlert("Error", "El nombre no puede estar vacío", "OK");
                    return;
                }
                if (nombreResult != cliente.Nombre)
                {
                    nuevoNombre = nombreResult;
                    hayCambios = true;
                }
            }
            else
            {
                return; // Canceló
            }

            // Editar apellidos (opcional)
            var apellidosResult = await DisplayPromptAsync("Editar Cliente", "Apellidos (opcional):",
                initialValue: cliente.Apellidos ?? "", cancel: "Cancelar");

            if (apellidosResult != null)
            {
                if (apellidosResult != cliente.Apellidos)
                {
                    nuevoApellidos = apellidosResult;
                    hayCambios = true;
                }
            }
            else
            {
                return; // Canceló
            }

            // Editar dirección - obligatoria
            var direccionResult = await DisplayPromptAsync("Editar Cliente", "Dirección:",
                initialValue: cliente.Direccion, cancel: "Cancelar");

            if (direccionResult != null)
            {
                if (string.IsNullOrWhiteSpace(direccionResult))
                {
                    await DisplayAlert("Error", "La dirección no puede estar vacía", "OK");
                    return;
                }
                if (direccionResult != cliente.Direccion)
                {
                    nuevaDireccion = direccionResult;
                    hayCambios = true;
                }
            }
            else
            {
                return; // Canceló
            }

            // Editar teléfono (opcional)
            var telefonoResult = await DisplayPromptAsync("Editar Cliente", "Teléfono (opcional):",
                initialValue: cliente.Telefono ?? "", cancel: "Cancelar");

            if (telefonoResult != null)
            {
                if (telefonoResult != cliente.Telefono)
                {
                    nuevoTelefono = telefonoResult;
                    hayCambios = true;
                }
            }
            else
            {
                return; // Canceló
            }

            // Editar email (opcional)
            var emailResult = await DisplayPromptAsync("Editar Cliente", "Email (opcional):",
                initialValue: cliente.Email ?? "", keyboard: Keyboard.Email, cancel: "Cancelar");

            if (emailResult != null)
            {
                if (emailResult != cliente.Email)
                {
                    nuevoEmail = emailResult;
                    hayCambios = true;
                }
            }
            else
            {
                return; // Canceló
            }

            // Solo actualizar si hay cambios
            if (hayCambios)
            {
                try
                {
                    await _supabaseAdminClient.Rpc("update_cliente", new
                    {
                        p_id = cliente.Id,
                        p_nombre = nuevoNombre ?? cliente.Nombre,
                        p_apellidos = nuevoApellidos ?? cliente.Apellidos,
                        p_direccion = nuevaDireccion ?? cliente.Direccion,
                        p_telefono = nuevoTelefono ?? cliente.Telefono,
                        p_email = nuevoEmail ?? cliente.Email
                    });

                    await DisplayAlert("Éxito", "Cliente actualizado correctamente", "OK");
                    CargarClientes();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"No se pudo actualizar: {ex.Message}", "OK");
                }
            }
            else
            {
                await DisplayAlert("Info", "No se realizaron cambios", "OK");
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