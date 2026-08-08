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

    public class JalurPengirimanDisplayItem
    {
        public string JalurName { get; set; } = "Tanpa Jalur";
        public int OrderCount { get; set; }
        public string OrderCountDisplay => $"{OrderCount} Nota";
        public decimal TotalNominal { get; set; }
        public string TotalNominalDisplay => $"Rp {TotalNominal:N0}";
    }

    public partial class HomePage : UserControl, IRefreshablePage
    {
        private readonly CultureInfo _cultureIndo = new CultureInfo("id-ID");

        public HomePage()
        {
            InitializeComponent();
            Loaded += HomePage_Loaded;
            Unloaded += HomePage_Unloaded;
        }

        private void HomePage_Unloaded(object sender, RoutedEventArgs e)
        {
            BackgroundSyncService.Instance.SyncStatusChanged -= BackgroundSync_SyncStatusChanged;
        }

        public void RefreshData()
        {
            _ = LoadDashboardDataAsync();
        }

        private async void HomePage_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundSyncService.Instance.SyncStatusChanged -= BackgroundSync_SyncStatusChanged;
            BackgroundSyncService.Instance.SyncStatusChanged += BackgroundSync_SyncStatusChanged;
            await LoadDashboardDataAsync();
        }

        private void BackgroundSync_SyncStatusChanged(object? sender, SyncStatusEventArgs e)
        {
            if (!e.IsSyncing)
            {
                Dispatcher.Invoke(() => RefreshData());
            }
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
                string monthName = now.ToString("MMMM yyyy", _cultureIndo);
                TeksPeriodeHeader.Text = $"📅 Bulan Berjalan: {monthName}";

                var db = await LocalDatabaseService.GetConnection();

                // 1. Fetch All Master Data & Transactions from SQLite
                var salesPersons = await db.Table<LocalSalesPerson>().ToListAsync();
                var allSales = await db.Table<LocalSale>().ToListAsync();
                var allVisits = await db.Table<LocalVisitLogQueue>().ToListAsync();
                var customers = await db.Table<LocalCustomer>().ToListAsync();

                // 2. Filter Sales in Current Month based on DeliveryDate (or OrderDate fallback) for Delivered Sales (TEMPO & DONE)
                var currentMonthDeliveredSales = allSales
                    .Where(s => {
                        bool isDeliveredStatus = string.Equals(s.Status, "DONE", StringComparison.OrdinalIgnoreCase) ||
                                                string.Equals(s.Status, "TEMPO", StringComparison.OrdinalIgnoreCase);
                        if (!isDeliveredStatus) return false;

                        DateTime dateToCheck = s.DeliveryDate ?? s.OrderDate;
                        return dateToCheck.Year == now.Year && dateToCheck.Month == now.Month;
                    })
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

                // 5. Build Sales Person Performance List (Grouped directly by resolved Sales Person Name)
                var currentMonthVisits = allVisits
                    .Where(v => v.CreatedAt.Year == now.Year && v.CreatedAt.Month == now.Month)
                    .ToList();

                var spById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var spByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var sp in salesPersons)
                {
                    if (!string.IsNullOrWhiteSpace(sp.Id))
                    {
                        string idKey = sp.Id.Trim();
                        spById[idKey] = sp.Name?.Trim() ?? "";
                        if (Guid.TryParse(idKey, out var g))
                        {
                            spById[g.ToString("D")] = sp.Name?.Trim() ?? "";
                            spById[g.ToString("N")] = sp.Name?.Trim() ?? "";
                            if (g.ToString("D").Length >= 8) spById[g.ToString("D").Substring(0, 8)] = sp.Name?.Trim() ?? "";
                        }
                        else if (idKey.Length >= 8)
                        {
                            spById[idKey.Substring(0, 8)] = sp.Name?.Trim() ?? "";
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(sp.Name))
                    {
                        spByName[sp.Name.Trim()] = sp.Name.Trim();
                    }
                }

                var activeSalesPersons = salesPersons
                    .Where(sp =>
                        !string.IsNullOrWhiteSpace(sp.Name) &&
                        !string.Equals(sp.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(sp.Role, "ADMIN", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(sp.Name, "blessstudiosie", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(sp.Name, "Developer", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(sp.Name, "Admin", StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();

                // Group delivered sales directly by resolved Sales Person Name
                var salesGroupedBySalesPerson = currentMonthDeliveredSales
                    .GroupBy(s => NameLookupService.GetSalesPersonName(s.SalesPersonId))
                    .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

                // Group current month visits directly by resolved Sales Person Name
                var visitsGroupedBySalesPerson = currentMonthVisits
                    .GroupBy(v => {
                        string spInput = !string.IsNullOrWhiteSpace(v.SalesPersonName) ? v.SalesPersonName : v.SalesPersonId ?? "";
                        return NameLookupService.GetSalesPersonName(spInput);
                    })
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                // Collect all Sales Person names from master table + actual transactions
                var allSalesNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var sp in activeSalesPersons)
                {
                    if (!string.IsNullOrWhiteSpace(sp.Name)) allSalesNames.Add(sp.Name.Trim());
                }

                foreach (var key in salesGroupedBySalesPerson.Keys)
                {
                    if (!string.IsNullOrWhiteSpace(key) && key != "-")
                    {
                        if (!key.Equals("blessstudiosie", StringComparison.OrdinalIgnoreCase) &&
                            !key.Equals("Developer", StringComparison.OrdinalIgnoreCase) &&
                            !key.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            allSalesNames.Add(key);
                        }
                    }
                }

                var salesPerformanceList = new List<SalesPersonPerformanceDisplayItem>();

                foreach (var salesName in allSalesNames)
                {
                    var spSales = salesGroupedBySalesPerson.TryGetValue(salesName, out var sList) ? sList : new List<LocalSale>();
                    int visitCount = visitsGroupedBySalesPerson.TryGetValue(salesName, out var vCount) ? vCount : 0;
                    
                    var masterSp = activeSalesPersons.FirstOrDefault(sp => string.Equals(sp.Name?.Trim(), salesName, StringComparison.OrdinalIgnoreCase));
                    string roleDisplay = masterSp != null && !string.IsNullOrWhiteSpace(masterSp.Role) ? masterSp.Role.ToUpperInvariant() : "SALES";
                    string spId = masterSp?.Id ?? salesName;

                    decimal spOmset = spSales.Sum(s => s.Total);

                    salesPerformanceList.Add(new SalesPersonPerformanceDisplayItem
                    {
                        SalesPersonId = spId,
                        SalesPersonName = salesName,
                        Role = roleDisplay,
                        VisitCountDisplay = $"{visitCount} Visit",
                        OrderCountDisplay = $"{spSales.Count} Nota",
                        OmsetTotal = spOmset,
                        OmsetDisplay = $"Rp {spOmset:N0}"
                    });
                }

                // Sort Sales Persons by Omset descending
                TabelKinerjaSales.ItemsSource = salesPerformanceList.OrderByDescending(s => s.OmsetTotal).ToList();

                // 6. Build Undelivered Sales Nominal Grouped by Delivery Route (Jalur Pengiriman)
                var customerDictById = new Dictionary<string, LocalCustomer>(StringComparer.OrdinalIgnoreCase);
                var customerDictByName = new Dictionary<string, LocalCustomer>(StringComparer.OrdinalIgnoreCase);

                foreach (var c in customers)
                {
                    if (!string.IsNullOrEmpty(c.Id)) customerDictById[c.Id] = c;
                    if (!string.IsNullOrWhiteSpace(c.Name)) customerDictByName[c.Name.Trim()] = c;
                }

                LocalCustomer? GetCustomerForSale(string? customerIdOrName)
                {
                    if (string.IsNullOrWhiteSpace(customerIdOrName)) return null;
                    string key = customerIdOrName.Trim();
                    if (customerDictById.TryGetValue(key, out var c1)) return c1;
                    if (customerDictByName.TryGetValue(key, out var c2)) return c2;
                    return null;
                }

                var undeliveredSalesList = allSales
                    .Where(s => string.Equals(s.Status, "ON PROSES", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(s.Status, "DALAM PENGIRIMAN", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(s.Status, "SO", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var jalurGroups = undeliveredSalesList
                    .GroupBy(s => {
                        var cust = GetCustomerForSale(s.CustomerId);
                        if (cust != null && !string.IsNullOrWhiteSpace(cust.JalurPengiriman))
                        {
                            return cust.JalurPengiriman.Trim();
                        }
                        return "Tanpa Jalur / Umum";
                    })
                    .Select(g => new JalurPengirimanDisplayItem
                    {
                        JalurName = g.Key.StartsWith("🚚") ? g.Key : $"🚚 {g.Key}",
                        OrderCount = g.Count(),
                        TotalNominal = g.Sum(s => s.Total)
                    })
                    .OrderByDescending(j => j.TotalNominal)
                    .ToList();


                if (!jalurGroups.Any())
                {
                    jalurGroups.Add(new JalurPengirimanDisplayItem
                    {
                        JalurName = "✅ Semua Nota Telah Terkirim (0 Pending)",
                        OrderCount = 0,
                        TotalNominal = 0
                    });
                }

                TabelJalurPengiriman.ItemsSource = jalurGroups;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading HomePage dashboard: {ex.Message}");
            }
        }
    }
}

