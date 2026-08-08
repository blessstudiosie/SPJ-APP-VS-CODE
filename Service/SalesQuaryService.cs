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

            var pageSales = sorted
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var pageItems = await SalesResolutionService.ResolveSaleDisplayItemsAsync(pageSales);

            return (pageItems, totalCount);
        }
		
		public static async Task<List<SaleDisplayItem>> GetSalesByStatusAsync(string status)
        {
            var localDb = await LocalDatabaseService.GetConnection();

            var sales = await localDb.Table<LocalSale>().Where(s => s.Status == status).ToListAsync();
            var sortedSales = sales.OrderByDescending(s => s.OrderDate).ToList();

            return await SalesResolutionService.ResolveSaleDisplayItemsAsync(sortedSales);
        }

    }
}