using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public string SalesPersonName { get; set; } = "";
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

            var routeByCustId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var routeByCustName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in customers)
            {
                string r = !string.IsNullOrWhiteSpace(c.JalurPengiriman) ? c.JalurPengiriman.Trim() : "Lokal / Umum";
                if (!string.IsNullOrWhiteSpace(c.Id)) routeByCustId[c.Id.Trim()] = r;
                if (!string.IsNullOrWhiteSpace(c.Name)) routeByCustName[c.Name.Trim()] = r;
            }

            string ResolveJalur(string? custId, string? custName)
            {
                if (!string.IsNullOrWhiteSpace(custId) && routeByCustId.TryGetValue(custId.Trim(), out var r1) && !string.IsNullOrWhiteSpace(r1))
                    return r1;
                if (!string.IsNullOrWhiteSpace(custName) && routeByCustName.TryGetValue(custName.Trim(), out var r2) && !string.IsNullOrWhiteSpace(r2))
                    return r2;
                return "Lokal / Umum";
            }

            _siapKirimItems = items.Select(s => new DeliverySelectableItem
            {
                Nota = s.Nota,
                CustomerName = string.IsNullOrWhiteSpace(s.CustomerName) ? "Pelanggan Umum" : s.CustomerName,
                SalesPersonName = string.IsNullOrWhiteSpace(s.SalesPersonName) ? "Sales Umum" : s.SalesPersonName,
                JalurPengiriman = ResolveJalur(s.Original.CustomerId, s.CustomerName),
                Total = s.Total,
                Remaining = s.Remaining,
                Original = s
            }).ToList();

            PopulateJalurFilterCombo();
            ApplySiapKirimFilter();
        }

        private void PopulateJalurFilterCombo()
        {
            string currentSelected = ComboFilterJalur.SelectedItem?.ToString() ?? "SEMUA JALUR PENGIRIMAN";

            var jalurs = _siapKirimItems
                .Select(i => i.JalurPengiriman)
                .Where(j => !string.IsNullOrWhiteSpace(j))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(j => j)
                .ToList();

            var comboItems = new List<string> { "SEMUA JALUR PENGIRIMAN" };
            comboItems.AddRange(jalurs);

            ComboFilterJalur.ItemsSource = comboItems;

            if (comboItems.Contains(currentSelected))
            {
                ComboFilterJalur.SelectedItem = currentSelected;
            }
            else
            {
                ComboFilterJalur.SelectedIndex = 0;
            }
        }

        private void ApplySiapKirimFilter()
        {
            string selectedJalur = ComboFilterJalur.SelectedItem?.ToString() ?? "SEMUA JALUR PENGIRIMAN";
            string searchText = TxtCariNota?.Text?.Trim() ?? string.Empty;

            IEnumerable<DeliverySelectableItem> filtered = _siapKirimItems;

            if (selectedJalur != "SEMUA JALUR PENGIRIMAN")
            {
                filtered = filtered.Where(i => string.Equals(i.JalurPengiriman, selectedJalur, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(i =>
                    (i.Nota != null && i.Nota.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (i.CustomerName != null && i.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (i.SalesPersonName != null && i.SalesPersonName.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                );
            }

            var viewList = filtered.ToList();
            var view = CollectionViewSource.GetDefaultView(viewList);
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DeliverySelectableItem.JalurPengiriman)));
            TabelSiapKirim.ItemsSource = view;
        }

        private void ComboFilterJalur_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplySiapKirimFilter();
            }
        }

        private void TxtCariNota_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ApplySiapKirimFilter();
            }
        }

        private async Task LoadOpenDeliveries()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var deliveries = await localDb.Table<LocalDelivery>().Where(d => d.Status == "OPEN").ToListAsync();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
            var allDetails = await localDb.Table<LocalDeliveryDetail>().ToListAsync();
            var allSales = await SalesQueryService.GetSalesByStatusAsync("DALAM PENGIRIMAN");
            var customers = await GetCustomers();

            var spById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var spByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sp in salesPersons)
            {
                if (!string.IsNullOrWhiteSpace(sp.Id)) spById[sp.Id.Trim()] = sp.Name?.Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(sp.Name)) spByName[sp.Name.Trim()] = sp.Name.Trim();
            }

            var routeByCustId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var routeByCustName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var c in customers)
            {
                string r = !string.IsNullOrWhiteSpace(c.JalurPengiriman) ? c.JalurPengiriman.Trim() : "Lokal / Umum";
                if (!string.IsNullOrWhiteSpace(c.Id)) routeByCustId[c.Id.Trim()] = r;
                if (!string.IsNullOrWhiteSpace(c.Name)) routeByCustName[c.Name.Trim()] = r;
            }

            string ResolveSalesName(string? idOrName)
            {
                return SalesResolutionService.ResolveSalesPersonName(idOrName, spById, spByName);
            }

            string ResolveJalur(string? custId, string? custName)
            {
                if (!string.IsNullOrWhiteSpace(custId) && routeByCustId.TryGetValue(custId.Trim(), out var r1) && !string.IsNullOrWhiteSpace(r1))
                    return r1;
                if (!string.IsNullOrWhiteSpace(custName) && routeByCustName.TryGetValue(custName.Trim(), out var r2) && !string.IsNullOrWhiteSpace(r2))
                    return r2;
                return "Lokal / Umum";
            }

            _openDeliveries = deliveries.Select(d =>
            {
                var detailSaleIds = allDetails.Where(x => x.DeliveryId == d.Id).Select(x => x.SaleId).ToHashSet();
                var itemsInThisDelivery = allSales.Where(s => detailSaleIds.Contains(s.Id))
                    .Select(s => new DeliverySelectableItem
                    {
                        Nota = s.Nota,
                        CustomerName = string.IsNullOrWhiteSpace(s.CustomerName) ? "Pelanggan Umum" : s.CustomerName,
                        SalesPersonName = string.IsNullOrWhiteSpace(s.SalesPersonName) ? "Sales Umum" : s.SalesPersonName,
                        JalurPengiriman = ResolveJalur(s.Original.CustomerId, s.CustomerName),
                        Total = s.Total,
                        Remaining = s.Remaining,
                        Original = s
                    }).ToList();

                return new OpenDeliveryDisplay
                {
                    Delivery = d,
                    DriverName = ResolveSalesName(d.DriverId),
                    HelperName = ResolveSalesName(d.HelperId),
                    CheckerName = ResolveSalesName(d.CheckerId),
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

                foreach (var item in deliveryDisplay.Items)
                {
                    var sale = item.Original.Original;
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
