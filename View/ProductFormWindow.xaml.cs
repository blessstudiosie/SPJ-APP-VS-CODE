using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Globalization;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class ProductFormWindow : Window
    {
        private readonly bool _isEditMode;
        private readonly LocalProduct? _existingProduct;
        private bool _isFormatting = false;

        public ProductFormWindow(LocalProduct? existingProduct = null)
        {
            InitializeComponent();
            _existingProduct = existingProduct;
            LoadDropdownOptions();

            if (existingProduct != null)
            {
                _isEditMode = true;

                InputName.Text = existingProduct.Name;
                InputSatuan.Text = existingProduct.Satuan;
                InputSatuanBesar.Text = existingProduct.SatuanBesar;
                InputQtyRatio.Text = existingProduct.QtyRatio.ToString();
                InputHargaR.Text = existingProduct.HargaR.ToString();
                InputHargaSg.Text = existingProduct.HargaSg.ToString();
                InputHargaG.Text = existingProduct.HargaG.ToString();
                InputHargaP.Text = existingProduct.HargaP.ToString();
                InputDescription.Text = existingProduct.Description;

                SetStatusSelection(existingProduct.Status);

                Title = "Edit Produk";
            }
            else
            {
                InputSatuan.Text = "PCS";
                InputSatuanBesar.Text = "DUS";
                InputQtyRatio.Text = "1";
                SetStatusSelection("Y");
                Title = "Tambah Produk";

                GenerateDescription();
            }
        }

        private async void LoadDropdownOptions()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var products = await localDb.Table<LocalProduct>().ToListAsync();

            InputKategori.ItemsSource = products
                .Where(p => !string.IsNullOrWhiteSpace(p.Kategori))
                .Select(p => p.Kategori).Distinct().OrderBy(k => k).ToList();

            InputSatuanBesar.ItemsSource = products
                .Where(p => !string.IsNullOrWhiteSpace(p.SatuanBesar))
                .Select(p => p.SatuanBesar).Distinct().OrderBy(s => s).ToList();

            if (_existingProduct != null)
            {
                InputKategori.SelectedItem = _existingProduct.Kategori;
                InputSatuanBesar.SelectedItem = _existingProduct.SatuanBesar;
            }
        }

        private void SetStatusSelection(string? status)
        {
            foreach (ComboBoxItem item in InputStatus.Items)
            {
                if ((string)item.Content == status)
                {
                    InputStatus.SelectedItem = item;
                    return;
                }
            }
            InputStatus.SelectedIndex = 0;
        }

        private void InputHarga_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            if (sender is not TextBox textBox) return;

            _isFormatting = true;

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

            _isFormatting = false;
        }

        private void TombolIsiOtomatis_Click(object sender, RoutedEventArgs e) => GenerateDescription();

        private void GenerateDescription()
        {
            string satuanBesar = string.IsNullOrWhiteSpace(InputSatuanBesar.Text) ? "?" : InputSatuanBesar.Text;
            string satuan = string.IsNullOrWhiteSpace(InputSatuan.Text) ? "?" : InputSatuan.Text;
            string qty = string.IsNullOrWhiteSpace(InputQtyRatio.Text) ? "?" : InputQtyRatio.Text;
            InputDescription.Text = $"1 {satuanBesar} = {qty} {satuan}";
        }

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InputName.Text))
                {
                    DialogHelper.ShowError("Nama produk wajib diisi.");
                    return;
                }

                string hargaRClean = InputHargaR.Text.Replace(".", "");
                string hargaSgClean = InputHargaSg.Text.Replace(".", "");
                string hargaGClean = InputHargaG.Text.Replace(".", "");
                string hargaPClean = InputHargaP.Text.Replace(".", "");

                if (!decimal.TryParse(hargaRClean, out decimal hargaR) ||
                    !decimal.TryParse(hargaSgClean, out decimal hargaSg) ||
                    !decimal.TryParse(hargaGClean, out decimal hargaG) ||
                    !decimal.TryParse(hargaPClean, out decimal hargaP) ||
                    !decimal.TryParse(InputQtyRatio.Text, out decimal qtyRatio))
                {
                    DialogHelper.ShowError("Pastikan semua angka (harga, qty ratio) diisi dengan format angka yang benar.");
                    return;
                }

                var localDb = await LocalDatabaseService.GetConnection();
                string trimmedName = InputName.Text.Trim();

                var duplicate = await localDb.Table<LocalProduct>()
                    .Where(p => p.Name == trimmedName)
                    .FirstOrDefaultAsync();

                if (duplicate != null && (!_isEditMode || duplicate.Id != _existingProduct!.Id))
                {
                    DialogHelper.ShowError("Nama produk sudah dipakai produk lain. Gunakan nama lain.");
                    return;
                }

                string? statusValue = (InputStatus.SelectedItem as ComboBoxItem)?.Content as string ?? "Y";

                var product = new LocalProduct
                {
                    Id = _isEditMode ? _existingProduct!.Id : Guid.NewGuid().ToString(),
                    Name = trimmedName,
                    Kategori = InputKategori.Text,
                    Satuan = InputSatuan.Text,
                    SatuanBesar = InputSatuanBesar.Text,
                    QtyRatio = qtyRatio,
                    StokReady = _existingProduct?.StokReady ?? 0,
                    StokFisik = _existingProduct?.StokFisik ?? 0,
                    HargaR = hargaR,
                    HargaSg = hargaSg,
                    HargaG = hargaG,
                    HargaP = hargaP,
                    Status = statusValue,
                    Description = InputDescription.Text,
                    UpdatedAt = DateTime.Now,
                    IsSynced = false,
                    LastSyncedUpdatedAt = _existingProduct?.LastSyncedUpdatedAt
                };

                await localDb.InsertOrReplaceAsync(product);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menyimpan");
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShortcutSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TombolSimpan_Click(sender, new RoutedEventArgs());
        }

        private void ShortcutClose_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            TombolBatal_Click(sender, new RoutedEventArgs());
        }
    }
}