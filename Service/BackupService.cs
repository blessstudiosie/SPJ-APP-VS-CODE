using System.IO;
using System.Text.Json;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class BackupService
    {
        public static async Task BackupLocalDatabaseAsync(string destinationPath)
        {
            // First, ensure the connection is closed to release file locks.
            // This is a simplification; a real app might need proper connection management.
            // For now, we rely on GetConnection to return the same instance and assume it's okay.
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sourceDb = Path.Combine(appData, "SPJ APP", "spj_local.db3");

            if (!File.Exists(sourceDb))
            {
                throw new FileNotFoundException("Database lokal tidak ditemukan atau belum dibuat.");
            }

            File.Copy(sourceDb, destinationPath, overwrite: true);
            await Task.CompletedTask;
        }

        public static async Task ExportLocalDataAsJsonAsync(string destinationPath)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var backupData = new
            {
                ExportedAt = DateTime.Now,
                Products = await localDb.Table<LocalProduct>().ToListAsync(),
                Sales = await localDb.Table<LocalSale>().ToListAsync(),
                SalesDetails = await localDb.Table<LocalSalesDetail>().ToListAsync(),
                Customers = await localDb.Table<LocalCustomer>().ToListAsync(),
                SalesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync(),
                Payments = await localDb.Table<LocalPayment>().ToListAsync(),
                Deliveries = await localDb.Table<LocalDelivery>().ToListAsync(),
                DeliveryDetails = await localDb.Table<LocalDeliveryDetail>().ToListAsync()
            };

            string json = JsonSerializer.Serialize(backupData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(destinationPath, json);
        }
    }
}
