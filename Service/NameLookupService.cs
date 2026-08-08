using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    /// <summary>
    /// Kamus terpusat ID -> Nama untuk Customer, SalesPerson, dan Produk.
    /// HANYA exact match. Tidak ada fuzzy/prefix matching - itu berisiko
    /// menampilkan nama yang SALAH tanpa error apapun (silent bug).
    /// </summary>
    public static class NameLookupService
    {
        private static Dictionary<string, string> _customerNames = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _salesPersonNames = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _productNames = new(StringComparer.OrdinalIgnoreCase);

        public static async Task RefreshAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();

            var customers = await localDb.Table<LocalCustomer>().ToListAsync();
            _customerNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in customers)
            {
                if (!string.IsNullOrWhiteSpace(c.Id))
                    _customerNames[c.Id.Trim()] = c.Name?.Trim() ?? "";
            }

            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
            _salesPersonNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in salesPersons)
            {
                if (!string.IsNullOrWhiteSpace(s.Id))
                    _salesPersonNames[s.Id.Trim()] = s.Name?.Trim() ?? "";
            }

            var products = await localDb.Table<LocalProduct>().ToListAsync();
            _productNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in products)
            {
                if (!string.IsNullOrWhiteSpace(p.Id))
                    _productNames[p.Id.Trim()] = p.Name?.Trim() ?? "";
            }
        }

        public static string GetCustomerName(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "-";
            return _customerNames.TryGetValue(id.Trim(), out var name) ? name : "(customer tidak ditemukan)";
        }

        public static string GetSalesPersonName(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "-";
            return _salesPersonNames.TryGetValue(id.Trim(), out var name) ? name : "(sales tidak ditemukan)";
        }

        public static string GetProductName(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "-";
            return _productNames.TryGetValue(id.Trim(), out var name) ? name : "(produk tidak ditemukan)";
        }
    }
}
