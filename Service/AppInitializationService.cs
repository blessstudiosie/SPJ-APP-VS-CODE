using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class AppInitializationService
    {
        public static event EventHandler<string> InitializationProgressChanged;

        public static async Task InitializeAppAsync()
        {
            try
            {
                OnProgress("Membuat atau memverifikasi database lokal...");
                var conn = await LocalDatabaseService.GetConnection();
                OnProgress("Database siap.");

                OnProgress("Memeriksa data awal...");
                var salesPersons = await conn.Table<LocalSalesPerson>().ToListAsync();
                if (salesPersons.Count == 0)
                {
                    var defaultSales = new LocalSalesPerson
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Sales Kantor / Direct",
                        Phone = "-",
                        Role = "Admin Sales",
                        TargetOmset = 0,
                        IsSynced = false,
                        UpdatedAt = DateTime.Now
                    };
                    await conn.InsertAsync(defaultSales);
                    OnProgress("Membuat sales person default...");
                }

                var customers = await conn.Table<LocalCustomer>().ToListAsync();
                if (customers.Count == 0)
                {
                    var defaultCustomer = new LocalCustomer
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = "Pelanggan Umum",
                        OwnerName = "-",
                        Phone = "-",
                        Address = "Dalam Kota",
                        JalurPengiriman = "Dalam Kota",
                        LimitPiutang = 0,
                        IsSynced = false
                    };
                    await conn.InsertAsync(defaultCustomer);
                    OnProgress("Membuat customer default...");
                }
                OnProgress("Data awal siap.");

                OnProgress("Memulai sinkronisasi data awal dengan server...");
                // On first launch, we prioritize getting all data from the server.
                // Conflicts are ignored as there's no local data to conflict with yet.
                var (summary, _) = await SyncService.SyncAllAsync();
                OnProgress($"Sinkronisasi awal selesai. {summary.ProductsPulled} produk diterima.");
                
                OnProgress("Inisialisasi selesai.");
            }
            catch (Exception ex)
            {
                OnProgress($"Error saat inisialisasi: {ex.Message}");
                // Rethrow or handle as needed. For now, we just report.
                // In a real scenario, you might want to prevent the app from starting.
            }
        }

        private static void OnProgress(string message)
        {
            InitializationProgressChanged?.Invoke(null, message);
        }
    }
}
