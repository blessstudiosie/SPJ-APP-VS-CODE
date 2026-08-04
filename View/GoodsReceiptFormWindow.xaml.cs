using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public class GoodsReceiptItemDisplay
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal QtyReady { get; set; }
        public decimal QtyFisik { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public partial class GoodsReceiptFormWindow : Window
    {
        private readonly List<GoodsReceiptItemDisplay> _items = new();

        public GoodsReceiptFormWindow()
        {
            InitializeComponent();
            Loaded += GoodsReceiptFormWindow_Loaded;
        }

        private async void GoodsReceiptFormWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var products = await localDb.Table<LocalProduct>().ToListAsync();

            _items.Clear();
            foreach (var product in products)
            {
                _items.Add(new GoodsReceiptItemDisplay
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    QtyReady = 0,
                    QtyFisik = 0
                });
            }

            TabelItem.ItemsSource = _items;
            InputTanggal.SelectedDate = DateTime.Today;
            TeksNomor.Text = $"Nomor: {DateTime.Now:ddMMyy-HHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
        }

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                var receiptId = Guid.NewGuid().ToString();
                var receiptNumber = TeksNomor.Text.Replace("Nomor: ", string.Empty);

                await localDb.RunInTransactionAsync(conn =>
                {
                    var receipt = new LocalGoodsReceipt
                    {
                        Id = receiptId,
                        ReceiptNumber = receiptNumber,
                        ReceiptDate = InputTanggal.SelectedDate ?? DateTime.Today,
                        Notes = InputCatatan.Text,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsSynced = false
                    };
                    conn.Insert(receipt);

                    foreach (var item in _items.Where(i => i.QtyReady > 0 || i.QtyFisik > 0))
                    {
                        var detail = new LocalGoodsReceiptDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            ReceiptId = receiptId,
                            ProductId = item.ProductId,
                            QtyReady = item.QtyReady,
                            QtyFisik = item.QtyFisik,
                            Notes = item.Notes
                        };
                        conn.Insert(detail);

                        var product = conn.Find<LocalProduct>(item.ProductId);
                        if (product is null) continue;

                        product.StokReady += item.QtyReady;
                        product.StokFisik += item.QtyFisik;
                        product.UpdatedAt = DateTime.Now;
                        product.IsSynced = false;
                        conn.Update(product);
                    }
                });

                DialogHelper.ShowInfo("Barang masuk tersimpan lokal.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menyimpan Barang Masuk");
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
