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

        // UN SOLO método ActualizarEstadoSync (con 4 parámetros, todos opcionales)
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

        // Obtener todas las entregas (para el administrador local)
        public async Task<List<EntregaLocal>> GetAllEntregas()
        {
            return await _database.Table<EntregaLocal>()
                .OrderByDescending(e => e.FechaHoraRegistro)
                .ToListAsync();
        }

        // Eliminar una entrega (opcional, para el administrador)
        public async Task<int> EliminarEntrega(int idLocal)
        {
            return await _database.DeleteAsync<EntregaLocal>(idLocal);
        }

        // Actualizar una entrega existente
        public async Task<int> ActualizarEntrega(EntregaLocal entrega)
        {
            // Verificar que la entrega existe
            var existe = await _database.FindAsync<EntregaLocal>(entrega.IdLocal);
            if (existe == null)
                return 0;

            // Actualizar SOLO los campos permitidos
            existe.ClienteNombre = entrega.ClienteNombre;
            existe.CantidadGarrafones = entrega.CantidadGarrafones;
            existe.Direccion = entrega.Direccion;
            existe.UpdatedAt = DateTime.Now;

            // Si ya estaba sincronizada, marcar como pendiente nuevamente
            if (existe.EstadoSync == 1)
            {
                existe.EstadoSync = 0; // Pendiente de re-sincronizar
            }

            return await _database.UpdateAsync(existe);
        }
    }
}