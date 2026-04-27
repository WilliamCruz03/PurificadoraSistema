using Android.Views;
using PurificadoraApp.Models;
using PurificadoraApp.Services;

namespace PurificadoraApp.Views
{
    public partial class FormularioClientePage : ContentPage
    {
        private readonly Supabase.Client _supabaseAdminClient;
        private readonly Cliente _cliente; // Si es null, es nuevo cliente
        private readonly bool _esEdicion;

        public string Titulo => _esEdicion ? "Editar Cliente" : "Nuevo Cliente";

        public FormularioClientePage(Cliente cliente = null)
        {
            InitializeComponent();
            _supabaseAdminClient = MauiProgram.GetService<Supabase.Client>();
            _cliente = cliente;
            _esEdicion = cliente != null;
            BindingContext = this;

            if (_esEdicion)
            {
                CargarDatos();
            }
        }

        private void CargarDatos()
        {
            TxtNombre.Text = _cliente.Nombre;
            TxtApellidos.Text = _cliente.Apellidos;
            TxtDireccion.Text = _cliente.Direccion;
            TxtTelefono.Text = _cliente.Telefono;
            TxtEmail.Text = _cliente.Email;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                await ToastService.Error("El nombre es obligatorio");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtDireccion.Text))
            {
                await ToastService.Error("La dirección es obligatoria");
                return;
            }

            try
            {
                IndicatorCarga.IsVisible = true;
                IndicatorCarga.IsRunning = true;

                if (_esEdicion)
                {
                    // Actualizar cliente existente
                    await _supabaseAdminClient.Rpc("update_cliente", new
                    {
                        p_id = _cliente.Id,
                        p_nombre = TxtNombre.Text,
                        p_apellidos = TxtApellidos.Text ?? "",
                        p_direccion = TxtDireccion.Text,
                        p_telefono = TxtTelefono.Text ?? "",
                        p_email = TxtEmail.Text ?? ""
                    });
                    await ToastService.Success("Cliente actualizado correctamente");
                }
                else
                {
                    // Crear nuevo cliente
                    await _supabaseAdminClient.Rpc("create_cliente", new
                    {
                        p_nombre = TxtNombre.Text,
                        p_apellidos = TxtApellidos.Text ?? "",
                        p_direccion = TxtDireccion.Text,
                        p_telefono = TxtTelefono.Text ?? "",
                        p_email = TxtEmail.Text ?? ""
                    });
                    await ToastService.Success("Cliente creado correctamente");
                }

                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;

                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;
                await ToastService.Error($"Error: {ex.Message}");
            }
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}