using PurificadoraApp.Models;
using PurificadoraApp.Services;
using System.Diagnostics;

namespace PurificadoraApp.Views
{
    public partial class EstadisticasPage : ContentPage
    {
        private readonly LocalDbService _localDbService;
        private bool _isLoading = false;
        private static bool _isOpen = false;

        public EstadisticasPage()
        {
            InitializeComponent();
            _localDbService = MauiProgram.GetService<LocalDbService>();

            if (_isOpen)
            {
                Navigation.PopModalAsync();
                return;
            }
            _isOpen = true;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await CargarEstadisticas();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _isOpen = false;
        }

        private async Task CargarEstadisticas()
        {
            if (_isLoading) return;
            _isLoading = true;

            try
            {
                IndicatorCarga.IsVisible = true;
                IndicatorCarga.IsRunning = true;

                var entregas = await Task.Run(async () => await _localDbService.GetAllEntregas());

                var totalEntregas = entregas.Count;
                var totalGarrafones = entregas.Sum(e => e.CantidadGarrafones);
                var promedio = totalEntregas > 0 ? totalGarrafones / totalEntregas : 0;
                var totalRepartidores = entregas.Select(e => e.RepartidorId).Distinct().Count();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    LblTotalEntregas.Text = totalEntregas.ToString();
                    LblTotalGarrafones.Text = totalGarrafones.ToString();
                    LblPromedio.Text = promedio.ToString();
                    LblRepartidores.Text = totalRepartidores.ToString();
                });

                // Día con más entregas
                var diaMax = entregas
                    .GroupBy(e => e.FechaHoraRegistro.Date)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (diaMax != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        LblDiaMaximo.Text = $"{diaMax.Key:dd/MM/yyyy} ({diaMax.Count()})";
                    });
                }

                var topRepartidores = entregas
                    .GroupBy(e => e.RepartidorNombre)
                    .Select(g => new { Nombre = g.Key, Total = g.Count() })
                    .OrderByDescending(x => x.Total)
                    .Take(5)
                    .ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ListaTopRepartidores.ItemsSource = topRepartidores;
                });

                var ultimasEntregas = entregas
                    .OrderByDescending(e => e.FechaHoraRegistro)
                    .Take(10)
                    .ToList();

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ListaUltimasEntregas.ItemsSource = ultimasEntregas;
                });

                IndicatorCarga.IsVisible = false;
                IndicatorCarga.IsRunning = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en EstadisticasPage: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    IndicatorCarga.IsVisible = false;
                    IndicatorCarga.IsRunning = false;
                    await DisplayAlert("Error", $"No se pudieron cargar las estadísticas: {ex.Message}", "OK");
                });
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void OnCerrarClicked(object sender, EventArgs e)
        {
            if (_isLoading) return;
            _isOpen = false;
            await Navigation.PopModalAsync();
        }
    }
}