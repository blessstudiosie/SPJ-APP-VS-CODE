using System.Windows;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public class AutoPoItemDisplay
    {
        public bool IsSelected { get; set; } = true;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal QtyRatio { get; set; }
        public decimal QtyCalculated { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public partial class AutoPurchaseOrderWindow : Window
    {
        private readonly List<AutoPoItemDisplay> _items = new();

        public AutoPurchaseOrderWindow()
        {
            InitializeComponent();
            Loaded += AutoPurchaseOrderWindow_Loaded;
        }

        private async void AutoPurchaseOrderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var products = await localDb.Table<LocalProduct>().ToListAsync();

            _items.Clear();
            foreach (var product in products)
            {
                _items.Add(new AutoPoItemDisplay
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    QtyRatio = product.QtyRatio > 0 ? product.QtyRatio : 1,
                    QtyCalculated = 0
                });
            }

            TabelItem.ItemsSource = _items;
        }

        private void TombolHitung_Click(object sender, RoutedEventArgs e) => CalculateRatios();

        private void InputTotalTarget_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            CalculateRatios();
        }

        private void CalculateRatios()
        {
            if (!decimal.TryParse(InputTotalTarget.Text.Replace(".", string.Empty), out decimal totalTarget) || totalTarget <= 0)
            {
                // Clear previous results if input is invalid
                foreach (var item in _items) item.QtyCalculated = 0;
                TabelItem.Items.Refresh();
                return;
            }

            var selectedItems = _items.Where(i => i.IsSelected).ToList();
            
            // Clear results for non-selected items
            foreach (var item in _items.Where(i => !i.IsSelected)) item.QtyCalculated = 0;

            if (selectedItems.Count == 0)
            {
                TabelItem.Items.Refresh();
                return;
            }

            decimal totalRatioWeight = selectedItems.Sum(i => i.QtyRatio > 0 ? i.QtyRatio : 1);
            if (totalRatioWeight <= 0) return;

            // --- Largest Remainder Method Implementation ---
            var calculatedItems = selectedItems.Select(item => new
            {
                DisplayItem = item,
                ExactQty = ( (item.QtyRatio > 0 ? item.QtyRatio : 1) / totalRatioWeight) * totalTarget
            }).ToList();

            // 1. Assign the floor of the exact quantity
            foreach (var calc in calculatedItems)
            {
                calc.DisplayItem.QtyCalculated = Math.Floor(calc.ExactQty);
            }

            // 2. Calculate the remainder
            decimal currentSum = calculatedItems.Sum(c => c.DisplayItem.QtyCalculated);
            int remainder = (int)(totalTarget - currentSum);

            // 3. Distribute the remainder to items with the largest fractional part
            var itemsToDistribute = calculatedItems
                .OrderByDescending(c => c.ExactQty - c.DisplayItem.QtyCalculated)
                .Take(remainder);

            foreach (var calc in itemsToDistribute)
            {
                calc.DisplayItem.QtyCalculated++;
            }
            
            TabelItem.Items.Refresh();
        }

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                decimal.TryParse(InputTotalTarget.Text.Replace(".", string.Empty), out decimal totalTarget);
                var selectedItems = _items.Where(i => i.IsSelected && i.QtyCalculated > 0).ToList();

                if (selectedItems.Count == 0)
                {
                    DialogHelper.ShowError("Pilih minimal 1 barang dengan qty hasil hitung lebih dari 0.");
                    return;
                }

                var localDb = await LocalDatabaseService.GetConnection();
                string poId = Guid.NewGuid().ToString();
                string poNumber = $"PO-{DateTime.Now:ddMMyy-HHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";

                await localDb.RunInTransactionAsync(conn =>
                {
                    var po = new LocalPurchaseOrder
                    {
                        Id = poId,
                        PurchaseOrderNumber = poNumber,
                        OrderDate = DateTime.Now,
                        Notes = InputCatatan.Text,
                        TotalQty = totalTarget,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        IsSynced = false
                    };
                    conn.Insert(po);

                    foreach (var item in selectedItems)
                    {
                        var detail = new LocalPurchaseOrderDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            PurchaseOrderId = poId,
                            ProductId = item.ProductId,
                            QtyCalculated = item.QtyCalculated,
                            QtyRatio = item.QtyRatio,
                            Notes = item.Notes
                        };
                        conn.Insert(detail);
                    }
                });

                DialogHelper.ShowInfo($"PO Otomatis '{poNumber}' berhasil dibuat.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Membuat PO");
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
