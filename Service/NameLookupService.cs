using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    /// <summary>
    /// Kamus terpusat ID -> Nama untuk Customer, SalesPerson, dan Produk.
    /// Dipakai SEMUA halaman yang menampilkan nama dari relasi ID, supaya konsisten
    /// dan tidak ada halaman yang lupa melakukan lookup (penyebab bug ID tampil mentah).
    /// </summary>
    public static class NameLookupService
    {
        private static Dictionary<string, string> _customerNames = new();
        private static Dictionary<string, string> _salesPersonNames = new();
        private static Dictionary<string, string> _productNames = new();

        public static async Task RefreshAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();

            var customers = await localDb.Table<LocalCustomer>().ToListAsync();
            var custDict = new Dictionary<string, string>();
            foreach (var c in customers)
            {
                if (!string.IsNullOrEmpty(c.Id))
                {
                    custDict[c.Id] = c.Name;
                }
            }
            _customerNames = custDict;

            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
            var spDict = new Dictionary<string, string>();
            foreach (var s in salesPersons)
            {
                if (!string.IsNullOrEmpty(s.Id))
                {
                    spDict[s.Id] = s.Name;
                }
            }
            _salesPersonNames = spDict;

            var products = await localDb.Table<LocalProduct>().ToListAsync();
            var prodDict = new Dictionary<string, string>();
            foreach (var p in products)
            {
                if (!string.IsNullOrEmpty(p.Id))
                {
                    prodDict[p.Id] = p.Name;
                }
            }
            _productNames = prodDict;
        }

        public static string GetCustomerName(string? id) =>
            string.IsNullOrEmpty(id) ? "-" : _customerNames.GetValueOrDefault(id, "(customer tidak ditemukan)");

        public static string GetSalesPersonName(string? id) =>
            string.IsNullOrEmpty(id) ? "-" : _salesPersonNames.GetValueOrDefault(id, "(sales tidak ditemukan)");

        public static string GetProductName(string? id) =>
            string.IsNullOrEmpty(id) ? "-" : _productNames.GetValueOrDefault(id, "(produk tidak ditemukan)");
    }
}
