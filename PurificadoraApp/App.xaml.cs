namespace PurificadoraApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var token = Preferences.Get("access_token", string.Empty);

            if (!string.IsNullOrEmpty(token))
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new NavigationPage(new Views.LoginPage());
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);

            // Configurar tamaño mínimo si es necesario
            if (window != null)
            {
                window.MinimumHeight = 600;
                window.MinimumWidth = 400;
            }

            return window;
        }
        protected override void OnStart()
        {
            base.OnStart();

            var client = MauiProgram.GetService<Supabase.Client>();
            System.Diagnostics.Debug.WriteLine($"Cliente Supabase: {client != null}");
            System.Diagnostics.Debug.WriteLine($"URL: {SupabaseConfig.Url}");
        }
    }

}