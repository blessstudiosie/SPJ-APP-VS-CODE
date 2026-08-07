using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public class StatusSummaryDisplayItem
    {
        public string StatusDisplay { get; set; } = string.Empty;
        public string CountDisplay { get; set; } = string.Empty;
        public string TotalDisplay { get; set; } = string.Empty;
        public bool IsOmset { get; set; }
    }

    public class SalesPersonPerformanceDisplayItem
    {
        public string SalesPersonId { get; set; } = string.Empty;
        public string SalesPersonName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string VisitCountDisplay { get; set; } = "0 Visit";
        public string OrderCountDisplay { get; set; } = "0 Nota";
        public decimal OmsetTotal { get; set; }
        public string OmsetDisplay { get; set; } = "Rp 0";
    }

    public partial class HomePage : UserControl, IRefreshablePage
    {
        private readonly CultureInfo _cultureIndo = new CultureInfo("id-ID");

        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
        }

        public void RefreshData()
        {
            _ = LoadDashboardDataAsync();
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        private async void TombolRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var now = DateTime.Now;
                var startDate = new DateTime(now.Year, now.Month, 1);
                var endDate = startDate.AddMonths(1).AddTicks(-1);

                string monthName = now.ToString("MMMM yyyy", _cultureIndo);
                TeksPeriodeHeader.Text = $"📅 Bulan Berjalan: {monthName}";

                var db = await LocalDatabaseService.GetConnection();

                // 1. Fetch All Master Data & Transactions from SQLite

                var salesPersons = await db.Table<LocalSalesPerson>().ToListAsync();
                var allSales = await db.Table<LocalSale>().ToListAsync();
                var allVisits = await db.Table<LocalVisitLogQueue>().ToListAsync();

                // 2. Filter Sales in Current Month based on DeliveryDate for Delivered Sales (TEMPO & DONE)
                var currentMonthDeliveredSales = allSales
                    .Where(s => s.DeliveryDate.HasValue &&
                                s.DeliveryDate.Value >= startDate &&
                                s.DeliveryDate.Value <= endDate &&
                                (string.Equals(s.Status, "DONE", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(s.Status, "TEMPO", StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                // 3. Calculate Executive KPI Card Metrics
                var doneSales = currentMonthDeliveredSales.Where(s => string.Equals(s.Status, "DONE", StringComparison.OrdinalIgnoreCase)).ToList();
                var tempoSales = currentMonthDeliveredSales.Where(s => string.Equals(s.Status, "TEMPO", StringComparison.OrdinalIgnoreCase)).ToList();

                decimal omsetDone = doneSales.Sum(s => s.Total);
                decimal omsetTempo = tempoSales.Sum(s => s.Total);
                decimal totalOmsetSah = omsetDone + omsetTempo;

                // Pending Delivery & Processing Notes
                var pendingSales = allSales
                    .Where(s => string.Equals(s.Status, "ON PROSES", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(s.Status, "DALAM PENGIRIMAN", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(s.Status, "SO", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                decimal nominalPending = pendingSales.Sum(s => s.Total);

                // Update Executive Card Labels
                TeksTotalOmset.Text = $"Rp {totalOmsetSah:N0}";
                TeksTotalOmsetNota.Text = $"{currentMonthDeliveredSales.Count} Nota (TEMPO + DONE)";

                TeksOmsetDone.Text = $"Rp {omsetDone:N0}";
                TeksOmsetDoneNota.Text = $"{doneSales.Count} Nota Lunas";

                TeksOmsetTempo.Text = $"Rp {omsetTempo:N0}";
                TeksOmsetTempoNota.Text = $"{tempoSales.Count} Nota Sisa Tagihan";

                TeksTotalPengiriman.Text = $"{pendingSales.Count} Nota";
                TeksNominalPengiriman.Text = $"Rp {nominalPending:N0} (Proses/Kirim)";

                // 4. Build Status Breakdown Summary List for Current Month
                var statusList = new List<StatusSummaryDisplayItem>();

                // DONE
                statusList.Add(new StatusSummaryDisplayItem
                {
                    StatusDisplay = "✅ DONE (Lunas)",
                    CountDisplay = $"{doneSales.Count} Nota",
                    TotalDisplay = $"Rp {omsetDone:N0}",
                    IsOmset = true
                });

                // TEMPO
                statusList.Add(new StatusSummaryDisplayItem
                {
                    StatusDisplay = "⏳ TEMPO (Piutang)",
                    CountDisplay = $"{tempoSales.Count} Nota",
                    TotalDisplay = $"Rp {omsetTempo:N0}",
                    IsOmset = true
                });

                // DALAM PENGIRIMAN
                var inDeliverySales = allSales.Where(s => string.Equals(s.Status, "DALAM PENGIRIMAN", StringComparison.OrdinalIgnoreCase)).ToList();
                statusList.Add(new StatusSummaryDisplayItem
                {
                    StatusDisplay = "🚚 DALAM PENGIRIMAN",
                    CountDisplay = $"{inDeliverySales.Count} Nota",
                    TotalDisplay = $"Rp {inDeliverySales.Sum(s => s.Total):N0}",
                    IsOmset = false
                });

                // ON PROSES
                var onProcessSales = allSales.Where(s => string.Equals(s.Status, "ON PROSES", StringComparison.OrdinalIgnoreCase)).ToList();
                statusList.Add(new StatusSummaryDisplayItem
                {
                    StatusDisplay = "📦 ON PROSES",
                    CountDisplay = $"{onProcessSales.Count} Nota",
                    TotalDisplay = $"Rp {onProcessSales.Sum(s => s.Total):N0}",
                    IsOmset = false
                });

                // SO (Draft Order)
                var draftSales = allSales.Where(s => string.Equals(s.Status, "SO", StringComparison.OrdinalIgnoreCase)).ToList();
                statusList.Add(new StatusSummaryDisplayItem
                {
                    StatusDisplay = "📑 SO (Draft Order)",
                    CountDisplay = $"{draftSales.Count} Nota",
                    TotalDisplay = $"Rp {draftSales.Sum(s => s.Total):N0}",
                    IsOmset = false
                });

                // Total Omset Sah Summary Row
                statusList.Add(new StatusSummaryDisplayItem
                {
                    StatusDisplay = "💰 TOTAL OMSET SAH",
                    CountDisplay = $"{currentMonthDeliveredSales.Count} Nota",
                    TotalDisplay = $"Rp {totalOmsetSah:N0}",
                    IsOmset = true
                });

                TabelStatusNota.ItemsSource = statusList;

                // 5. Build Sales Person Performance List (ONLY Sales Persons in sales_persons table)
                var currentMonthVisits = allVisits
                    .Where(v => v.CreatedAt >= startDate && v.CreatedAt <= endDate)
                    .ToList();

                var salesPerformanceList = new List<SalesPersonPerformanceDisplayItem>();

                foreach (var sp in salesPersons)
                {
                    // Filter Visits for this Sales Person in current month
                    int visitCount = currentMonthVisits.Count(v =>
                        (!string.IsNullOrEmpty(v.SalesPersonId) && string.Equals(v.SalesPersonId, sp.Id, StringComparison.OrdinalIgnoreCase)) ||
                        (string.IsNullOrEmpty(v.SalesPersonId) && string.Equals(v.SalesPersonName, sp.Name, StringComparison.OrdinalIgnoreCase)));

                    // Filter Delivered Sales (TEMPO & DONE) for this Sales Person in current month (by DeliveryDate)
                    var spSales = currentMonthDeliveredSales.Where(s =>
                        (!string.IsNullOrEmpty(s.SalesPersonId) && string.Equals(s.SalesPersonId, sp.Id, StringComparison.OrdinalIgnoreCase)) ||
                        string.Equals(s.SalesPersonId, sp.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    decimal spOmset = spSales.Sum(s => s.Total);

                    salesPerformanceList.Add(new SalesPersonPerformanceDisplayItem
                    {
                        SalesPersonId = sp.Id,
                        SalesPersonName = sp.Name,
                        Role = string.IsNullOrWhiteSpace(sp.Role) ? "SALES" : sp.Role.ToUpperInvariant(),
                        VisitCountDisplay = $"{visitCount} Visit",
                        OrderCountDisplay = $"{spSales.Count} Nota",
                        OmsetTotal = spOmset,
                        OmsetDisplay = $"Rp {spOmset:N0}"
                    });
                }

                // Sort Sales Persons by Omset descending
                TabelKinerjaSales.ItemsSource = salesPerformanceList.OrderByDescending(s => s.OmsetTotal).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading HomePage dashboard: {ex.Message}");
            }
        }
    }
}
