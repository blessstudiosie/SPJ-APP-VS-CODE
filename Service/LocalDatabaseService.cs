using System.IO;
using SQLite;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public class LocalDatabaseService
    {
        private static SQLiteAsyncConnection? _database;

        public static async Task<SQLiteAsyncConnection> GetConnection()
        {
            if (_database == null)
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appFolder = Path.Combine(appDataPath, "SPJ APP");
                Directory.CreateDirectory(appFolder);

                string legacyDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "spj_local.db3");
                string dbPath = Path.Combine(appFolder, "spj_local.db3");

                if (!File.Exists(dbPath) && File.Exists(legacyDbPath))
                {
                    File.Copy(legacyDbPath, dbPath, overwrite: true);
                }

                _database = new SQLiteAsyncConnection(dbPath);
                await _database.CreateTableAsync<LocalProduct>();
                await _database.CreateTableAsync<LocalSale>();
                await _database.CreateTableAsync<LocalSalesDetail>();
                await _database.CreateTableAsync<LocalSalesPerson>();
                await _database.CreateTableAsync<LocalCustomer>();
                await _database.CreateTableAsync<LocalPayment>();
                await _database.CreateTableAsync<LocalDelivery>();
                await _database.CreateTableAsync<LocalDeliveryDetail>();
                await _database.CreateTableAsync<LocalGoodsReceipt>();
                await _database.CreateTableAsync<LocalGoodsReceiptDetail>();
                await _database.CreateTableAsync<LocalStockOpname>();
                await _database.CreateTableAsync<LocalStockOpnameDetail>();
                await _database.CreateTableAsync<LocalPurchaseOrder>();
                await _database.CreateTableAsync<LocalPurchaseOrderDetail>();
                await _database.CreateTableAsync<LocalActivityLog>();
                await _database.CreateTableAsync<LocalSalesOrderQueue>();
                await _database.CreateTableAsync<LocalVisitLogQueue>();

            }

            return _database;
        }
    }
}