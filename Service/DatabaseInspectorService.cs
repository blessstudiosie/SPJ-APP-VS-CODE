using SPJ_APP.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SPJ_APP.Service
{
    public class DatabaseInspectorService
    {
        public Dictionary<string, Type> InspectableTables { get; } = new()
        {
            { "Produk", typeof(LocalProduct) },
            { "Customer", typeof(LocalCustomer) },
            { "Sales Person", typeof(LocalSalesPerson) },
            { "Nota", typeof(LocalSale) },
            { "Item Nota", typeof(LocalSalesDetail) },
            { "Pembayaran", typeof(LocalPayment) },
            { "Pengiriman", typeof(LocalDelivery) },
            { "Item Pengiriman", typeof(LocalDeliveryDetail) },
            { "Purchase Order", typeof(LocalPurchaseOrder) },
            { "Activity Log", typeof(LocalActivityLog) }
        };

        public async Task<List<object>> GetAllRowsAsync(Type tableType)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var tableQuery = localDb.Table(tableType);
            var results = await tableQuery.ToListAsync();
            return results;
        }

        public async Task UpdateRowAsync(object item)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            await localDb.UpdateAsync(item);
        }

        public async Task DeleteRowsAsync(IEnumerable<object> items)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            await localDb.RunInTransactionAsync(conn =>
            {
                foreach (var item in items)
                {
                    conn.Delete(item);
                }
            });
        }
    }
}