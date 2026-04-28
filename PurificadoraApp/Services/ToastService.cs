using Microsoft.Maui.Devices;
using Microsoft.Maui.Controls;

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

            // Usar Frame en lugar de StackLayout para tener CornerRadius
            var toastFrame = new Frame
            {
                Content = toastLabel,
                BackgroundColor = isError ? Color.FromArgb("#E74C3C") : Color.FromArgb("#2C3E50"),
                CornerRadius = 10,
                Margin = new Thickness(20),
                Padding = new Thickness(10, 5),
                Opacity = 0,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.End,
                HasShadow = false,
                WidthRequest = DeviceDisplay.Current.MainDisplayInfo.Width / DeviceDisplay.Current.MainDisplayInfo.Density - 40
            };

            var parentGrid = (Application.Current.MainPage as ContentPage)?.Content as Grid;
            if (parentGrid == null) return;

            parentGrid.Children.Add(toastFrame);

            // Determinar la última fila disponible
            var lastRow = parentGrid.RowDefinitions.Count - 1;
            if (lastRow >= 0)
            {
                Grid.SetRow(toastFrame, lastRow);
            }

            await toastFrame.FadeTo(1, 250);
            await Task.Delay(toastDuration);
            await toastFrame.FadeTo(0, 250);

            parentGrid.Children.Remove(toastFrame);
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