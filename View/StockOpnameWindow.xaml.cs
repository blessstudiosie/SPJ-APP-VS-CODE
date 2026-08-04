using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public class StockOpnameItemDisplay
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal SystemQty { get; set; }
        public decimal ActualQty { get; set; }
        public decimal DifferenceQty => ActualQty - SystemQty;
        public string Notes { get; set; } = string.Empty;
    }

    public partial class StockOpnameWindow : Window
    {
        private readonly List<StockOpnameItemDisplay> _items = new();

        public StockOpnameWindow()
        {
            InitializeComponent();
            Loaded += StockOpnameWindow_Loaded;
        }

        private async void StockOpnameWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var products = await localDb.Table<LocalProduct>().ToListAsync();

            _items.Clear();
            foreach (var product in products)
            {
                _items.Add(new StockOpnameItemDisplay
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SystemQty = product.StokFisik,
                    ActualQty = product.StokFisik
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
                var opnameId = Guid.NewGuid().ToString();
                var opnameNumber = TeksNomor.Text.Replace("Nomor: ", string.Empty);

                await localDb.RunInTransactionAsync(conn =>
                {
                    var opname = new LocalStockOpname
                    {
                        Id = opnameId,
                        OpnameNumber = opnameNumber,
                        OpnameDate = InputTanggal.SelectedDate ?? DateTime.Today,
                        Notes = InputCatatan.Text,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsSynced = false
                    };
                    conn.Insert(opname);

                    foreach (var item in _items)
                    {
                        var detail = new LocalStockOpnameDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            OpnameId = opnameId,
                            ProductId = item.ProductId,
                            SystemQty = item.SystemQty,
                            ActualQty = item.ActualQty,
                            DifferenceQty = item.DifferenceQty,
                            Notes = item.Notes
                        };
                        conn.Insert(detail);

                        var product = conn.Find<LocalProduct>(item.ProductId);
                        if (product is null) continue;

                        product.StokFisik = item.ActualQty;
                        product.StokReady = Math.Max(0, product.StokReady + (item.ActualQty - item.SystemQty));
                        product.UpdatedAt = DateTime.Now;
                        product.IsSynced = false;
                        conn.Update(product);
                    }
                });

                DialogHelper.ShowInfo("Stok opname tersimpan lokal.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menyimpan Stok Opname");
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
