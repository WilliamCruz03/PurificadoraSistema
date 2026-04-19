using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using System.Text.Json;

namespace PurificadoraApp.Services
{
    public class CustomSessionHandler : IGotrueSessionPersistence<Session>
    {
        private const string SessionKey = "SUPABASE_SESSION";

        public void SaveSession(Session session)
        {
            var json = JsonSerializer.Serialize(session);
            Preferences.Set(SessionKey, json);
        }

        public void DestroySession()
        {
            Preferences.Remove(SessionKey);
        }

        public Session? LoadSession()
        {
            var json = Preferences.Get(SessionKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return null;

            var session = JsonSerializer.Deserialize<Session>(json);

            // Verificar si la sesión ha expirado
            if (session?.ExpiresAt() <= DateTime.Now)
                return null;

            return session;
        }
    }
}