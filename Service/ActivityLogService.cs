using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class ActivityLogService
    {
        public static async Task LogAsync(string action, string? details = null)
        {
            var localDb = await LocalDatabaseService.GetConnection();

            var log = new LocalActivityLog
            {
                Id = Guid.NewGuid().ToString(),
                UserName = CurrentUserService.CurrentUserName,
                Action = action,
                Details = details,
                CreatedAt = DateTime.Now,
                IsSynced = false
            };

            await localDb.InsertAsync(log);
        }
    }
}