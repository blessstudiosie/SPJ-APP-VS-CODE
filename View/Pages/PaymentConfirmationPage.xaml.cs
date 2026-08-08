using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public class PaymentDisplayItem
    {
        public string NotaNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string SalesPersonName { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public LocalPayment Original { get; set; } = null!;
    }

    public partial class PaymentConfirmationPage : UserControl, IRefreshablePage
    {
        public PaymentConfirmationPage()
        {
            InitializeComponent();
            RefreshData();
        }

        public async void RefreshData()
        {
            try
            {
                var payments = await SyncService.PullPendingPaymentsAsync();
                var db = await LocalDatabaseService.GetConnection();
                var sales = await db.Table<LocalSale>().ToListAsync();
                var customers = await db.Table<LocalCustomer>().ToListAsync();
                var salesPersons = await db.Table<LocalSalesPerson>().ToListAsync();

                var custById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var custByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in customers)
                {
                    if (!string.IsNullOrWhiteSpace(c.Id)) custById[c.Id.Trim()] = c.Name?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(c.Name)) custByName[c.Name.Trim()] = c.Name.Trim();
                }

                var spById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var spByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var sp in salesPersons)
                {
                    if (!string.IsNullOrWhiteSpace(sp.Id)) spById[sp.Id.Trim()] = sp.Name?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(sp.Name)) spByName[sp.Name.Trim()] = sp.Name.Trim();
                }

                var displayItems = payments.Select(p =>
                {
                    var sale = sales.FirstOrDefault(s => s.Id == p.SaleId || string.Equals(s.Nota, p.SaleId, StringComparison.OrdinalIgnoreCase));
                    string custRaw = sale?.CustomerId ?? "";
                    string spRaw = sale?.SalesPersonId ?? "";

                    return new PaymentDisplayItem
                    {
                        NotaNumber = sale?.Nota ?? p.SaleId,
                        CustomerName = SalesResolutionService.ResolveCustomerName(custRaw, custById, custByName),
                        SalesPersonName = SalesResolutionService.ResolveSalesPersonName(spRaw, spById, spByName),
                        PaymentDate = p.PaymentDate,
                        Amount = p.Amount,
                        PaymentMethod = p.PaymentMethod ?? "-",
                        Notes = p.Notes,
                        Original = p
                    };
                }).ToList();

                TabelPembayaran.ItemsSource = displayItems;
                TeksStatus.Text = $"Menunggu konfirmasi: {displayItems.Count} pembayaran";
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Memuat Pembayaran");
            }
        }

        private void TombolMuat_Click(object sender, RoutedEventArgs e) => RefreshData();

        private async void TombolSetujui_Click(object sender, RoutedEventArgs e) =>
            await ProcessConfirmation(sender, true);

        private async void TombolTolak_Click(object sender, RoutedEventArgs e) =>
            await ProcessConfirmation(sender, false);

        private async Task ProcessConfirmation(object sender, bool approve)
        {
            if (sender is not Button { Tag: LocalPayment payment }) return;
            if (!DialogHelper.ShowConfirm($"{(approve ? "Setujui" : "Tolak")} pembayaran {payment.Amount:N0}?")) return;
            try
            {
                await SyncService.ConfirmPaymentAsync(payment, approve);
                RefreshData();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Mengonfirmasi Pembayaran");
            }
        }
    }
}

