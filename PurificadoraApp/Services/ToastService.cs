namespace PurificadoraApp.Services
{
    public static class ToastService
    {
        public static async Task Show(string message, bool isError = false)
        {
            var toastDuration = TimeSpan.FromSeconds(3);

            var toastLabel = new Label
            {
                Text = message,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Colors.White,
                Margin = new Thickness(20, 10),
                FontSize = 14
            };

            var toastStack = new StackLayout
            {
                Children = { toastLabel },
                BackgroundColor = isError ? Color.FromArgb("#E74C3C") : Color.FromArgb("#2C3E50"),
                CornerRadius = 10,
                Margin = new Thickness(20),
                Padding = 0,
                Opacity = 0,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                WidthRequest = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - 40
            };

            var parentGrid = (Application.Current.MainPage as ContentPage)?.Content as Grid;
            if (parentGrid == null) return;

            parentGrid.Children.Add(toastStack);
            parentGrid.SetRow(toastStack, parentGrid.RowDefinitions.Count - 1);

            await toastStack.FadeTo(1, 250);
            await Task.Delay(toastDuration);
            await toastStack.FadeTo(0, 250);

            parentGrid.Children.Remove(toastStack);
        }

        public static async Task Success(string message)
        {
            await Show(message);
        }

        public static async Task Error(string message)
        {
            await Show(message, true);
        }

        public static async Task Info(string message)
        {
            await Show(message);
        }

        public static async Task SyncResult(int subidos, int bajados)
        {
            if (subidos > 0 || bajados > 0)
                await Show($"📤 Subidos: {subidos}  |  📥 Descargados: {bajados}");
            else
                await Show("📭 No hay cambios pendientes");
        }
    }
}