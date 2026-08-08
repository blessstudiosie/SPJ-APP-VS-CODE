using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class SalesResolutionService
    {
        public static async Task<List<FullSaleDisplayItem>> GetFullSalesAsync(List<LocalSale>? salesInput = null)
        {
            var localDb = await LocalDatabaseService.GetConnection();

            var salesList = salesInput ?? await localDb.Table<LocalSale>().ToListAsync();
            if (!salesList.Any()) return new List<FullSaleDisplayItem>();

            var allDetails = await localDb.Table<LocalSalesDetail>().ToListAsync();

            var detailsBySaleId = allDetails.GroupBy(d => d.SaleId)
                                            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            var result = new List<FullSaleDisplayItem>();

            foreach (var s in salesList)
            {
                string custName = NameLookupService.GetCustomerName(s.CustomerId);
                string salesName = NameLookupService.GetSalesPersonName(s.SalesPersonId);

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
                    ProductName = NameLookupService.GetProductName(d.ProductId),
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

        public static string ResolveProductName(
            string? productIdOrName,
            Dictionary<string, string> prodById,
            Dictionary<string, string> prodByName)
        {
            return NameLookupService.GetProductName(productIdOrName);
        }

        public static string ResolveCustomerName(
            string? customerIdOrName,
            Dictionary<string, string> custById,
            Dictionary<string, string> custByName)
        {
            return NameLookupService.GetCustomerName(customerIdOrName);
        }

        public static string ResolveSalesPersonName(
            string? salesPersonIdOrName,
            Dictionary<string, string> spById,
            Dictionary<string, string> spByName)
        {
            return NameLookupService.GetSalesPersonName(salesPersonIdOrName);
        }
    }
}
