using SPJ_APP.Model;
using System;
using System.Threading.Tasks;

namespace SPJ_APP.Service
{
    public static class ActivityLogService
    {
        public static async Task LogAsync(string action, string? details = null)
        {
            try
            {
                var currentUser = CurrentUserService.LoggedInUser;
                if (currentUser == null)
                {
                    // If no user is logged in, we can't log the activity.
                    // This might happen for early startup tasks before login.
                    // For now, we'll just ignore these.
                    return;
                }

                var logEntry = new LocalActivityLog
                {
                    Id = Guid.NewGuid(),
                    UserName = currentUser.Name,
                    Action = action,
                    Details = details,
                    CreatedAt = DateTime.UtcNow, // Use UTC for consistency
                    IsSynced = false
                };

                var conn = await LocalDatabaseService.GetConnection();
                await conn.InsertAsync(logEntry);
            }
            catch (Exception ex)
            {
                // Failed to write log. For now, we fail silently to not interrupt app flow.
                // In a real-world scenario, you might want to log this failure to a local file.
                Console.WriteLine($"Failed to write to activity log: {ex.Message}");
            }
        }
    }
}
