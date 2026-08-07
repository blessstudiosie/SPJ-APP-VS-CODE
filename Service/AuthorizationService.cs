using SPJ_APP.Model;
using System.Linq;
using System.Threading.Tasks;

namespace SPJ_APP.Service
{
    public static class AuthorizationService
    {
        public static async Task<bool> AuthorizeManagerActionAsync(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                return false;
            }

            var localDb = await LocalDatabaseService.GetConnection();
            var managers = await localDb.Table<LocalSalesPerson>()
                                        .Where(p => p.Role == "Manager" || p.Role == "Owner")
                                        .ToListAsync();

            if (!managers.Any())
            {
                // If no managers/owners exist, deny the action by default for security.
                return false;
            }

            return managers.Any(m => PasswordHasherService.VerifyPassword(password, m.Password));
        }
    }
}
