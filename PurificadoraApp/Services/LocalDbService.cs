using SQLite;
using PurificadoraApp.Models;
using System.Diagnostics;

namespace PurificadoraApp.Services
{
    public class LocalDbService
    {
        private SQLiteAsyncConnection _database;

        public LocalDbService()
        {
            InitializeDatabase();
        }

        private async Task InitializeDatabase()
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "purificadora.db3");
            _database = new SQLiteAsyncConnection(dbPath);
            await _database.CreateTableAsync<EntregaLocal>();
        }

        // Guardar entrega localmente
        public async Task<int> GuardarEntrega(EntregaLocal entrega)
        {
            return await _database.InsertAsync(entrega);
        }

        // Obtener entregas pendientes de sincronizar
        public async Task<List<EntregaLocal>> GetEntregasPendientes()
        {
            return await _database.Table<EntregaLocal>()
                .Where(e => e.EstadoSync == 0)
                .ToListAsync();
        }

        // Actualizar estado de sincronización
        public async Task<int> ActualizarEstadoSync(int idLocal, int estado, string? idRemoto = null, string? errorMessage = null)
        {
            var entrega = await _database.FindAsync<EntregaLocal>(idLocal);
            if (entrega != null)
            {
                entrega.EstadoSync = estado;
                if (!string.IsNullOrEmpty(idRemoto))
                    entrega.IdRemoto = idRemoto;
                if (!string.IsNullOrEmpty(errorMessage))
                    entrega.ErrorMessage = errorMessage;
                entrega.FechaSincronizacion = DateTime.Now;
                return await _database.UpdateAsync(entrega);
            }
            return 0;
        }

        // Obtener todas las entregas
        public async Task<List<EntregaLocal>> GetAllEntregas()
        {
            var entregas = await _database.Table<EntregaLocal>()
                .OrderByDescending(e => e.FechaHoraRegistro)
                .ToListAsync();

            Debug.WriteLine($"GetAllEntregas: {entregas.Count} entregas encontradas");
            foreach (var e in entregas.Take(5))
            {
                Debug.WriteLine($"  - ID: {e.IdLocal}, Cliente: {e.ClienteNombre}, Cantidad: {e.CantidadGarrafones}");
            }

            return entregas;
        }

        // Eliminar una entrega
        public async Task<int> EliminarEntrega(int idLocal)
        {
            return await _database.DeleteAsync<EntregaLocal>(idLocal);
        }

        // Actualizar una entrega existente
        public async Task<int> ActualizarEntrega(EntregaLocal entrega)
        {
            try
            {
                var existe = await _database.FindAsync<EntregaLocal>(entrega.IdLocal);
                if (existe == null)
                {
                    Debug.WriteLine($"Entrega con IdLocal {entrega.IdLocal} no encontrada");
                    return 0;
                }

                Debug.WriteLine($"Actualizando entrega ID: {entrega.IdLocal}");
                Debug.WriteLine($"  ClienteNombre: {entrega.ClienteNombre}");
                Debug.WriteLine($"  Cantidad: {entrega.CantidadGarrafones}");
                Debug.WriteLine($"  Direccion: {entrega.Direccion}");

                existe.ClienteId = entrega.ClienteId;
                existe.ClienteNombre = entrega.ClienteNombre;
                existe.CantidadGarrafones = entrega.CantidadGarrafones;
                existe.Direccion = entrega.Direccion;
                existe.UpdatedAt = DateTime.Now;
                existe.EstadoSync = 0; // Marcar como pendiente

                var resultado = await _database.UpdateAsync(existe);
                Debug.WriteLine($"Resultado update: {resultado}");

                return resultado;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en ActualizarEntrega: {ex.Message}");
                return 0;
            }
        }

        // Obtener todos los clientes (para LicenciaService)
        public async Task<List<Cliente>> GetAllClientes()
        {
            try
            {
                var clientes = await _database.Table<Cliente>().ToListAsync();
                return clientes;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en GetAllClientes: {ex.Message}");
                return new List<Cliente>();
            }
        }
    }
}