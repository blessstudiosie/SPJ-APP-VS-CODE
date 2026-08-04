using System.Net.NetworkInformation;

namespace SPJ_APP.Service
{
    public static class ConnectivityService
    {
        public static async Task<bool> IsOnlineAsync()
        {
            try
            {
                using var ping = new Ping();
                var result = await ping.SendPingAsync("8.8.8.8", 3000);
                return result.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}