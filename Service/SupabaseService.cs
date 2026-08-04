using Supabase;

namespace SPJ_APP.Service
{
    public class SupabaseService
    {
        private static Supabase.Client? _client;

        public static async Task<Supabase.Client> GetClient()
        {
            if (_client == null)
            {
                var options = new SupabaseOptions
                {
                    AutoConnectRealtime = true
                };

                _client = new Supabase.Client(SupabaseConfig.Url, SupabaseConfig.AnonKey, options);
                await _client.InitializeAsync();
            }

            return _client;
        }
    }
}