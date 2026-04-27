using PurificadoraApp.Services;

namespace PurificadoraApp.Views
{
    public partial class FormularioUsuarioPage : ContentPage
    {
        private readonly Supabase.Client _supabaseAdminClient;

        public FormularioUsuarioPage()
        {
            InitializeComponent();
            _supabaseAdminClient = MauiProgram.GetService<Supabase.Client>();
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

            if (string.IsNullOrWhiteSpace(TxtPassword.Text))
            {
                await ToastService.Error("Ingrese una contraseña");
                return;
            }

            if (TxtPassword.Text != TxtConfirmPassword.Text)
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

                var response = await _supabaseAdminClient.Rpc("create_user_with_username", new
                {
                    p_email = TxtEmail.Text,
                    p_username = TxtUsername.Text,
                    p_password = TxtPassword.Text,
                    p_nombre = TxtNombre.Text,
                    p_rol = rol
                });

                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;

                await ToastService.Success("Usuario creado correctamente");
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