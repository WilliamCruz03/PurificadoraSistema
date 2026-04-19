namespace PurificadoraApp.Services
{
    public static class ToastService
    {
        public static async Task ShowMessage(string message)
        {
            await Application.Current.MainPage.DisplayAlert("Info", message, "OK");
        }

        public static async Task ShowSuccess(string message)
        {
            await Application.Current.MainPage.DisplayAlert("✅ Éxito", message, "OK");
        }

        public static async Task ShowError(string message)
        {
            await Application.Current.MainPage.DisplayAlert("❌ Error", message, "OK");
        }

        public static async Task ShowInfo(string message)
        {
            await Application.Current.MainPage.DisplayAlert("ℹ️ Información", message, "OK");
        }

        public static async Task ShowSyncResult(int subidos, int bajados)
        {
            if (subidos > 0 || bajados > 0)
            {
                await ShowSuccess($"Subidos: {subidos}, Descargados: {bajados}");
            }
            else
            {
                await ShowInfo("No hay cambios pendientes");
            }
        }
    }
}