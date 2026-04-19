namespace PurificadoraApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Registrar rutas para navegación
            Routing.RegisterRoute("login", typeof(Views.LoginPage));
            Routing.RegisterRoute("main", typeof(MainPage));
        }
    }
}