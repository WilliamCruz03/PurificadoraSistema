using PurificadoraApp.Services;
using PurificadoraApp.ViewModels;

namespace PurificadoraApp.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            InitializeComponent();

            var supabaseClient = MauiProgram.GetService<Supabase.Client>();
            var localDbService = MauiProgram.GetService<LocalDbService>();

            BindingContext = new LoginViewModel(supabaseClient, localDbService);
        }
    }
}