using Supabase;
using PurificadoraApp.Models;
using System.Text.Json;
using Supabase.Postgrest.Models;
using Microsoft.Maui.Networking;
using System.Diagnostics;

namespace PurificadoraApp.Services
{
    [Supabase.Postgrest.Attributes.Table("entregas")]
    public class EntregaRemota : BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("repartidor_id")]
        public string RepartidorId { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("repartidor_nombre")]
        public string RepartidorNombre { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("cliente_nombre")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("direccion")]
        public string Direccion { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("cantidad_garrafones")]
        public int CantidadGarrafones { get; set; }

        [Supabase.Postgrest.Attributes.Column("fecha_hora_registro")]
        public DateTime FechaHoraRegistro { get; set; }

        [Supabase.Postgrest.Attributes.Column("version")]
        public int Version { get; set; }
    }

    public class SyncService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly LocalDbService _localDbService;

        public SyncService(Supabase.Client supabaseClient, LocalDbService localDbService)
        {
            _supabaseClient = supabaseClient;
            _localDbService = localDbService;
        }

        public async Task<bool> HasInternetConnection()
        {
            try
            {
                var current = Connectivity.Current;
                return current.NetworkAccess == NetworkAccess.Internet;
            }
            catch
            {
                return false;
            }
        }

        public async Task<int> SyncUpdatedDeliveries()
        {
            if (!await HasInternetConnection())
                return 0;

            var pendientes = await _localDbService.GetEntregasPendientes();
            int sincronizados = 0;

            foreach (var entregaLocal in pendientes)
            {
                try
                {
                    Debug.WriteLine($"Sincronizando entrega ID local: {entregaLocal.IdLocal}, Remoto: {entregaLocal.IdRemoto ?? "Nuevo"}");

                    if (!string.IsNullOrEmpty(entregaLocal.IdRemoto))
                    {
                        var entregaRemota = new EntregaRemota
                        {
                            Id = entregaLocal.IdRemoto,
                            RepartidorId = entregaLocal.RepartidorId,
                            RepartidorNombre = entregaLocal.RepartidorNombre,
                            ClienteNombre = entregaLocal.ClienteNombre,
                            Direccion = entregaLocal.Direccion,
                            CantidadGarrafones = entregaLocal.CantidadGarrafones,
                            FechaHoraRegistro = entregaLocal.FechaHoraRegistro,
                            Version = entregaLocal.Version + 1
                        };

                        await _supabaseClient.From<EntregaRemota>().Update(entregaRemota);
                        Debug.WriteLine($"  Actualizado en Supabase - ID: {entregaLocal.IdRemoto}");
                    }
                    else
                    {
                        var entregaRemota = new EntregaRemota
                        {
                            RepartidorId = entregaLocal.RepartidorId,
                            RepartidorNombre = entregaLocal.RepartidorNombre,
                            ClienteNombre = entregaLocal.ClienteNombre,
                            Direccion = entregaLocal.Direccion,
                            CantidadGarrafones = entregaLocal.CantidadGarrafones,
                            FechaHoraRegistro = entregaLocal.FechaHoraRegistro,
                            Version = entregaLocal.Version + 1
                        };

                        var response = await _supabaseClient.From<EntregaRemota>().Insert(entregaRemota);
                        await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 1, response.Model?.Id);
                        Debug.WriteLine($"  Insertado en Supabase - Nuevo ID: {response.Model?.Id}");
                    }

                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 1);
                    sincronizados++;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sincronizando entrega {entregaLocal.IdLocal}: {ex.Message}");
                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 2, errorMessage: ex.Message);
                }
            }

            return sincronizados;
        }

        public async Task<int> SyncAdminDeliveries()
        {
            if (!await HasInternetConnection())
                return 0;

            try
            {
                var desde = DateTime.Now.AddDays(-30);
                var response = await _supabaseClient
                    .From<EntregaRemota>()
                    .Where(x => x.FechaHoraRegistro >= desde)
                    .Get();

                var entregasRemotas = response.Models;
                Debug.WriteLine($"Encontradas {entregasRemotas.Count} entregas en Supabase");

                int descargados = 0;
                foreach (var item in entregasRemotas)
                {
                    var existentes = await _localDbService.GetAllEntregas();
                    if (!existentes.Any(e => e.IdRemoto == item.Id))
                    {
                        var entregaLocal = new EntregaLocal
                        {
                            IdRemoto = item.Id,
                            RepartidorId = item.RepartidorId,
                            RepartidorNombre = item.RepartidorNombre,
                            ClienteNombre = item.ClienteNombre,
                            Direccion = item.Direccion,
                            CantidadGarrafones = item.CantidadGarrafones,
                            FechaHoraRegistro = item.FechaHoraRegistro,
                            EstadoSync = 1,
                            Version = item.Version
                        };

                        await _localDbService.GuardarEntrega(entregaLocal);
                        descargados++;
                    }
                }

                Debug.WriteLine($"Nuevas entregas guardadas: {descargados}");
                return descargados;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al descargar: {ex.Message}");
                return 0;
            }
        }

        // SyncAll original (sin clientes)
        public async Task<(int subidos, int bajados)> SyncAll()
        {
            var subidos = await SyncUpdatedDeliveries();
            var bajados = await SyncAdminDeliveries();
            return (subidos, bajados);
        }
    }
}