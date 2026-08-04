using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class CustomerFormWindow : Window
    {
        private readonly bool _isEditMode;
        private readonly LocalCustomer? _existing;
        private bool _isFormatting = false;

        public CustomerFormWindow(LocalCustomer? existing = null)
        {
            InitializeComponent();
            _existing = existing;
            LoadSalesPersonOptions();

            if (existing != null)
            {
                _isEditMode = true;
                InputName.Text = existing.Name;
                InputOwnerName.Text = existing.OwnerName;
                InputPhone.Text = existing.Phone;
                InputAddress.Text = existing.Address;
                InputLimitPiutang.Text = existing.LimitPiutang.ToString();
                SetJalurSelection(existing.JalurPengiriman);
                Title = "Edit Customer";
            }
            else
            {
                SetJalurSelection("LOBAR-MATARAM");
                Title = "Tambah Customer";
            }
        }

        private void InputAngka_TextChanged(object sender, TextChangedEventArgs e)
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

        private async void LoadSalesPersonOptions()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

            InputSalesPerson.ItemsSource = salesPersons;

            if (_existing != null && !string.IsNullOrEmpty(_existing.SalesPersonId))
            {
                InputSalesPerson.SelectedItem = salesPersons.FirstOrDefault(s => s.Id == _existing.SalesPersonId);
            }
        }

        private void SetJalurSelection(string? jalur)
        {
            foreach (ComboBoxItem item in InputJalur.Items)
            {
                if ((string)item.Content == jalur)
                {
                    InputJalur.SelectedItem = item;
                    return;
                }
            }
            InputJalur.SelectedIndex = 3;
        }

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InputName.Text))
                {
                    DialogHelper.ShowError("Nama toko wajib diisi.");
                    return;
                }

                string limitPiutangClean = InputLimitPiutang.Text.Replace(".", "");
                if (!decimal.TryParse(limitPiutangClean, out decimal limitPiutang))
                {
                    limitPiutang = 0;
                }

                var localDb = await LocalDatabaseService.GetConnection();
                string trimmedName = InputName.Text.Trim();

                var duplicate = await localDb.Table<LocalCustomer>()
                    .Where(c => c.Name == trimmedName)
                    .FirstOrDefaultAsync();

                if (duplicate != null && (!_isEditMode || duplicate.Id != _existing!.Id))
                {
                    DialogHelper.ShowError("Nama customer sudah dipakai. Gunakan nama lain.");
                    return;
                }

                string? jalurValue = (InputJalur.SelectedItem as ComboBoxItem)?.Content as string ?? "LOBAR-MATARAM";
                string? salesPersonId = (InputSalesPerson.SelectedItem as LocalSalesPerson)?.Id;

                var item = new LocalCustomer
                {
                    Id = _isEditMode ? _existing!.Id : Guid.NewGuid().ToString(),
                    Name = trimmedName,
                    OwnerName = InputOwnerName.Text,
                    Phone = InputPhone.Text,
                    Address = InputAddress.Text,
                    JalurPengiriman = jalurValue,
                    Latitude = _existing?.Latitude,
                    Longitude = _existing?.Longitude,
                    SalesPersonId = salesPersonId,
                    LimitPiutang = limitPiutang,
                    UpdatedAt = DateTime.Now,
                    IsSynced = false,
                    LastSyncedUpdatedAt = _existing?.LastSyncedUpdatedAt
                };

                await localDb.InsertOrReplaceAsync(item);

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

        private void ShortcutSave_Executed(object sender, ExecutedRoutedEventArgs e) => TombolSimpan_Click(sender, new RoutedEventArgs());
        private void ShortcutClose_Executed(object sender, ExecutedRoutedEventArgs e) => TombolBatal_Click(sender, new RoutedEventArgs());
    }
}