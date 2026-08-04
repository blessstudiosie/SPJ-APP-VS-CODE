using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;
using SPJ_APP.View.Pages;

namespace SPJ_APP.View
{
    public class VerificationItemDisplay
    {
        public string Nota { get; set; } = "";
        public string SaleId { get; set; } = "";
        public string DetailId { get; set; } = "";
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal OriginalQty { get; set; }
        public decimal Price { get; set; }
        public string ActualQtyText { get; set; } = "";
    }

    public partial class DeliveryVerificationWindow : Window
    {
        private readonly OpenDeliveryDisplay _deliveryDisplay;
        private List<VerificationItemDisplay> _verificationItems = new();

        public DeliveryVerificationWindow(OpenDeliveryDisplay deliveryDisplay)
        {
            InitializeComponent();
            _deliveryDisplay = deliveryDisplay;
            Loaded += DeliveryVerificationWindow_Loaded;
        }

        private async void DeliveryVerificationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var products = await localDb.Table<LocalProduct>().ToListAsync();

            _verificationItems = new List<VerificationItemDisplay>();

            foreach (var item in _deliveryDisplay.Items)
            {
                var sale = item.Original.Original;
                var details = await localDb.Table<LocalSalesDetail>().Where(d => d.SaleId == sale.Id).ToListAsync();

                foreach (var d in details)
                {
                    _verificationItems.Add(new VerificationItemDisplay
                    {
                        Nota = sale.Nota,
                        SaleId = sale.Id,
                        DetailId = d.Id,
                        ProductId = d.ProductId,
                        ProductName = products.FirstOrDefault(p => p.Id == d.ProductId)?.Name ?? "(produk dihapus)",
                        OriginalQty = d.Qty,
                        Price = d.Price,
                        ActualQtyText = d.Qty.ToString("0.##")
                    });
                }
            }

            TabelVerifikasi.ItemsSource = _verificationItems;
        }

        private async void TombolKonfirmasi_Click(object sender, RoutedEventArgs e)
        {
            TabelVerifikasi.CommitEdit(DataGridEditingUnit.Row, true);

            bool confirm = DialogHelper.ShowConfirm(
                "Simpan penyesuaian ini? Item yang qty-nya dikurangi akan mengembalikan sebagian Stok Ready, dan total nota akan disesuaikan. Tindakan ini tidak bisa dibatalkan.",
                "Konfirmasi Penyesuaian");

            if (!confirm) return;

            try
            {
                var localDb = await LocalDatabaseService.GetConnection();

                await localDb.RunInTransactionAsync(conn =>
                {
                    var itemsBySale = _verificationItems.GroupBy(x => x.SaleId);

                    foreach (var group in itemsBySale)
                    {
                        var sale = conn.Table<LocalSale>().FirstOrDefault(s => s.Id == group.Key);
                        if (sale is null)
                            continue;

                        decimal newTotal = 0;

                        foreach (var vItem in group)
                        {
                            decimal actualQty = decimal.TryParse(vItem.ActualQtyText, out decimal q) ? q : 0;
                            actualQty = Math.Min(Math.Max(actualQty, 0), vItem.OriginalQty);
                            decimal diff = vItem.OriginalQty - actualQty;

                            var product = conn.Table<LocalProduct>().FirstOrDefault(p => p.Id == vItem.ProductId);
                            if (product != null)
                            {
                                if (diff > 0)
                                {
                                    product.StokReady = Math.Max(0, product.StokReady + diff);
                                }

                                if (actualQty > 0)
                                {
                                    product.StokFisik = Math.Max(0, product.StokFisik - actualQty);
                                }

                                product.UpdatedAt = DateTime.Now;
                                product.IsSynced = false;
                                conn.Update(product);
                            }

                            var detail = conn.Table<LocalSalesDetail>().FirstOrDefault(d => d.Id == vItem.DetailId);
                            if (detail is null)
                                continue;

                            if (actualQty <= 0)
                            {
                                conn.Delete(detail);
                            }
                            else
                            {
                                detail.Qty = actualQty;
                                detail.Subtotal = detail.Price * actualQty;
                                conn.Update(detail);
                                newTotal += detail.Subtotal;
                            }
                        }

                        sale.Total = Math.Max(0, newTotal);
                        sale.Remaining = Math.Max(0, sale.Total - sale.Paid);
                        sale.Status = sale.Remaining <= 0 ? "DONE" : "TEMPO";
                        sale.DeliveryDate = DateTime.Now;
                        sale.UpdatedAt = DateTime.Now;
                        sale.IsSynced = false;
                        conn.Update(sale);
                    }

                    var delivery = _deliveryDisplay.Delivery;
                    delivery.Status = "CLOSED";
                    delivery.ClosedAt = DateTime.Now;
                    delivery.UpdatedAt = DateTime.Now;
                    delivery.IsSynced = false;
                    conn.Update(delivery);
                });

                DialogHelper.ShowInfo("Verifikasi tersimpan, dan pengiriman berhasil ditutup.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menyimpan Verifikasi");
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}