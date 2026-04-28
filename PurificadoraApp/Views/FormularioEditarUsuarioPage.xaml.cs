using PurificadoraApp.Services;

namespace PurificadoraApp.Views
{
    public partial class FormularioEditarUsuarioPage : ContentPage
    {
        private readonly Supabase.Client _supabaseAdminClient;
        private readonly GestionUsuariosPage.UserInfo _usuario;

        public FormularioEditarUsuarioPage(GestionUsuariosPage.UserInfo usuario)
        {
            InitializeComponent();
            _supabaseAdminClient = MauiProgram.GetService<Supabase.Client>();
            _usuario = usuario;
            CargarDatos();
        }

        private void CargarDatos()
        {
            // Cargar datos actuales
            TxtUsername.Text = _usuario.Username ?? _usuario.Email; // Usar username si existe
            TxtEmail.Text = _usuario.Email;
            TxtNombre.Text = _usuario.Nombre;
            PickerRol.SelectedItem = _usuario.Rol;
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtUsername.Text))
            {
                await ToastService.Error("Ingrese un nombre de usuario");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                await ToastService.Error("Ingrese un correo electrónico");
                return;
            }

            if (!string.IsNullOrWhiteSpace(TxtPassword.Text) && TxtPassword.Text != TxtConfirmPassword.Text)
            {
                await ToastService.Error("Las contraseñas no coinciden");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                await ToastService.Error("Ingrese el nombre completo");
                return;
            }

            var rol = PickerRol.SelectedItem?.ToString() ?? "Repartidor";

            try
            {
                IndicatorCarga.IsVisible = true;
                IndicatorCarga.IsRunning = true;

                // Actualizar usuario (con username)
                await _supabaseAdminClient.Rpc("update_user_full", new
                {
                    p_user_id = _usuario.Id,
                    p_email = TxtEmail.Text,
                    p_username = TxtUsername.Text,  // Ahora usa el username del campo
                    p_nombre = TxtNombre.Text,
                    p_rol = rol
                });

                // Si se proporcionó nueva contraseña, actualizarla
                if (!string.IsNullOrWhiteSpace(TxtPassword.Text))
                {
                    await _supabaseAdminClient.Rpc("update_user_password", new
                    {
                        p_user_id = _usuario.Id,
                        p_password = TxtPassword.Text
                    });
                }

                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;

                await ToastService.Success("Usuario actualizado correctamente");
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