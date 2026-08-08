using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SPJ_APP.Model;
using SPJ_APP.Service;
using SPJ_APP.View.Pages;

namespace SPJ_APP.View
{
    public partial class DeliveryAssignmentWindow : Window
    {
        private readonly List<DeliverySelectableItem> _selectedSales;

        public DeliveryAssignmentWindow(List<DeliverySelectableItem> selectedSales)
        {
            InitializeComponent();
            _selectedSales = selectedSales;
            TeksJumlahNota.Text = $"{selectedSales.Count} nota akan dimasukkan ke pengiriman ini.";
            Loaded += DeliveryAssignmentWindow_Loaded;
        }

        private async void DeliveryAssignmentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

                InputSopir.ItemsSource = salesPersons;
                InputHelper.ItemsSource = salesPersons;
                InputChecker.ItemsSource = salesPersons;
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal memuat daftar personel armada: {ex.Message}");
            }
        }

        private async void TombolBuat_Click(object sender, RoutedEventArgs e)
        {
            TombolBuat.IsEnabled = false;
            TombolBuatDanCopyWa.IsEnabled = false;

            bool success = await SaveDeliveryAsync(copyToWa: false);
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                TombolBuat.IsEnabled = true;
                TombolBuatDanCopyWa.IsEnabled = true;
            }
        }

        private async void TombolBuatDanCopyWa_Click(object sender, RoutedEventArgs e)
        {
            TombolBuat.IsEnabled = false;
            TombolBuatDanCopyWa.IsEnabled = false;

            bool success = await SaveDeliveryAsync(copyToWa: true);
            if (success)
            {
                DialogResult = true;
                Close();
            }
            else
            {
                TombolBuat.IsEnabled = true;
                TombolBuatDanCopyWa.IsEnabled = true;
            }
        }

        private async Task<bool> SaveDeliveryAsync(bool copyToWa)
        {
            if (InputSopir.SelectedItem is not LocalSalesPerson sopir)
            {
                DialogHelper.ShowError("Pilih sopir terlebih dahulu.");
                return false;
            }

            if (InputChecker.SelectedItem is not LocalSalesPerson checker)
            {
                DialogHelper.ShowError("Pilih checker (penanggung jawab) terlebih dahulu.");
                return false;
            }

            string helperName = (InputHelper.SelectedItem as LocalSalesPerson)?.Name ?? "-";
            string helperId = (InputHelper.SelectedItem as LocalSalesPerson)?.Id ?? "";

            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                string deliveryId = Guid.NewGuid().ToString();
                string deliveryNumber = CreateDeliveryNumber();

                var delivery = new LocalDelivery
                {
                    Id = deliveryId,
                    DeliveryNumber = deliveryNumber,
                    DriverId = sopir.Id,
                    HelperId = string.IsNullOrEmpty(helperId) ? null : helperId,
                    CheckerId = checker.Id,
                    Status = "OPEN",
                    UpdatedAt = DateTime.Now,
                    IsSynced = false
                };

                await localDb.RunInTransactionAsync(conn =>
                {
                    conn.Insert(delivery);

                    foreach (var item in _selectedSales)
                    {
                        var sale = item.Original.Original;
                        sale.Status = "DALAM PENGIRIMAN";
                        sale.UpdatedAt = DateTime.Now;
                        sale.IsSynced = false;
                        conn.Update(sale);

                        var detail = new LocalDeliveryDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            DeliveryId = deliveryId,
                            SaleId = sale.Id
                        };
                        conn.Insert(detail);
                    }
                });

                if (copyToWa)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"*Surat Pengiriman {deliveryNumber} - {DateTime.Now:dd/MM/yyyy HH:mm}*");
                    sb.AppendLine($"👨‍✈️ Sopir: {sopir.Name}");
                    if (!string.IsNullOrWhiteSpace(helperName) && helperName != "-")
                        sb.AppendLine($"👥 Helper: {helperName}");
                    sb.AppendLine($"🔍 Checker: {checker.Name}");
                    sb.AppendLine();

                    decimal grandTotal = 0;
                    var groupedByJalur = _selectedSales.GroupBy(x => x.JalurPengiriman).OrderBy(g => g.Key);

                    foreach (var group in groupedByJalur)
                    {
                        sb.AppendLine($"🚚 *Jalur: {group.Key}*");
                        int no = 1;
                        foreach (var item in group.OrderBy(x => x.Nota))
                        {
                            sb.AppendLine($"{no}. {item.Nota} - {item.CustomerName} (Sales: {item.SalesPersonName}) - Rp {item.Total:N0}");
                            grandTotal += item.Total;
                            no++;
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine($"*Total Nilai Pengiriman: Rp {grandTotal:N0}*");
                    sb.AppendLine($"*Jumlah Nota: {_selectedSales.Count}*");

                    Clipboard.SetText(sb.ToString());
                    DialogHelper.ShowInfo($"Nota Pengiriman '{deliveryNumber}' berhasil dibuat dan format rekap WhatsApp telah disalin ke clipboard!\n\nSilakan paste (Ctrl+V) langsung ke grup WhatsApp Expedisi/Sopir.");
                }
                else
                {
                    DialogHelper.ShowInfo($"Nota Pengiriman '{deliveryNumber}' berhasil dibuat dengan {_selectedSales.Count} nota.");
                }

                return true;
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Membuat Pengiriman");
                return false;
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string CreateDeliveryNumber() =>
            $"{DateTime.Now:ddMMyy-HHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
    }
}
