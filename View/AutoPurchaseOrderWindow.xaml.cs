using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public class AutoPoItemDisplay
    {
        public bool IsSelected { get; set; } = true;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal QtyRatio { get; set; }
        public decimal QtyCalculated { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public partial class AutoPurchaseOrderWindow : Window
    {
        private readonly List<AutoPoItemDisplay> _allItems = new();
        private List<string> _distinctProductNames = new();
        private List<string> _distinctCategories = new();
        private bool _isSelectingSuggestion = false;

        public AutoPurchaseOrderWindow()
        {
            InitializeComponent();
            Loaded += AutoPurchaseOrderWindow_Loaded;
        }

        private async void AutoPurchaseOrderWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                var products = await localDb.Table<LocalProduct>().ToListAsync();

                _allItems.Clear();
                foreach (var product in products)
                {
                    _allItems.Add(new AutoPoItemDisplay
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Category = product.Kategori ?? "Umum",
                        QtyRatio = product.QtyRatio > 0 ? product.QtyRatio : 1,
                        QtyCalculated = 0
                    });
                }

                _distinctProductNames = _allItems
                    .Select(i => i.ProductName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(n => n)
                    .ToList();

                _distinctCategories = _allItems
                    .Select(i => i.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal memuat produk PO: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            string nameQuery = TxtCariNamaBarang.Text.Trim();
            string catQuery = TxtCariKategori.Text.Trim();

            var filtered = _allItems.Where(item =>
                (string.IsNullOrWhiteSpace(nameQuery) || item.ProductName.Contains(nameQuery, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrWhiteSpace(catQuery) || item.Category.Contains(catQuery, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            TabelItem.ItemsSource = filtered;
        }

        #region Autocomplete Nama Barang Event Handlers

        private void TxtCariNamaBarang_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowNamaBarangSuggestions();
        }

        private void TxtCariNamaBarang_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isSelectingSuggestion)
            {
                ShowNamaBarangSuggestions();
                ApplyFilters();
            }
        }

        private void ShowNamaBarangSuggestions()
        {
            string query = TxtCariNamaBarang.Text.Trim();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var matches = _distinctProductNames
                    .Where(n => n.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(12)
                    .ToList();

                if (matches.Any())
                {
                    ListNamaBarangSuggestions.ItemsSource = matches;
                    PopupNamaBarangSuggestions.IsOpen = true;
                    return;
                }
            }
            PopupNamaBarangSuggestions.IsOpen = false;
        }

        private void TxtCariNamaBarang_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && PopupNamaBarangSuggestions.IsOpen)
            {
                ListNamaBarangSuggestions.Focus();
                if (ListNamaBarangSuggestions.Items.Count > 0)
                {
                    ListNamaBarangSuggestions.SelectedIndex = 0;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PopupNamaBarangSuggestions.IsOpen = false;
            }
        }

        private void ListNamaBarangSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListNamaBarangSuggestions.SelectedItem is string selectedName)
            {
                _isSelectingSuggestion = true;
                TxtCariNamaBarang.Text = selectedName;
                TxtCariNamaBarang.CaretIndex = selectedName.Length;
                PopupNamaBarangSuggestions.IsOpen = false;
                _isSelectingSuggestion = false;
                ApplyFilters();
            }
        }

        #endregion

        #region Autocomplete Kategori Event Handlers

        private void TxtCariKategori_GotFocus(object sender, RoutedEventArgs e)
        {
            ShowKategoriSuggestions();
        }

        private void TxtCariKategori_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isSelectingSuggestion)
            {
                ShowKategoriSuggestions();
                ApplyFilters();
            }
        }

        private void ShowKategoriSuggestions()
        {
            string query = TxtCariKategori.Text.Trim();
            if (!string.IsNullOrWhiteSpace(query))
            {
                var matches = _distinctCategories
                    .Where(c => c.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(12)
                    .ToList();

                if (matches.Any())
                {
                    ListKategoriSuggestions.ItemsSource = matches;
                    PopupKategoriSuggestions.IsOpen = true;
                    return;
                }
            }
            else
            {
                if (_distinctCategories.Any())
                {
                    ListKategoriSuggestions.ItemsSource = _distinctCategories.Take(12).ToList();
                    PopupKategoriSuggestions.IsOpen = true;
                    return;
                }
            }
            PopupKategoriSuggestions.IsOpen = false;
        }

        private void TxtCariKategori_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && PopupKategoriSuggestions.IsOpen)
            {
                ListKategoriSuggestions.Focus();
                if (ListKategoriSuggestions.Items.Count > 0)
                {
                    ListKategoriSuggestions.SelectedIndex = 0;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                PopupKategoriSuggestions.IsOpen = false;
            }
        }

        private void ListKategoriSuggestions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListKategoriSuggestions.SelectedItem is string selectedCat)
            {
                _isSelectingSuggestion = true;
                TxtCariKategori.Text = selectedCat;
                TxtCariKategori.CaretIndex = selectedCat.Length;
                PopupKategoriSuggestions.IsOpen = false;
                _isSelectingSuggestion = false;
                ApplyFilters();
            }
        }

        #endregion

        private void TombolResetFilter_Click(object sender, RoutedEventArgs e)
        {
            _isSelectingSuggestion = true;
            TxtCariNamaBarang.Text = string.Empty;
            TxtCariKategori.Text = string.Empty;
            PopupNamaBarangSuggestions.IsOpen = false;
            PopupKategoriSuggestions.IsOpen = false;
            _isSelectingSuggestion = false;
            ApplyFilters();
        }

        private void TombolHitung_Click(object sender, RoutedEventArgs e) => CalculateRatios();

        private void InputTotalTarget_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateRatios();
        }

        private void CalculateRatios()
        {
            if (!decimal.TryParse(InputTotalTarget.Text.Replace(".", string.Empty), out decimal totalTarget) || totalTarget <= 0)
            {
                foreach (var item in _allItems) item.QtyCalculated = 0;
                TabelItem.Items.Refresh();
                return;
            }

            var selectedItems = _allItems.Where(i => i.IsSelected).ToList();
            
            foreach (var item in _allItems.Where(i => !i.IsSelected)) item.QtyCalculated = 0;

            if (selectedItems.Count == 0)
            {
                TabelItem.Items.Refresh();
                return;
            }

            decimal totalRatioWeight = selectedItems.Sum(i => i.QtyRatio > 0 ? i.QtyRatio : 1);
            if (totalRatioWeight <= 0) return;

            var calculatedItems = selectedItems.Select(item => new
            {
                DisplayItem = item,
                ExactQty = ((item.QtyRatio > 0 ? item.QtyRatio : 1) / totalRatioWeight) * totalTarget
            }).ToList();

            foreach (var calc in calculatedItems)
            {
                calc.DisplayItem.QtyCalculated = Math.Floor(calc.ExactQty);
            }

            decimal currentSum = calculatedItems.Sum(c => c.DisplayItem.QtyCalculated);
            int remainder = (int)(totalTarget - currentSum);

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
                var selectedItems = _allItems.Where(i => i.IsSelected && i.QtyCalculated > 0).ToList();

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
