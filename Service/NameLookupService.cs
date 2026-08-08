using System;
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
        private static Dictionary<string, string> _customerNames = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _salesPersonNames = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _productNames = new(StringComparer.OrdinalIgnoreCase);

        public static async Task RefreshAsync()
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();

                var customers = await localDb.Table<LocalCustomer>().ToListAsync();
                var custDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in customers)
                {
                    if (!string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Name))
                    {
                        string idKey = c.Id.Trim();
                        string nameValue = c.Name.Trim();
                        custDict[idKey] = nameValue;
                        if (Guid.TryParse(idKey, out var g))
                        {
                            string gD = g.ToString("D");
                            string gN = g.ToString("N");
                            custDict[gD] = nameValue;
                            custDict[gN] = nameValue;
                            if (gD.Length >= 8) custDict[gD.Substring(0, 8)] = nameValue;
                        }
                        else if (idKey.Length >= 8)
                        {
                            custDict[idKey.Substring(0, 8)] = nameValue;
                        }
                    }
                }
                _customerNames = custDict;

                var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
                var spDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var s in salesPersons)
                {
                    if (!string.IsNullOrWhiteSpace(s.Id) && !string.IsNullOrWhiteSpace(s.Name))
                    {
                        string idKey = s.Id.Trim();
                        string nameValue = s.Name.Trim();
                        spDict[idKey] = nameValue;
                        if (Guid.TryParse(idKey, out var g))
                        {
                            string gD = g.ToString("D");
                            string gN = g.ToString("N");
                            spDict[gD] = nameValue;
                            spDict[gN] = nameValue;
                            if (gD.Length >= 8) spDict[gD.Substring(0, 8)] = nameValue;
                        }
                        else if (idKey.Length >= 8)
                        {
                            spDict[idKey.Substring(0, 8)] = nameValue;
                        }
                    }
                }
                _salesPersonNames = spDict;

                var products = await localDb.Table<LocalProduct>().ToListAsync();
                var prodDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in products)
                {
                    if (!string.IsNullOrWhiteSpace(p.Id) && !string.IsNullOrWhiteSpace(p.Name))
                    {
                        string idKey = p.Id.Trim();
                        string nameValue = p.Name.Trim();
                        prodDict[idKey] = nameValue;
                        if (Guid.TryParse(idKey, out var g))
                        {
                            string gD = g.ToString("D");
                            string gN = g.ToString("N");
                            prodDict[gD] = nameValue;
                            prodDict[gN] = nameValue;
                            if (gD.Length >= 8) prodDict[gD.Substring(0, 8)] = nameValue;
                        }
                        else if (idKey.Length >= 8)
                        {
                            prodDict[idKey.Substring(0, 8)] = nameValue;
                        }
                    }
                }
                _productNames = prodDict;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in NameLookupService.RefreshAsync: {ex.Message}");
            }
        }

        public static string GetCustomerName(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "-";
            string key = id.Trim();
            if (_customerNames.TryGetValue(key, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
            if (Guid.TryParse(key, out var g))
            {
                if (_customerNames.TryGetValue(g.ToString("D"), out var nameD)) return nameD;
                if (_customerNames.TryGetValue(g.ToString("N"), out var nameN)) return nameN;
                if (g.ToString("D").Length >= 8 && _customerNames.TryGetValue(g.ToString("D").Substring(0, 8), out var nameP8)) return nameP8;
            }
            else if (key.Length >= 8 && _customerNames.TryGetValue(key.Substring(0, 8), out var nameK8))
            {
                return nameK8;
            }

            var prefixMatch = _customerNames.FirstOrDefault(kv => kv.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrWhiteSpace(prefixMatch)) return prefixMatch;

            if (!Guid.TryParse(key, out _) && !(key.Length >= 32 && key.All(c => char.IsLetterOrDigit(c) || c == '-')))
                return key;
            return "(customer tidak ditemukan)";
        }

        public static string GetSalesPersonName(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "-";
            string key = id.Trim();
            if (_salesPersonNames.TryGetValue(key, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
            if (Guid.TryParse(key, out var g))
            {
                if (_salesPersonNames.TryGetValue(g.ToString("D"), out var nameD)) return nameD;
                if (_salesPersonNames.TryGetValue(g.ToString("N"), out var nameN)) return nameN;
                if (g.ToString("D").Length >= 8 && _salesPersonNames.TryGetValue(g.ToString("D").Substring(0, 8), out var nameP8)) return nameP8;
            }
            else if (key.Length >= 8 && _salesPersonNames.TryGetValue(key.Substring(0, 8), out var nameK8))
            {
                return nameK8;
            }

            var prefixMatch = _salesPersonNames.FirstOrDefault(kv => kv.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrWhiteSpace(prefixMatch)) return prefixMatch;

            if (!Guid.TryParse(key, out _) && !(key.Length >= 32 && key.All(c => char.IsLetterOrDigit(c) || c == '-')))
                return key;
            return "(sales tidak ditemukan)";
        }

        public static string GetProductName(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "-";
            string key = id.Trim();
            if (_productNames.TryGetValue(key, out var name) && !string.IsNullOrWhiteSpace(name)) return name;
            if (Guid.TryParse(key, out var g))
            {
                if (_productNames.TryGetValue(g.ToString("D"), out var nameD)) return nameD;
                if (_productNames.TryGetValue(g.ToString("N"), out var nameN)) return nameN;
                if (g.ToString("D").Length >= 8 && _productNames.TryGetValue(g.ToString("D").Substring(0, 8), out var nameP8)) return nameP8;
            }
            else if (key.Length >= 8 && _productNames.TryGetValue(key.Substring(0, 8), out var nameK8))
            {
                return nameK8;
            }

            var prefixMatch = _productNames.FirstOrDefault(kv => kv.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrWhiteSpace(prefixMatch)) return prefixMatch;

            if (!Guid.TryParse(key, out _) && !(key.Length >= 32 && key.All(c => char.IsLetterOrDigit(c) || c == '-')))
                return key;
            return "(produk tidak ditemukan)";
        }
    }
}
