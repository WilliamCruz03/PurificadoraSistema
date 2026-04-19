using PurificadoraApp.Models;
using PurificadoraApp.Services;

namespace PurificadoraApp.Views
{
    public partial class EstadisticasPage : ContentPage
    {
        private readonly LocalDbService _localDbService;

        public EstadisticasPage()
        {
            InitializeComponent();
            _localDbService = MauiProgram.GetService<LocalDbService>();
            CargarEstadisticas();
        }

        private async void CargarEstadisticas()
        {
            var entregas = await _localDbService.GetAllEntregas();

            // Totales
            var totalEntregas = entregas.Count;
            var totalGarrafones = entregas.Sum(e => e.CantidadGarrafones);
            var promedio = totalEntregas > 0 ? totalGarrafones / totalEntregas : 0;
            var totalRepartidores = entregas.Select(e => e.RepartidorId).Distinct().Count();

            LblTotalEntregas.Text = totalEntregas.ToString();
            LblTotalGarrafones.Text = totalGarrafones.ToString();
            LblPromedio.Text = promedio.ToString();
            LblRepartidores.Text = totalRepartidores.ToString();

            // Top repartidores
            var topRepartidores = entregas
                .GroupBy(e => e.RepartidorNombre)
                .Select(g => new { Nombre = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            ListaTopRepartidores.ItemsSource = topRepartidores;

            // Últimas entregas
            var ultimasEntregas = entregas
                .OrderByDescending(e => e.FechaHoraRegistro)
                .Take(10)
                .ToList();

            ListaUltimasEntregas.ItemsSource = ultimasEntregas;
        }

        private async void OnCerrarClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}