using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class SalesResolutionService
    {
        private static bool IsGuidString(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            return Guid.TryParse(value.Trim(), out _);
        }

        public static async Task<List<SaleDisplayItem>> ResolveSaleDisplayItemsAsync(IEnumerable<LocalSale> sales)
        {
            var salesList = sales?.ToList() ?? new List<LocalSale>();
            if (!salesList.Any()) return new List<SaleDisplayItem>();

            var db = await LocalDatabaseService.GetConnection();
            var customers = await db.Table<LocalCustomer>().ToListAsync();
            var salesPersons = await db.Table<LocalSalesPerson>().ToListAsync();

            var custById = customers
                .Where(c => !string.IsNullOrEmpty(c.Id))
                .ToDictionary(c => c.Id.Trim(), c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            var custByName = customers
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .ToDictionary(c => c.Name.Trim(), c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            var spById = salesPersons
                .Where(sp => !string.IsNullOrEmpty(sp.Id))
                .ToDictionary(sp => sp.Id.Trim(), sp => sp.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            var spByName = salesPersons
                .Where(sp => !string.IsNullOrWhiteSpace(sp.Name))
                .ToDictionary(sp => sp.Name.Trim(), sp => sp.Name.Trim(), StringComparer.OrdinalIgnoreCase);

            return salesList.Select(s => MapToDisplayItem(s, custById, custByName, spById, spByName)).ToList();
        }

        public static async Task<SaleDisplayItem> ResolveSaleDisplayItemAsync(LocalSale sale)
        {
            var list = await ResolveSaleDisplayItemsAsync(new[] { sale });
            return list.FirstOrDefault() ?? MapToDisplayItem(sale, new(), new(), new(), new());
        }

        public static SaleDisplayItem MapToDisplayItem(
            LocalSale s,
            Dictionary<string, string> custById,
            Dictionary<string, string> custByName,
            Dictionary<string, string> spById,
            Dictionary<string, string> spByName)
        {
            string resolvedCustomerName = ResolveCustomerName(s.CustomerId, custById, custByName);
            string resolvedSalesPersonName = ResolveSalesPersonName(s.SalesPersonId, spById, spByName);

            return new SaleDisplayItem
            {
                Id = s.Id,
                Nota = s.Nota,
                CustomerName = resolvedCustomerName,
                SalesPersonName = resolvedSalesPersonName,
                OrderDate = s.OrderDate,
                DeliveryDate = s.DeliveryDate,
                Status = s.Status,
                Total = s.Total,
                Paid = s.Paid,
                Remaining = s.Remaining,
                Original = s
            };
        }

        public static string ResolveCustomerName(
            string? customerIdOrName,
            Dictionary<string, string> custById,
            Dictionary<string, string> custByName)
        {
            if (string.IsNullOrWhiteSpace(customerIdOrName))
            {
                return "Pelanggan Umum";
            }

            string key = customerIdOrName.Trim();

            if (custById.TryGetValue(key, out var nameById))
            {
                return nameById;
            }

            if (custByName.TryGetValue(key, out var nameByName))
            {
                return nameByName;
            }

            // Jika input berbentuk GUID string tetapi tidak ditemukan di master data
            if (IsGuidString(key))
            {
                return "Pelanggan Umum";
            }

            // Jika input berupa teks nama biasa
            return key;
        }

        public static string ResolveSalesPersonName(
            string? salesPersonIdOrName,
            Dictionary<string, string> spById,
            Dictionary<string, string> spByName)
        {
            if (string.IsNullOrWhiteSpace(salesPersonIdOrName))
            {
                return "Sales Umum";
            }

            string key = salesPersonIdOrName.Trim();

            if (spById.TryGetValue(key, out var nameById))
            {
                return nameById;
            }

            if (spByName.TryGetValue(key, out var nameByName))
            {
                return nameByName;
            }

            // Jika input berbentuk GUID string tetapi tidak ditemukan di master data
            if (IsGuidString(key))
            {
                return "Sales Umum";
            }

            // Jika input berupa teks nama biasa
            return key;
        }
    }
}
