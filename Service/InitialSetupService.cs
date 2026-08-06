using SPJ_APP.Model;
using SPJ_APP.View;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SPJ_APP.Service
{
    public class InitialSetupService
    {
        public async Task<InitialSetupAction> CheckAndRunInitialSetupIfNeededAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var supabase = await SupabaseService.GetClient();

            var localUsers = await localDb.Table<LocalSalesPerson>().ToListAsync();
            var remoteUsersResponse = await supabase.From<SalesPerson>().Get();
            
            if (localUsers.Any() || remoteUsersResponse.Models.Any())
            {
                return InitialSetupAction.NotSet; // Proceed with normal startup
            }

            // --- Kondisi Inisialisasi Awal Terdeteksi ---
            var setupWindow = new InitialSetupWindow();
            if (setupWindow.ShowDialog() != true)
            {
                return InitialSetupAction.ExitApplication;
            }

            if (setupWindow.SelectedAction == InitialSetupAction.AdminCreated)
            {
                var createAdminWindow = new CreateAdminWindow();
                if (createAdminWindow.ShowDialog() == true && createAdminWindow.NewAdmin != null)
                {
                    var newAdmin = createAdminWindow.NewAdmin;
                    await localDb.InsertAsync(newAdmin);
                    await SyncService.SyncSalesPersonsAsync();
                    CurrentUserService.SetUser(newAdmin);
                    return InitialSetupAction.AdminCreated;
                }
                else
                {
                    return InitialSetupAction.ExitApplication;
                }
            }
            else if (setupWindow.SelectedAction == InitialSetupAction.SyncAndLogin)
            {
                return InitialSetupAction.SyncAndLogin;
            }

            return InitialSetupAction.ExitApplication;
        }
    }
}
