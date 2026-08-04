using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class SalesQueryService
    {
        private static readonly Dictionary<string, int> StatusOrder = new()
        {
            { "SO", 0 },
            { "ON PROSES", 1 },
            { "DALAM PENGIRIMAN", 2 },
            { "TEMPO", 3 },
            { "DONE", 4 }
        };

        public const int PageSize = 20;

        public static async Task<(List<SaleDisplayItem> Items, int TotalCount)> GetPagedSalesAsync(int pageNumber, DateTime? startDate = null, DateTime? endDate = null)
        {
            var localDb = await LocalDatabaseService.GetConnection();

            var allSales = await localDb.Table<LocalSale>().ToListAsync();
            var customers = await localDb.Table<LocalCustomer>().ToListAsync();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

            if (startDate.HasValue)
            {
                allSales = allSales.Where(s => s.OrderDate.Date >= startDate.Value.Date).ToList();
            }
            if (endDate.HasValue)
            {
                allSales = allSales.Where(s => s.OrderDate.Date <= endDate.Value.Date).ToList();
            }

            var sorted = allSales
                .OrderBy(s => StatusOrder.TryGetValue(s.Status, out int order) ? order : 99)
                .ThenByDescending(s => s.OrderDate)
                .ToList();

            int totalCount = sorted.Count;

            var pageItems = sorted
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .Select(s => new SaleDisplayItem
                {
                    Id = s.Id,
                    Nota = s.Nota,
                    CustomerName = customers.FirstOrDefault(c => c.Id == s.CustomerId)?.Name ?? "-",
                    SalesPersonName = salesPersons.FirstOrDefault(sp => sp.Id == s.SalesPersonId)?.Name ?? "-",
                    OrderDate = s.OrderDate,
                    DeliveryDate = s.DeliveryDate,
                    Status = s.Status,
                    Total = s.Total,
                    Paid = s.Paid,
                    Remaining = s.Remaining,
                    Original = s
                })
                .ToList();

            return (pageItems, totalCount);
        }
		
		public static async Task<List<SaleDisplayItem>> GetSalesByStatusAsync(string status)
{
    var localDb = await LocalDatabaseService.GetConnection();

    var sales = await localDb.Table<LocalSale>().Where(s => s.Status == status).ToListAsync();
    var customers = await localDb.Table<LocalCustomer>().ToListAsync();
    var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

    return sales
        .OrderByDescending(s => s.OrderDate)
        .Select(s => new SaleDisplayItem
        {
            Id = s.Id,
            Nota = s.Nota,
            CustomerName = customers.FirstOrDefault(c => c.Id == s.CustomerId)?.Name ?? "-",
            SalesPersonName = salesPersons.FirstOrDefault(sp => sp.Id == s.SalesPersonId)?.Name ?? "-",
            OrderDate = s.OrderDate,
            DeliveryDate = s.DeliveryDate,
            Status = s.Status,
            Total = s.Total,
            Paid = s.Paid,
            Remaining = s.Remaining,
            Original = s
        })
        .ToList();
}
    }
}