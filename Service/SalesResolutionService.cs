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
            string trimmed = value.Trim();
            if (Guid.TryParse(trimmed, out _)) return true;
            if (trimmed.Length >= 32 && trimmed.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == '-'))
                return true;
            return false;
        }

        public static async Task<List<FullSaleDisplayItem>> GetFullSalesAsync(List<LocalSale>? salesInput = null)
        {
            var localDb = await LocalDatabaseService.GetConnection();

            var salesList = salesInput ?? await localDb.Table<LocalSale>().ToListAsync();
            if (!salesList.Any()) return new List<FullSaleDisplayItem>();

            var customers = await localDb.Table<LocalCustomer>().ToListAsync();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
            var products = await localDb.Table<LocalProduct>().ToListAsync();
            var allDetails = await localDb.Table<LocalSalesDetail>().ToListAsync();

            if (!customers.Any() || !salesPersons.Any() || !products.Any())
            {
                try
                {
                    await SyncService.SyncCustomersAsync();
                    await SyncService.SyncSalesPersonsAsync();
                    await SyncService.SyncProductsAsync();

                    customers = await localDb.Table<LocalCustomer>().ToListAsync();
                    salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
                    products = await localDb.Table<LocalProduct>().ToListAsync();
                }
                catch { }
            }

            var custById = BuildLookupDictionary(customers.Select(c => (c.Id ?? "", c.Name ?? "")));
            var custByName = customers.Where(c => !string.IsNullOrWhiteSpace(c.Name))
                                      .ToDictionary(c => c.Name!.Trim(), c => c.Name!.Trim(), StringComparer.OrdinalIgnoreCase);

            var spById = BuildLookupDictionary(salesPersons.Select(sp => (sp.Id ?? "", sp.Name ?? "")));
            var spByName = salesPersons.Where(sp => !string.IsNullOrWhiteSpace(sp.Name))
                                       .ToDictionary(sp => sp.Name!.Trim(), sp => sp.Name!.Trim(), StringComparer.OrdinalIgnoreCase);

            var prodById = BuildLookupDictionary(products.Select(p => (p.Id ?? "", p.Name ?? "")));
            var prodByName = products.Where(p => !string.IsNullOrWhiteSpace(p.Name))
                                     .ToDictionary(p => p.Name!.Trim(), p => p.Name!.Trim(), StringComparer.OrdinalIgnoreCase);

            var detailsBySaleId = allDetails.GroupBy(d => d.SaleId)
                                            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new List<FullSaleDisplayItem>();

            foreach (var s in salesList)
            {
                string custName = ResolveCustomerName(s.CustomerId, custById, custByName);
                string salesName = ResolveSalesPersonName(s.SalesPersonId, spById, spByName);

                var detailsForSale = new List<LocalSalesDetail>();
                if (detailsBySaleId.TryGetValue(s.Id, out var d1)) detailsForSale.AddRange(d1);
                if (!string.IsNullOrWhiteSpace(s.Nota) && detailsBySaleId.TryGetValue(s.Nota, out var d2))
                {
                    foreach (var item in d2)
                    {
                        if (!detailsForSale.Any(x => x.Id == item.Id)) detailsForSale.Add(item);
                    }
                }

                var detailDisplayItems = detailsForSale.Select(d => new FullSaleDetailDisplayItem
                {
                    Id = d.Id,
                    SaleId = d.SaleId,
                    ProductId = d.ProductId,
                    ProductName = ResolveProductName(d.ProductId, prodById, prodByName),
                    Qty = d.Qty,
                    Price = d.Price,
                    PriceCategory = d.PriceCategory,
                    Subtotal = d.Subtotal,
                    DetailOriginal = d
                }).ToList();

                result.Add(new FullSaleDisplayItem
                {
                    Id = s.Id,
                    Nota = s.Nota,
                    CustomerId = s.CustomerId ?? "",
                    CustomerName = custName,
                    SalesPersonId = s.SalesPersonId ?? "",
                    SalesPersonName = salesName,
                    OrderDate = s.OrderDate,
                    DeliveryDate = s.DeliveryDate,
                    Status = s.Status,
                    Total = s.Total,
                    Paid = s.Paid,
                    Remaining = s.Remaining,
                    Description = s.Description,
                    Details = detailDisplayItems,
                    HeaderOriginal = s
                });
            }

            return result;
        }

        public static async Task<FullSaleDisplayItem?> GetFullSaleByIdAsync(string saleIdOrNota)
        {
            if (string.IsNullOrWhiteSpace(saleIdOrNota)) return null;

            var localDb = await LocalDatabaseService.GetConnection();
            string key = saleIdOrNota.Trim();
            var sale = await localDb.Table<LocalSale>()
                                   .Where(s => s.Id == key || s.Nota == key)
                                   .FirstOrDefaultAsync();

            if (sale == null) return null;

            var fullList = await GetFullSalesAsync(new List<LocalSale> { sale });
            return fullList.FirstOrDefault();
        }

        public static async Task<List<SaleDisplayItem>> ResolveSaleDisplayItemsAsync(IEnumerable<LocalSale> sales)
        {
            var fullSales = await GetFullSalesAsync(sales?.ToList());
            return fullSales.Select(f => new SaleDisplayItem
            {
                Id = f.Id,
                Nota = f.Nota,
                CustomerName = f.CustomerName,
                SalesPersonName = f.SalesPersonName,
                OrderDate = f.OrderDate,
                DeliveryDate = f.DeliveryDate,
                Status = f.Status,
                Total = f.Total,
                Paid = f.Paid,
                Remaining = f.Remaining,
                Original = f.HeaderOriginal
            }).ToList();
        }

        private static Dictionary<string, string> BuildLookupDictionary(IEnumerable<(string Id, string Name)> items)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (rawId, rawName) in items)
            {
                if (string.IsNullOrWhiteSpace(rawId) || string.IsNullOrWhiteSpace(rawName)) continue;

                string idKey = rawId.Trim();
                string nameValue = rawName.Trim();

                dict[idKey] = nameValue;

                if (Guid.TryParse(idKey, out var g))
                {
                    string gD = g.ToString("D");
                    string gN = g.ToString("N");

                    dict[gD] = nameValue;
                    dict[gN] = nameValue;

                    if (gD.Length >= 8) dict[gD.Substring(0, 8)] = nameValue;
                    if (gN.Length >= 8) dict[gN.Substring(0, 8)] = nameValue;
                }
                else if (idKey.Length >= 8)
                {
                    dict[idKey.Substring(0, 8)] = nameValue;
                }
            }

            return dict;
        }

        public static string ResolveProductName(
            string? productIdOrName,
            Dictionary<string, string> prodById,
            Dictionary<string, string> prodByName)
        {
            if (string.IsNullOrWhiteSpace(productIdOrName))
            {
                return "(produk dihapus)";
            }

            string key = productIdOrName.Trim();

            // 1. Direct match by ID
            if (prodById.TryGetValue(key, out var nameById) && !string.IsNullOrWhiteSpace(nameById))
            {
                return nameById;
            }

            // 2. Direct match by Name
            if (prodByName.TryGetValue(key, out var nameByName) && !string.IsNullOrWhiteSpace(nameByName))
            {
                return nameByName;
            }

            // 3. Match normalized GUID or prefix
            if (Guid.TryParse(key, out var parsedGuid))
            {
                string normD = parsedGuid.ToString("D");
                if (prodById.TryGetValue(normD, out var nameD) && !string.IsNullOrWhiteSpace(nameD)) return nameD;

                string normN = parsedGuid.ToString("N");
                if (prodById.TryGetValue(normN, out var nameN) && !string.IsNullOrWhiteSpace(nameN)) return nameN;

                if (normD.Length >= 8 && prodById.TryGetValue(normD.Substring(0, 8), out var nameP8) && !string.IsNullOrWhiteSpace(nameP8))
                    return nameP8;
            }
            else if (key.Length >= 8 && prodById.TryGetValue(key.Substring(0, 8), out var nameK8) && !string.IsNullOrWhiteSpace(nameK8))
            {
                return nameK8;
            }

            // 4. Prefix match loop
            var prefixMatch = prodById.FirstOrDefault(kv => kv.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrWhiteSpace(prefixMatch)) return prefixMatch;

            // 5. If key itself is a human text name, return key directly
            if (!IsGuidString(key))
            {
                return key;
            }

            return "(produk dihapus)";
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

            if (custById.TryGetValue(key, out var nameById) && !string.IsNullOrWhiteSpace(nameById)) return nameById;
            if (custByName.TryGetValue(key, out var nameByName) && !string.IsNullOrWhiteSpace(nameByName)) return nameByName;

            if (Guid.TryParse(key, out var parsedGuid))
            {
                string normD = parsedGuid.ToString("D");
                if (custById.TryGetValue(normD, out var nameD) && !string.IsNullOrWhiteSpace(nameD)) return nameD;

                string normN = parsedGuid.ToString("N");
                if (custById.TryGetValue(normN, out var nameN) && !string.IsNullOrWhiteSpace(nameN)) return nameN;

                if (normD.Length >= 8 && custById.TryGetValue(normD.Substring(0, 8), out var nameP8) && !string.IsNullOrWhiteSpace(nameP8))
                    return nameP8;
            }
            else if (key.Length >= 8 && custById.TryGetValue(key.Substring(0, 8), out var nameK8) && !string.IsNullOrWhiteSpace(nameK8))
            {
                return nameK8;
            }

            var prefixMatch = custById.FirstOrDefault(kv => kv.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrWhiteSpace(prefixMatch)) return prefixMatch;

            if (!IsGuidString(key))
            {
                return key;
            }

            return "Pelanggan Umum";
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

            if (spById.TryGetValue(key, out var nameById) && !string.IsNullOrWhiteSpace(nameById)) return nameById;
            if (spByName.TryGetValue(key, out var nameByName) && !string.IsNullOrWhiteSpace(nameByName)) return nameByName;

            if (Guid.TryParse(key, out var parsedGuid))
            {
                string normD = parsedGuid.ToString("D");
                if (spById.TryGetValue(normD, out var nameD) && !string.IsNullOrWhiteSpace(nameD)) return nameD;

                string normN = parsedGuid.ToString("N");
                if (spById.TryGetValue(normN, out var nameN) && !string.IsNullOrWhiteSpace(nameN)) return nameN;

                if (normD.Length >= 8 && spById.TryGetValue(normD.Substring(0, 8), out var nameP8) && !string.IsNullOrWhiteSpace(nameP8))
                    return nameP8;
            }
            else if (key.Length >= 8 && spById.TryGetValue(key.Substring(0, 8), out var nameK8) && !string.IsNullOrWhiteSpace(nameK8))
            {
                return nameK8;
            }

            var prefixMatch = spById.FirstOrDefault(kv => kv.Key.StartsWith(key, StringComparison.OrdinalIgnoreCase) || key.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase)).Value;
            if (!string.IsNullOrWhiteSpace(prefixMatch)) return prefixMatch;

            if (!IsGuidString(key))
            {
                return key;
            }

            return "Sales Umum";
        }
    }
}
