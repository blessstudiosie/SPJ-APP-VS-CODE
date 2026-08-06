using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public class NotaItemDisplay
    {
        public string Id { get; set; } = "";
        public string ProductId { get; set; } = "";
        public string ProductName { get; set; } = "";
        public decimal Qty { get; set; }
        public string PriceCategory { get; set; } = "R";
        public decimal Price { get; set; }
        public decimal Subtotal { get; set; }
    }

    public partial class NotaFormWindow : Window
    {
        private readonly LocalSale? _existingSale;
        private readonly bool _isEditMode;
        private List<LocalCustomer> _customers = new();
        private List<LocalSalesPerson> _salesPersons = new();
        private List<LocalProduct> _products = new();
        private List<NotaItemDisplay> _items = new();

        private bool _isPriceFormatting = false;
        private bool _isPriceSyncFromCategory = false;

        private string _originalStatus = "SO";
        private List<NotaItemDisplay> _originalItemsSnapshot = new();

        public NotaFormWindow(LocalSale? existingSale = null)
        {
            InitializeComponent();
            _existingSale = existingSale;
            _isEditMode = existingSale != null;
            Loaded += NotaFormWindow_Loaded;
        }

        private async void NotaFormWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await ReloadReferenceData();

            if (_isEditMode)
            {
                TeksNota.Text = $"Nota: {_existingSale!.Nota}";
                InputCustomer.SelectedItem = _customers.FirstOrDefault(c => c.Id == _existingSale.CustomerId);
                InputSales.SelectedItem = _salesPersons.FirstOrDefault(s => s.Id == _existingSale.SalesPersonId);

                var localDb = await LocalDatabaseService.GetConnection();
                var details = await localDb.Table<LocalSalesDetail>()
                    .Where(d => d.SaleId == _existingSale.Id)
                    .ToListAsync();

                _items = details.Select(d => new NotaItemDisplay
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    ProductName = _products.FirstOrDefault(p => p.Id == d.ProductId)?.Name ?? "(produk dihapus)",
                    Qty = d.Qty,
                    PriceCategory = d.PriceCategory,
                    Price = d.Price,
                    Subtotal = d.Subtotal
                }).ToList();

                RefreshItemTable();
                UpdateStatusBadge(_existingSale.Status);
                UpdatePaymentInfo();

                _originalStatus = _existingSale.Status;
                _originalItemsSnapshot = _items.Select(i => new NotaItemDisplay
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Qty = i.Qty,
                    PriceCategory = i.PriceCategory,
                    Price = i.Price,
                    Subtotal = i.Subtotal
                }).ToList();

                TombolCetak.IsEnabled = _existingSale.Status == "SO";
                TombolPembayaran.IsEnabled = _existingSale.Status == "TEMPO";
                SetItemEditingState();
                TombolHapusNota.IsEnabled = CanEditItems();
            }
            else
            {
                TeksNota.Text = "Nota Baru (nomor akan dibuat otomatis)";
                UpdateStatusBadge("SO");
                UpdatePaymentInfo();
                TombolPembayaran.IsEnabled = false;
                TombolUnduhGambar.IsEnabled = false;
                TombolCetak.IsEnabled = false;
                TombolHapusNota.IsEnabled = false;
            }
        }

        private bool HasItemsChanged()
        {
            string Signature(List<NotaItemDisplay> items) => string.Join("|",
                items.OrderBy(i => i.ProductId)
                     .Select(i => $"{i.ProductId}:{i.Qty}:{i.PriceCategory}:{i.Price}"));

            return Signature(_items) != Signature(_originalItemsSnapshot);
        }

        private bool CanEditItems() => !_isEditMode || _existingSale is null ||
            _existingSale.Status is "SO" or "ON PROSES";

        private void SetItemEditingState()
        {
            bool canEdit = CanEditItems();
            PanelTambahItem.IsEnabled = canEdit;
            TabelItem.IsEnabled = canEdit;
        }

        private async Task ReloadReferenceData()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            _customers = await localDb.Table<LocalCustomer>().ToListAsync();
            _salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
            _products = await localDb.Table<LocalProduct>().ToListAsync();

            InputCustomer.ItemsSource = null;
            InputCustomer.ItemsSource = _customers;

            InputSales.ItemsSource = null;
            InputSales.ItemsSource = _salesPersons;

            InputProduk.ItemsSource = null;
            InputProduk.ItemsSource = _products;
        }

        private void UpdateStatusBadge(string status)
        {
            TeksStatusBadge.Text = $"Status: {status}";
        }

        private void UpdatePaymentInfo()
        {
            if (_existingSale != null)
            {
                TeksPembayaran.Text = $"Sudah Dibayar: {_existingSale.Paid:N0} | Sisa: {_existingSale.Remaining:N0}";
            }
            else
            {
                TeksPembayaran.Text = "";
            }
        }

        private void InputCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InputCustomer.SelectedItem is LocalCustomer customer && !string.IsNullOrEmpty(customer.SalesPersonId))
            {
                InputSales.SelectedItem = _salesPersons.FirstOrDefault(s => s.Id == customer.SalesPersonId);
            }
        }

        private async void TombolTambahCustomerBaru_Click(object sender, RoutedEventArgs e)
        {
            var form = new CustomerFormWindow { Owner = this };
            if (form.ShowDialog() == true)
            {
                await ReloadReferenceData();

                var newest = _customers.OrderByDescending(c => c.UpdatedAt).FirstOrDefault();
                if (newest != null)
                {
                    InputCustomer.SelectedItem = newest;
                }
            }
        }

        // ===== Kategori Harga & Qty =====

        private void InputProduk_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateHargaInfo();
            SuggestPriceCategory();
            ApplyCategoryPriceToTextbox();
        }

        private void InputQty_TextChanged(object sender, TextChangedEventArgs e)
        {
            SuggestPriceCategory();
            ApplyCategoryPriceToTextbox();
        }

        private void InputKategoriHarga_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyCategoryPriceToTextbox();
        }

        private void SuggestPriceCategory()
        {
            if (InputProduk.SelectedItem is not LocalProduct product) return;
            if (!decimal.TryParse(InputQty.Text, out decimal qty)) return;
            if (product.QtyRatio <= 0) return;

            string suggested;
            if (qty < product.QtyRatio / 2)
                suggested = "R";
            else if (qty < product.QtyRatio)
                suggested = "SG";
            else
                suggested = "G";

            SetKategoriSelection(suggested);
        }

        private void SetKategoriSelection(string category)
        {
            foreach (ComboBoxItem item in InputKategoriHarga.Items)
            {
                if ((string)item.Tag == category)
                {
                    if (InputKategoriHarga.SelectedItem != item)
                    {
                        InputKategoriHarga.SelectionChanged -= InputKategoriHarga_SelectionChanged;
                        InputKategoriHarga.SelectedItem = item;
                        InputKategoriHarga.SelectionChanged += InputKategoriHarga_SelectionChanged;
                    }
                    break;
                }
            }
        }

        private void UpdateHargaInfo()
        {
            if (InputProduk.SelectedItem is not LocalProduct product)
            {
                TeksInfoHarga.Text = "";
                return;
            }

            TeksInfoHarga.Text = $"Info harga - R: {product.HargaR:N0} | SG: {product.HargaSg:N0} | G: {product.HargaG:N0} | P: {product.HargaP:N0} (Qty Ratio: {product.QtyRatio:N0} {product.Satuan}/{product.SatuanBesar})";
        }

        private void ApplyCategoryPriceToTextbox()
        {
            if (InputProduk.SelectedItem is not LocalProduct product) return;

            string category = (InputKategoriHarga.SelectedItem as ComboBoxItem)?.Tag as string ?? "R";
            decimal price = GetPriceForCategory(product, category);

            _isPriceSyncFromCategory = true;
            InputHarga.Text = price.ToString("N0", new CultureInfo("id-ID"));
            _isPriceSyncFromCategory = false;
        }

        private decimal GetPriceForCategory(LocalProduct product, string category) => category switch
        {
            "R" => product.HargaR,
            "SG" => product.HargaSg,
            "G" => product.HargaG,
            "P" => product.HargaP,
            _ => product.HargaR
        };

        // ===== Textbox Harga (format ribuan + penentuan kategori berdasarkan rentang) =====

        private void InputHarga_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPriceFormatting) return;
            if (sender is not TextBox textBox) return;

            _isPriceFormatting = true;

            int caretPosition = textBox.CaretIndex;
            int lengthBefore = textBox.Text.Length;

            string digitsOnly = new string(textBox.Text.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(digitsOnly))
            {
                textBox.Text = "";
            }
            else
            {
                if (long.TryParse(digitsOnly, out long value))
                {
                    textBox.Text = value.ToString("N0", new CultureInfo("id-ID"));
                }
            }

            int lengthAfter = textBox.Text.Length;
            int newCaretPosition = caretPosition + (lengthAfter - lengthBefore);
            textBox.CaretIndex = Math.Max(0, Math.Min(newCaretPosition, textBox.Text.Length));

            _isPriceFormatting = false;

            if (!_isPriceSyncFromCategory)
            {
                MatchPriceToCategory();
            }
        }

        private void MatchPriceToCategory()
        {
            if (InputProduk.SelectedItem is not LocalProduct product) return;

            string cleanText = InputHarga.Text.Replace(".", "");
            if (!decimal.TryParse(cleanText, out decimal typedPrice)) return;

            string matchedCategory;
            if (typedPrice >= product.HargaR)
                matchedCategory = "R";
            else if (typedPrice >= product.HargaSg)
                matchedCategory = "SG";
            else if (typedPrice >= product.HargaG)
                matchedCategory = "G";
            else if (typedPrice >= product.HargaP)
                matchedCategory = "P";
            else
                matchedCategory = "P";

            SetKategoriSelection(matchedCategory);
        }

        // ===== Item =====

        private void TombolTambahItem_Click(object sender, RoutedEventArgs e)
        {
            if (InputProduk.SelectedItem is not LocalProduct product)
            {
                DialogHelper.ShowError("Pilih produk terlebih dahulu.");
                return;
            }

            if (!decimal.TryParse(InputQty.Text, out decimal qty) || qty <= 0)
            {
                DialogHelper.ShowError("Qty harus angka positif.");
                return;
            }

            string cleanHarga = InputHarga.Text.Replace(".", "");
            if (!decimal.TryParse(cleanHarga, out decimal price))
            {
                DialogHelper.ShowError("Harga jual tidak valid.");
                return;
            }

            if (price < product.HargaP)
            {
                DialogHelper.ShowError($"Harga jual tidak boleh di bawah harga Partai (minimal {product.HargaP:N0}).");
                return;
            }

            string category = (InputKategoriHarga.SelectedItem as ComboBoxItem)?.Tag as string ?? "R";

            var item = new NotaItemDisplay
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = product.Id,
                ProductName = product.Name,
                Qty = qty,
                PriceCategory = category,
                Price = price,
                Subtotal = price * qty
            };

            _items.Add(item);
            RefreshItemTable();
        }

        private void TombolHapusItem_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEditItems())
            {
                DialogHelper.ShowError("Detail nota tidak dapat diubah setelah nota masuk pengiriman atau selesai.");
                return;
            }

            if (sender is Button btn && btn.Tag is NotaItemDisplay item)
            {
                _items.Remove(item);
                RefreshItemTable();
            }
        }

        private void RefreshItemTable()
        {
            TabelItem.ItemsSource = null;
            TabelItem.ItemsSource = _items;

            decimal total = _items.Sum(i => i.Subtotal);
            TeksTotal.Text = $"Total: {total:N0}";
        }

        // ===== Simpan =====

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CanEditItems() && HasItemsChanged())
                {
                    DialogHelper.ShowError("Detail nota tidak dapat diubah setelah nota masuk pengiriman atau selesai.");
                    return;
                }

                if (InputCustomer.SelectedItem is not LocalCustomer customer)
                {
                    DialogHelper.ShowError("Pilih customer terlebih dahulu.");
                    return;
                }

                if (_items.Count == 0)
                {
                    DialogHelper.ShowError("Tambahkan minimal 1 item.");
                    return;
                }

                var localDb = await LocalDatabaseService.GetConnection();
                string saleId = _isEditMode ? _existingSale!.Id : Guid.NewGuid().ToString();
                string nota = _isEditMode ? _existingSale!.Nota : GenerateNotaNumber();
                decimal total = _items.Sum(i => i.Subtotal);
                decimal paid = _existingSale?.Paid ?? 0;

                string newStatus = _existingSale?.Status ?? "SO";

                if (_isEditMode && _originalStatus == "ON PROSES" && HasItemsChanged())
{
    await localDb.RunInTransactionAsync(conn =>
    {
        foreach (var oldItem in _originalItemsSnapshot)
        {
            var product = conn.Table<LocalProduct>().FirstOrDefault(p => p.Id == oldItem.ProductId);
            if (product != null)
            {
                product.StokReady += oldItem.Qty;
                product.UpdatedAt = DateTime.Now;
                product.IsSynced = false;
                conn.Update(product);
            }
        }
    });

    newStatus = "SO";
    DialogHelper.ShowInfo("Item berubah - status nota dikembalikan ke SO dan stok yang sempat terpotong sudah dikembalikan. Cetak ulang untuk memproses nota ini kembali.");
}

                var sale = new LocalSale
                {
                    Id = saleId,
                    Nota = nota,
                    CustomerId = customer.Id,
                    OrderDate = _existingSale?.OrderDate ?? DateTime.Now,
                    DeliveryDate = _existingSale?.DeliveryDate,
                    Status = newStatus,
                    SalesPersonId = (InputSales.SelectedItem as LocalSalesPerson)?.Id,
                    Total = total,
                    Paid = paid,
                    Remaining = total - paid,
                    Description = _existingSale?.Description,
                    UpdatedAt = DateTime.Now,
                    IsSynced = false,
                    LastSyncedUpdatedAt = _existingSale?.LastSyncedUpdatedAt
                };

                await localDb.RunInTransactionAsync(conn =>
{
    conn.InsertOrReplace(sale);

    if (_isEditMode)
    {
        var oldDetails = conn.Table<LocalSalesDetail>().Where(d => d.SaleId == saleId).ToList();
        foreach (var old in oldDetails)
        {
            conn.Delete(old);
        }
    }

    foreach (var item in _items)
    {
        var detail = new LocalSalesDetail
        {
            Id = item.Id,
            SaleId = saleId,
            ProductId = item.ProductId,
            Qty = item.Qty,
            Price = item.Price,
            PriceCategory = item.PriceCategory,
            Subtotal = item.Subtotal
        };
        conn.InsertOrReplace(detail);
    }
});

                DialogHelper.ShowInfo("Nota berhasil disimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menyimpan");
            }
        }

        private string GenerateNotaNumber()
        {
            return DateTime.Now.ToString("ddMMyy-HHmmss");
        }

        // ===== Cetak =====

        private async void TombolCetak_Click(object sender, RoutedEventArgs e)
{
    if (!_isEditMode)
    {
        DialogHelper.ShowError("Simpan nota terlebih dahulu sebelum mencetak.");
        return;
    }

    if (_existingSale!.Status != "SO")
    {
        DialogHelper.ShowError("Nota ini sudah tidak berstatus SO, tidak bisa dicetak ulang lewat sini.");
        return;
    }

    bool confirm = DialogHelper.ShowConfirm(
        "Mencetak nota akan mengubah status menjadi ON PROSES dan mengurangi Stok Ready produk. Tindakan ini tidak bisa dibatalkan. Lanjutkan?",
        "Konfirmasi Cetak");

    if (!confirm) return;

    try
    {
        var localDb = await LocalDatabaseService.GetConnection();

        await localDb.RunInTransactionAsync(conn =>
        {
            // Semua langkah di dalam sini dijamin: semua berhasil, atau semua dibatalkan (rollback)
            foreach (var item in _items)
            {
                var product = conn.Table<LocalProduct>().FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    product.StokReady -= item.Qty;
                    product.UpdatedAt = DateTime.Now;
                    product.IsSynced = false;
                    conn.Update(product);
                }
            }

            _existingSale.Status = "ON PROSES";
            _existingSale.UpdatedAt = DateTime.Now;
            _existingSale.IsSynced = false;
            conn.Update(_existingSale);
        });

        await ActivityLogService.LogAsync("PRINT_NOTA", $"Nota '{_existingSale.Nota}' dicetak dan status diubah ke ON PROSES.");

        DialogHelper.ShowInfo("Status nota diubah menjadi ON PROSES, dan Stok Ready produk sudah dikurangi. (Cetak fisik belum diimplementasikan)");

        DialogResult = true;
        Close();
    }
    catch (Exception ex)
    {
        DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal memperbarui status");
    }
}

        // ===== Pembayaran =====

        private void TombolPembayaran_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                DialogHelper.ShowError("Simpan nota terlebih dahulu sebelum mencatat pembayaran.");
                return;
            }

            if (_existingSale!.Status != "TEMPO")
            {
                DialogHelper.ShowError("Pembayaran hanya dapat dicatat untuk nota berstatus TEMPO.");
                return;
            }

            var form = new PaymentFormWindow(_existingSale!) { Owner = this };
            if (form.ShowDialog() == true)
            {
                UpdateStatusBadge(_existingSale!.Status);
                UpdatePaymentInfo();
            }
        }

        // ===== Unduh Gambar =====

        private void TombolUnduhGambar_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                DialogHelper.ShowError("Simpan nota terlebih dahulu sebelum mengunduh gambar.");
                return;
            }

            try
            {
                var customer = InputCustomer.SelectedItem as LocalCustomer;
                var sales = InputSales.SelectedItem as LocalSalesPerson;

                var visual = BuildNotaVisual(customer, sales);

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PNG Image|*.png",
                    FileName = $"Nota_{_existingSale!.Nota}.png"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    SaveVisualAsPng(visual, saveDialog.FileName);
                    DialogHelper.ShowInfo("Gambar nota berhasil disimpan.");
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Membuat Gambar");
            }
        }

        private FrameworkElement BuildNotaVisual(LocalCustomer? customer, LocalSalesPerson? sales)
        {
            var container = new StackPanel
            {
                Width = 500,
                Background = Brushes.White,
                Margin = new Thickness(20)
            };

            container.Children.Add(new TextBlock
            {
                Text = "CV. Sarana Prima Jaya",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 2)
            });
            container.Children.Add(new TextBlock
            {
                Text = "Jl. Gora No. 39 Selagalas, Mataram, Lombok, NTB",
                FontSize = 11,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 10)
            });

            container.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 10) });

            container.Children.Add(new TextBlock { Text = $"No Nota: {_existingSale!.Nota}", FontWeight = FontWeights.Bold, FontSize = 14 });
            container.Children.Add(new TextBlock { Text = $"Tanggal: {_existingSale.OrderDate:dd/MM/yyyy HH:mm}", Margin = new Thickness(0, 2, 0, 0) });
            container.Children.Add(new TextBlock { Text = $"Customer: {customer?.Name ?? "-"}", Margin = new Thickness(0, 2, 0, 0) });
            container.Children.Add(new TextBlock { Text = $"Sales: {sales?.Name ?? "-"}", Margin = new Thickness(0, 2, 0, 0) });
            container.Children.Add(new TextBlock { Text = $"Status: {_existingSale.Status}", Margin = new Thickness(0, 2, 0, 10) });

            container.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 10) });

            foreach (var item in _items)
            {
                var itemPanel = new Grid { Margin = new Thickness(0, 0, 0, 5) };
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var namaQty = new TextBlock
                {
                    Text = $"{item.ProductName} ({item.Qty:N0} x {item.Price:N0}) [{item.PriceCategory}]",
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetColumn(namaQty, 0);

                var subtotalText = new TextBlock { Text = item.Subtotal.ToString("N0"), FontWeight = FontWeights.SemiBold };
                Grid.SetColumn(subtotalText, 1);

                itemPanel.Children.Add(namaQty);
                itemPanel.Children.Add(subtotalText);
                container.Children.Add(itemPanel);
            }

            container.Children.Add(new Separator { Margin = new Thickness(0, 10, 0, 10) });

            decimal total = _items.Sum(i => i.Subtotal);
            container.Children.Add(new TextBlock
            {
                Text = $"TOTAL: {total:N0}",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            container.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            container.Arrange(new Rect(container.DesiredSize));

            return container;
        }

        private void SaveVisualAsPng(FrameworkElement visual, string filePath)
        {
            var renderBitmap = new RenderTargetBitmap(
                (int)visual.ActualWidth,
                (int)visual.ActualHeight,
                96, 96, PixelFormats.Pbgra32);

            renderBitmap.Render(visual);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            using var fileStream = new FileStream(filePath, FileMode.Create);
            encoder.Save(fileStream);
        }

        // ===== Hapus =====

        private async void TombolHapusNota_Click(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                DialogResult = false;
                Close();
                return;
            }

            if (!CanEditItems())
            {
                DialogHelper.ShowError("Nota tidak dapat dihapus setelah masuk pengiriman atau selesai.");
                return;
            }

            bool confirm = DialogHelper.ShowConfirm($"Yakin ingin menghapus nota '{_existingSale!.Nota}'? Tindakan ini tidak bisa dibatalkan.", "Konfirmasi Hapus");
            if (!confirm) return;

            try
            {
                var localDb = await LocalDatabaseService.GetConnection();

                var details = await localDb.Table<LocalSalesDetail>()
                    .Where(d => d.SaleId == _existingSale.Id)
                    .ToListAsync();
                await localDb.RunInTransactionAsync(conn =>
                {
                    // Saat ON PROSES stok ready sudah dikurangi ketika nota dicetak.
                    // Menghapus nota harus mengembalikan reservasi stok tersebut.
                    if (_existingSale.Status == "ON PROSES")
                    {
                        foreach (var detail in details)
                        {
                            var product = conn.Table<LocalProduct>().FirstOrDefault(p => p.Id == detail.ProductId);
                            if (product is null) continue;
                            product.StokReady += detail.Qty;
                            product.UpdatedAt = DateTime.Now;
                            product.IsSynced = false;
                            conn.Update(product);
                        }
                    }

                    foreach (var detail in details)
                        conn.Delete(detail);
                    conn.Delete(_existingSale);
                });

                await ActivityLogService.LogAsync("DELETE_NOTA", $"Nota '{_existingSale.Nota}' dihapus.");

                DialogHelper.ShowInfo("Nota dihapus dari lokal. Perlu sync manual untuk menghapus juga dari server (fitur ini akan disempurnakan).");

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menghapus");
            }
        }

        private void TombolTutup_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShortcutClose_Executed(object sender, ExecutedRoutedEventArgs e) => TombolTutup_Click(sender, new RoutedEventArgs());

        private void ShortcutSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TombolSimpan_Click(sender, new RoutedEventArgs());
        }
    }
}
