using PurificadoraApp.Views;

namespace PurificadoraApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Verificar si hay sesión guardada
            var token = Preferences.Get("access_token", string.Empty);
            var usuarioJson = Preferences.Get("usuario_actual", string.Empty);

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(usuarioJson))
            {
                // Ir directamente al dashboard, el LoginViewModel manejará el modo offline
                MainPage = new NavigationPage(new Views.LoginPage());
            }
            else
            {
                MainPage = new NavigationPage(new Views.LoginPage());
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            if (window != null)
            {
                window.MinimumHeight = 600;
                window.MinimumWidth = 400;
            }
            return window;
        }
    }
}