using System;
using System.Threading.Tasks;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class ActivityLogService
    {
        public static async Task LogAsync(string action, string? details = null)
        {
            var user = CurrentUserService.LoggedInUser;
            if (user is null) return; // Don't log if no user is logged in.

            var logEntry = new LocalActivityLog
            {
                Id = Guid.NewGuid(),
                UserName = user.Name,
                Action = action,
                Details = details,
                CreatedAt = DateTime.UtcNow,
                IsSynced = false
            };

            var localDb = await LocalDatabaseService.GetConnection();
            await localDb.InsertAsync(logEntry);
        }
    }
}