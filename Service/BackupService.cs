using System.IO;
using System.Text.Json;
using SPJ_APP.Model;
using System.Collections.Generic;

namespace SPJ_APP.Service
{
    // Helper class to structure the backup data for deserialization
    public class BackupData
    {
        public DateTime ExportedAt { get; set; }
        public List<LocalProduct> Products { get; set; } = new();
        public List<LocalSale> Sales { get; set; } = new();
        public List<LocalSalesDetail> SalesDetails { get; set; } = new();
        public List<LocalCustomer> Customers { get; set; } = new();
        public List<LocalSalesPerson> SalesPersons { get; set; } = new();
        public List<LocalPayment> Payments { get; set; } = new();
        public List<LocalDelivery> Deliveries { get; set; } = new();
        public List<LocalDeliveryDetail> DeliveryDetails { get; set; } = new();
    }


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

        public static async Task RestoreDataFromJsonAsync(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException("File backup JSON tidak ditemukan.", jsonFilePath);
            }

            string json = await File.ReadAllTextAsync(jsonFilePath);
            var backupData = JsonSerializer.Deserialize<BackupData>(json);

            if (backupData == null)
            {
                throw new JsonException("Gagal membaca data dari file JSON atau format tidak valid.");
            }

            var localDb = await LocalDatabaseService.GetConnection();

            await localDb.RunInTransactionAsync(db =>
            {
                // Clear existing data. Order can matter if there are foreign key constraints,
                // but for this simple restore, we clear the main ones.
                db.DeleteAll<LocalDeliveryDetail>();
                db.DeleteAll<LocalDelivery>();
                db.DeleteAll<LocalPayment>();
                db.DeleteAll<LocalSalesPerson>();
                db.DeleteAll<LocalCustomer>();
                db.DeleteAll<LocalSalesDetail>();
                db.DeleteAll<LocalSale>();
                db.DeleteAll<LocalProduct>();

                // Insert new data
                if (backupData.Products?.Count > 0) db.InsertAll(backupData.Products);
                if (backupData.Sales?.Count > 0) db.InsertAll(backupData.Sales);
                if (backupData.SalesDetails?.Count > 0) db.InsertAll(backupData.SalesDetails);
                if (backupData.Customers?.Count > 0) db.InsertAll(backupData.Customers);
                if (backupData.SalesPersons?.Count > 0) db.InsertAll(backupData.SalesPersons);
                if (backupData.Payments?.Count > 0) db.InsertAll(backupData.Payments);
                if (backupData.Deliveries?.Count > 0) db.InsertAll(backupData.Deliveries);
                if (backupData.DeliveryDetails?.Count > 0) db.InsertAll(backupData.DeliveryDetails);
            });
        }
    }
}
