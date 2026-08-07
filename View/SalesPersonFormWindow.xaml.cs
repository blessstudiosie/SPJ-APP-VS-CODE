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
        private bool _isEditMode;

        private readonly LocalSalesPerson? _existing;
        private bool _isFormatting = false;

        public SalesPersonFormWindow(LocalSalesPerson? existing = null)
        {
            InitializeComponent();
            _existing = existing;

            Loaded += SalesPersonFormWindow_Loaded;
        }

        private bool CanManageSalesMaster()
        {
            var user = CurrentUserService.LoggedInUser;
            if (user == null) return false;

            var role = user.Role?.ToUpperInvariant() ?? "";
            var name = user.Name ?? "";

            return role == "MANAGER" || role == "OWNER" || role == "DEVELOPER" || role == "ADMIN" ||
                   string.Equals(name, "blessstudiosie", StringComparison.OrdinalIgnoreCase);
        }

        private void SalesPersonFormWindow_Loaded(object sender, RoutedEventArgs e)
        {
            bool hasFullAccess = CanManageSalesMaster();

            if (_existing != null)
            {
                _isEditMode = true;
                InputName.Text = _existing.Name;
                InputPhone.Text = _existing.Phone;
                InputEmail.Text = _existing.Email;
                InputTargetOmset.Text = _existing.TargetOmset.ToString();
                SetRoleSelection(_existing.Role);
                Title = "Edit Sales";

                if (hasFullAccess)
                {
                    TombolHapus.Visibility = Visibility.Visible;
                }
            }
            else
            {
                SetRoleSelection("SALES");
                Title = "Tambah Sales";
                TombolHapus.Visibility = Visibility.Collapsed;
            }

            if (!hasFullAccess)
            {
                InputName.IsEnabled = false;
                InputPhone.IsEnabled = false;
                InputEmail.IsEnabled = false;
                InputRole.IsEnabled = false;
                InputTargetOmset.IsEnabled = false;
                TombolSimpan.IsEnabled = false;
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
            if (!CanManageSalesMaster())
            {
                DialogHelper.ShowError("Hanya role MANAGER dan OWNER yang berhak mengedit atau menambah data sales.");
                return;
            }

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
                    Password = _existing?.Password,
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

        private async void TombolHapus_Click(object sender, RoutedEventArgs e)
        {
            if (!CanManageSalesMaster())
            {
                DialogHelper.ShowError("Hanya role MANAGER dan OWNER yang berhak menghapus data sales.");
                return;
            }

            if (!_isEditMode || _existing == null) return;

            if (string.Equals(_existing.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase) || _existing.Name == "blessstudiosie")
            {
                DialogHelper.ShowError("Akun Developer Khusus Sistem tidak dapat dihapus.");
                return;
            }

            bool confirm = DialogHelper.ShowConfirm(
                $"Apakah Anda yakin ingin MENGHAPUS sales '{_existing.Name}' ({_existing.Role})?\n\nTindakan ini akan menghapus akun sales dari database lokal dan server.",
                "Konfirmasi Hapus Sales");

            if (!confirm) return;

            try
            {
                var db = await LocalDatabaseService.GetConnection();
                await db.DeleteAsync(_existing);

                // Hapus juga di remote Supabase jika terkoneksi
                try
                {
                    var supabase = await SupabaseService.GetClient();
                    await supabase.From<SalesPerson>().Where(x => x.Name == _existing.Name).Delete();
                }
                catch (Exception exRemote)
                {
                    System.Diagnostics.Debug.WriteLine($"Remote delete failed (akan disinkronkan nanti): {exRemote.Message}");
                }

                await ActivityLogService.LogAsync("DELETE_SALES_PERSON", $"Menghapus data sales '{_existing.Name}' ({_existing.Role}).");

                DialogHelper.ShowInfo($"Data sales '{_existing.Name}' berhasil dihapus.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menghapus data sales: {ex.Message}");
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