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

        private async Task<List<object>> _GetAllRowsInternalAsync<T>() where T : new()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            return (await localDb.Table<T>().ToListAsync()).Cast<object>().ToList();
        }

        public async Task<List<object>> GetAllRowsAsync(Type tableType)
        {
            var method = GetType().GetMethod(nameof(_GetAllRowsInternalAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException($"Helper method '{nameof(_GetAllRowsInternalAsync)}' not found.");
            }

            var genericMethod = method.MakeGenericMethod(tableType);
            var task = (Task<List<object>>)genericMethod.Invoke(this, null)!;
            return await task;
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