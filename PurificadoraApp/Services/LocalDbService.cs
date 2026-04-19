using SQLite;
using PurificadoraApp.Models;

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
        public async Task<int> ActualizarEstadoSync(int idLocal, int estado, string? idRemoto = null)
        {
            var entrega = await _database.FindAsync<EntregaLocal>(idLocal);
            if (entrega != null)
            {
                entrega.EstadoSync = estado;
                if (!string.IsNullOrEmpty(idRemoto))
                    entrega.IdRemoto = idRemoto;
                entrega.FechaSincronizacion = DateTime.Now;
                return await _database.UpdateAsync(entrega);
            }
            return 0;
        }

        // Obtener todas las entregas (para el administrador local)
        public async Task<List<EntregaLocal>> GetAllEntregas()
        {
            return await _database.Table<EntregaLocal>().OrderByDescending(e => e.FechaHoraRegistro).ToListAsync();
        }
    }
}