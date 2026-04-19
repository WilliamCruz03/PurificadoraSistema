using Supabase;
using PurificadoraApp.Models;
using System.Text.Json;
using Supabase.Postgrest.Models;
using Microsoft.Maui.Networking;

namespace PurificadoraApp.Services
{
    // Clase que representa la tabla 'entregas' en Supabase
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

        // Verificar si hay conexión a internet
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

        // Sincronizar entregas pendientes (SUBIR)
        public async Task<int> SyncPendingDeliveries()
        {
            if (!await HasInternetConnection())
                return 0;

            var pendientes = await _localDbService.GetEntregasPendientes();
            int sincronizados = 0;

            foreach (var entregaLocal in pendientes)
            {
                try
                {
                    // Crear el objeto para Supabase usando la clase tipada
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

                    // Insertar en Supabase
                    var response = await _supabaseClient.From<EntregaRemota>().Insert(entregaRemota);

                    // Actualizar estado local
                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 1, response.Model?.Id);
                    sincronizados++;
                }
                catch (Exception ex)
                {
                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 2, errorMessage: ex.Message);
                }
            }

            return sincronizados;
        }

        // Descargar entregas del administrador (BAJAR)
        public async Task<int> SyncAdminDeliveries()
        {
            if (!await HasInternetConnection())
                return 0;

            try
            {
                // Obtener entregas de Supabase (solo últimas 30 días)
                var desde = DateTime.Now.AddDays(-30);

                var response = await _supabaseClient
                    .From<EntregaRemota>()
                    .Where(x => x.FechaHoraRegistro >= desde)
                    .Get();

                var entregasRemotas = response.Models;
                int descargados = 0;

                foreach (var item in entregasRemotas)
                {
                    // Verificar si ya existe localmente para no duplicar
                    var existentes = await _localDbService.GetAllEntregas();
                    if (!existentes.Any(e => e.IdRemoto == item.Id))
                    {
                        // Guardar en SQLite local (como referencia para el repartidor)
                        var entregaLocal = new EntregaLocal
                        {
                            IdRemoto = item.Id,
                            RepartidorId = item.RepartidorId,
                            RepartidorNombre = item.RepartidorNombre,
                            ClienteNombre = item.ClienteNombre,
                            Direccion = item.Direccion,
                            CantidadGarrafones = item.CantidadGarrafones,
                            FechaHoraRegistro = item.FechaHoraRegistro,
                            EstadoSync = 1, // Ya sincronizado
                            Version = item.Version
                        };

                        await _localDbService.GuardarEntrega(entregaLocal);
                        descargados++;
                    }
                }

                return descargados;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al descargar: {ex.Message}");
                return 0;
            }
        }

        // Sincronización completa
        public async Task<(int subidos, int bajados)> SyncAll()
        {
            var subidos = await SyncPendingDeliveries();
            var bajados = await SyncAdminDeliveries();
            return (subidos, bajados);
        }

        // Sincronizar entregas actualizadas (modificadas localmente)
        public async Task<int> SyncUpdatedDeliveries()
        {
            if (!await HasInternetConnection())
                return 0;

            // Obtener entregas que fueron actualizadas y están pendientes
            var actualizadas = await _localDbService.GetEntregasPendientes();
            int sincronizados = 0;

            foreach (var entregaLocal in actualizadas)
            {
                try
                {
                    if (!string.IsNullOrEmpty(entregaLocal.IdRemoto))
                    {
                        // Actualizar existente en Supabase
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
                    }
                    else
                    {
                        // Insertar nueva
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
                    }

                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 1);
                    sincronizados++;
                }
                catch (Exception ex)
                {
                    await _localDbService.ActualizarEstadoSync(entregaLocal.IdLocal, 2, errorMessage: ex.Message);
                }
            }

            return sincronizados;
        }
    }
}