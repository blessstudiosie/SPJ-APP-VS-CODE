using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class SalesPersonFormWindow : Window
    {
        private readonly bool _isEditMode;
        private readonly LocalSalesPerson? _existing;
        private bool _isFormatting = false;

        public SalesPersonFormWindow(LocalSalesPerson? existing = null)
        {
            InitializeComponent();
            _existing = existing;

            if (existing != null)
            {
                _isEditMode = true;
                InputName.Text = existing.Name;
                InputPhone.Text = existing.Phone;
                InputEmail.Text = existing.Email;
                InputTargetOmset.Text = existing.TargetOmset.ToString();
                SetRoleSelection(existing.Role);
                Title = "Edit Sales";
            }
            else
            {
                SetRoleSelection("SALES");
                Title = "Tambah Sales";
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

        private void SetRoleSelection(string? role)
        {
            foreach (ComboBoxItem item in InputRole.Items)
            {
                if ((string)item.Content == role)
                {
                    InputRole.SelectedItem = item;
                    return;
                }
            }
            InputRole.SelectedIndex = 0;
        }

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(InputName.Text))
                {
                    DialogHelper.ShowError("Nama wajib diisi.");
                    return;
                }

                string targetOmsetClean = InputTargetOmset.Text.Replace(".", "");
                if (!decimal.TryParse(targetOmsetClean, out decimal targetOmset))
                {
                    targetOmset = 0;
                }

                var localDb = await LocalDatabaseService.GetConnection();
                string trimmedName = InputName.Text.Trim();

                var duplicate = await localDb.Table<LocalSalesPerson>()
                    .Where(p => p.Name == trimmedName)
                    .FirstOrDefaultAsync();

                if (duplicate != null && (!_isEditMode || duplicate.Id != _existing!.Id))
                {
                    DialogHelper.ShowError("Nama sales sudah dipakai. Gunakan nama lain.");
                    return;
                }

                string? roleValue = (InputRole.SelectedItem as ComboBoxItem)?.Content as string ?? "SALES";

                var item = new LocalSalesPerson
                {
                    Id = _isEditMode ? _existing!.Id : Guid.NewGuid().ToString(),
                    Name = trimmedName,
                    Phone = InputPhone.Text,
                    Email = InputEmail.Text,
                    TargetOmset = targetOmset,
                    Role = roleValue,
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