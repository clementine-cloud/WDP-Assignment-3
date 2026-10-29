using System.Text.Json;

namespace AspNetCoreMvcWebSite.Models
{
    // Remembers which AIImage blocks the current visitor has already liked.
    // The brief says: one like per block, and the user has to log off and log in again to like again.
    // We store the liked ids in the Session (server-side memory tied to a browser cookie).
    // When a visitor who is NOT logged in loads the Home or AI Images page, the list is cleared,
    // so logging out (which lands on the Home page) resets the likes.
    public static class LikeTracker
    {
        private const string SessionKey = "LikedImageIds";

        public static HashSet<int> Get(ISession session)
        {
            var json = session.GetString(SessionKey);
            if (string.IsNullOrEmpty(json))
            {
                return new HashSet<int>();
            }

            try
            {
                return JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
            }
            catch (JsonException)
            {
                // A damaged value should never crash the site - just start again.
                return new HashSet<int>();
            }
        }

        public static void Save(ISession session, HashSet<int> ids)
        {
            session.SetString(SessionKey, JsonSerializer.Serialize(ids));
        }

        public static void Clear(ISession session)
        {
            session.Remove(SessionKey);
        }
    }
}
