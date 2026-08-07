using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public class DeliverySelectableItem
    {
        public bool IsSelected { get; set; }
        public string Nota { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string JalurPengiriman { get; set; } = "-";
        public decimal Total { get; set; }
        public decimal Remaining { get; set; }
        public string StatusTujuan => Remaining <= 0 ? "DONE" : "TEMPO";
        public SaleDisplayItem Original { get; set; } = null!;
    }

    public class OpenDeliveryDisplay
    {
        public LocalDelivery Delivery { get; set; } = null!;
        public string DriverName { get; set; } = "-";
        public string HelperName { get; set; } = "-";
        public string CheckerName { get; set; } = "-";
        public int JumlahNota { get; set; }
        public List<DeliverySelectableItem> Items { get; set; } = new();
    }

    public partial class DeliveryPage : UserControl, IRefreshablePage
    {
        private List<DeliverySelectableItem> _siapKirimItems = new();
        private List<OpenDeliveryDisplay> _openDeliveries = new();

        public DeliveryPage()
        {
            InitializeComponent();
            RefreshData();
        }

        public async void RefreshData()
        {
            await LoadSiapKirim();
            await LoadOpenDeliveries();
        }

        private async Task<List<LocalCustomer>> GetCustomers()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            return await localDb.Table<LocalCustomer>().ToListAsync();
        }

        private async Task LoadSiapKirim()
        {
            var items = await SalesQueryService.GetSalesByStatusAsync("ON PROSES");
            var customers = await GetCustomers();

            _siapKirimItems = items.Select(s => new DeliverySelectableItem
            {
                Nota = s.Nota,
                CustomerName = s.CustomerName,
                JalurPengiriman = customers.FirstOrDefault(c => c.Id == s.Original.CustomerId)?.JalurPengiriman ?? "-",
                Total = s.Total,
                Remaining = s.Remaining,
                Original = s
            }).ToList();

            var view = CollectionViewSource.GetDefaultView(_siapKirimItems);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DeliverySelectableItem.JalurPengiriman)));
            TabelSiapKirim.ItemsSource = view;
        }

        private async Task LoadOpenDeliveries()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var deliveries = await localDb.Table<LocalDelivery>().Where(d => d.Status == "OPEN").ToListAsync();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
            var allDetails = await localDb.Table<LocalDeliveryDetail>().ToListAsync();
            var allSales = await SalesQueryService.GetSalesByStatusAsync("DALAM PENGIRIMAN");
            var customers = await GetCustomers();

            _openDeliveries = deliveries.Select(d =>
            {
                var detailSaleIds = allDetails.Where(x => x.DeliveryId == d.Id).Select(x => x.SaleId).ToHashSet();
                var itemsInThisDelivery = allSales.Where(s => detailSaleIds.Contains(s.Id))
                    .Select(s => new DeliverySelectableItem
                    {
                        Nota = s.Nota,
                        CustomerName = s.CustomerName,
                        JalurPengiriman = customers.FirstOrDefault(c => c.Id == s.Original.CustomerId)?.JalurPengiriman ?? "-",
                        Total = s.Total,
                        Remaining = s.Remaining,
                        Original = s
                    }).ToList();

                return new OpenDeliveryDisplay
                {
                    Delivery = d,
                    DriverName = salesPersons.FirstOrDefault(sp => sp.Id == d.DriverId)?.Name ?? "-",
                    HelperName = salesPersons.FirstOrDefault(sp => sp.Id == d.HelperId)?.Name ?? "-",
                    CheckerName = salesPersons.FirstOrDefault(sp => sp.Id == d.CheckerId)?.Name ?? "-",
                    JumlahNota = itemsInThisDelivery.Count,
                    Items = itemsInThisDelivery
                };
            }).ToList();

            TabelPengirimanAktif.ItemsSource = _openDeliveries;
        }

        private void TombolBuatPengiriman_Click(object sender, RoutedEventArgs e)
        {
            TabelSiapKirim.CommitEdit(DataGridEditingUnit.Row, true);

            var selected = _siapKirimItems.Where(x => x.IsSelected).ToList();

            if (selected.Count == 0)
            {
                DialogHelper.ShowError("Pilih minimal 1 nota untuk dikirim.");
                return;
            }

            var form = new DeliveryAssignmentWindow(selected) { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                RefreshData();
            }
        }

        private void TombolSelesaikanDelivery_Click(object sender, RoutedEventArgs e)
{
    if (sender is not Button btn || btn.Tag is not OpenDeliveryDisplay deliveryDisplay) return;

    bool semuaSesuai = DialogHelper.ShowConfirm(
        $"Apakah semua barang di pengiriman '{deliveryDisplay.Delivery.DeliveryNumber}' terkirim SESUAI nota (tidak ada yang dibatalkan)?\n\nKlik 'Ya' kalau semua sesuai, atau 'Tidak' kalau ada barang yang dibatalkan/berkurang.",
        "Verifikasi Pengiriman");

    if (semuaSesuai)
    {
        ProsesSelesaikanTanpaPerubahan(deliveryDisplay);
    }
    else
    {
        var verifWindow = new DeliveryVerificationWindow(deliveryDisplay) { Owner = Window.GetWindow(this) };
        if (verifWindow.ShowDialog() == true)
        {
            RefreshData();
        }
    }
}

        private async void TombolBatalkanDelivery_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not OpenDeliveryDisplay deliveryDisplay)
                return;

            var choice = MessageBox.Show(
                "Pilih hasil pembatalan pengiriman:\n\n" +
                "Ya: nota kembali ke ON PROSES (siap dijadwalkan ulang, stok tetap dipesan).\n" +
                "Tidak: barang ditolak dan nota kembali ke SO (Stok Ready dikembalikan).\n" +
                "Batal: tidak ada perubahan.",
                "Batalkan Pengiriman",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            if (choice == MessageBoxResult.Cancel)
                return;

            var targetStatus = choice == MessageBoxResult.Yes ? "ON PROSES" : "SO";
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                await localDb.RunInTransactionAsync(conn =>
                {
                    var deliveryDetails = conn.Table<LocalDeliveryDetail>()
                        .Where(detail => detail.DeliveryId == deliveryDisplay.Delivery.Id)
                        .ToList();

                    foreach (var deliveryDetail in deliveryDetails)
                    {
                        var sale = conn.Find<LocalSale>(deliveryDetail.SaleId);
                        if (sale is null || sale.Status != "DALAM PENGIRIMAN")
                            continue;

                        if (targetStatus == "SO")
                        {
                            foreach (var detail in conn.Table<LocalSalesDetail>().Where(item => item.SaleId == sale.Id))
                            {
                                var product = conn.Find<LocalProduct>(detail.ProductId);
                                if (product is null) continue;
                                product.StokReady = Math.Max(0, product.StokReady + detail.Qty);
                                product.UpdatedAt = DateTime.Now;
                                product.IsSynced = false;
                                conn.Update(product);
                            }
                        }

                        sale.Status = targetStatus;
                        sale.DeliveryDate = null;
                        sale.UpdatedAt = DateTime.Now;
                        sale.IsSynced = false;
                        conn.Update(sale);
                        conn.Delete(deliveryDetail);
                    }

                    var delivery = deliveryDisplay.Delivery;
                    delivery.Status = "CANCELLED";
                    delivery.ClosedAt = DateTime.Now;
                    delivery.UpdatedAt = DateTime.Now;
                    delivery.IsSynced = false;
                    conn.Update(delivery);
                });

                DialogHelper.ShowInfo($"Pengiriman dibatalkan. Nota dikembalikan ke status {targetStatus}.");
                RefreshData();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Membatalkan Pengiriman");
            }
        }

        private async void ProsesSelesaikanTanpaPerubahan(OpenDeliveryDisplay deliveryDisplay)
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                bool requiresAuth = false;

                // Pre-check if authorization is needed
                foreach (var item in deliveryDisplay.Items)
                {
                    var sale = item.Original.Original;
                    // Do not re-prompt for auth if status is already TEMPO or DONE
                    if (sale.Status == "TEMPO" || sale.Status == "DONE") continue;
                    
                    string newStatus = sale.Remaining <= 0 ? "DONE" : "TEMPO";
                    if (newStatus == "TEMPO" || newStatus == "DONE")
                    {
                        requiresAuth = true;
                        break;
                    }
                }

                if (requiresAuth)
                {
                    var passwordDialog = new PasswordPromptWindow();
                    if (passwordDialog.ShowDialog() == true)
                    {
                        bool authorized = await AuthorizationService.AuthorizeManagerActionAsync(passwordDialog.Password);
                        if (!authorized)
                        {
                            DialogHelper.ShowError("Password otorisasi salah atau tidak ada Manager/Owner yang terdaftar.", "Otorisasi Gagal");
                            return;
                        }
                        await ActivityLogService.LogAsync("AUTH_SUCCESS", "Otorisasi Manager berhasil untuk menyelesaikan pengiriman tanpa perubahan.");
                    }
                    else
                    {
                        // User cancelled the password dialog
                        return;
                    }
                }
                
                await localDb.RunInTransactionAsync(conn =>
                {
                    foreach (var item in deliveryDisplay.Items)
                    {
                        var sale = item.Original.Original;

                        var details = conn.Table<LocalSalesDetail>().Where(d => d.SaleId == sale.Id).ToList();
                        foreach (var detail in details)
                        {
                            var product = conn.Table<LocalProduct>().FirstOrDefault(p => p.Id == detail.ProductId);
                            if (product != null)
                            {
                                product.StokFisik = Math.Max(0, product.StokFisik - detail.Qty);
                                product.UpdatedAt = DateTime.Now;
                                product.IsSynced = false;
                                conn.Update(product);
                            }
                        }

                        sale.Status = sale.Remaining <= 0 ? "DONE" : "TEMPO";
                        sale.DeliveryDate = DateTime.Now;
                        sale.UpdatedAt = DateTime.Now;
                        sale.IsSynced = false;
                        conn.Update(sale);
                    }

                    var delivery = deliveryDisplay.Delivery;
                    delivery.Status = "CLOSED";
                    delivery.ClosedAt = DateTime.Now;
                    delivery.UpdatedAt = DateTime.Now;
                    delivery.IsSynced = false;
                    conn.Update(delivery);
                });

                await ActivityLogService.LogAsync("CLOSE_DELIVERY_NOCHANGE", $"Pengiriman '{deliveryDisplay.Delivery.DeliveryNumber}' ditutup tanpa perubahan.");
                DialogHelper.ShowInfo("Pengiriman berhasil diselesaikan dan ditutup.");
                RefreshData();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menyelesaikan Pengiriman");
            }
        }

        private void TombolCopyWaSiapKirim_Click(object sender, RoutedEventArgs e)
        {
            TabelSiapKirim.CommitEdit(DataGridEditingUnit.Row, true);
            var selected = _siapKirimItems.Where(x => x.IsSelected).ToList();
            CopySelectedToClipboard(selected, "Daftar Nota Siap Dikirim", null, null, null);
        }

        private void TombolCopyWaDelivery_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not OpenDeliveryDisplay deliveryDisplay) return;
            CopySelectedToClipboard(deliveryDisplay.Items, $"Pengiriman {deliveryDisplay.Delivery.DeliveryNumber}",
                deliveryDisplay.DriverName, deliveryDisplay.HelperName, deliveryDisplay.CheckerName);
        }

        private void CopySelectedToClipboard(List<DeliverySelectableItem> items, string judul,
            string? sopir, string? helper, string? checker)
        {
            if (items.Count == 0)
            {
                DialogHelper.ShowError("Tidak ada nota untuk di-copy.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"*{judul} - {DateTime.Now:dd/MM/yyyy}*");
            if (sopir != null) sb.AppendLine($"Sopir: {sopir} | Helper: {helper} | Checker: {checker}");
            sb.AppendLine();

            decimal grandTotal = 0;
            var groupedByJalur = items.GroupBy(x => x.JalurPengiriman).OrderBy(g => g.Key);

            foreach (var group in groupedByJalur)
            {
                sb.AppendLine($"🚚 *Jalur: {group.Key}*");
                int no = 1;
                foreach (var item in group.OrderBy(x => x.Nota))
                {
                    sb.AppendLine($"{no}. {item.Nota} - {item.CustomerName} - Rp {item.Total:N0}");
                    grandTotal += item.Total;
                    no++;
                }
                sb.AppendLine();
            }

            sb.AppendLine($"*Total: Rp {grandTotal:N0}*");
            sb.AppendLine($"*Jumlah Nota: {items.Count}*");

            Clipboard.SetText(sb.ToString());
            DialogHelper.ShowInfo("Teks berhasil disalin ke clipboard. Silakan paste (Ctrl+V) ke WhatsApp.");
        }
    }
}
