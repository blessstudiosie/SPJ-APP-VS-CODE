using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class SalesPersonListPage : UserControl, IRefreshablePage
    {
        private List<LocalSalesPerson> _allSales = new();

        public SalesPersonListPage()
        {
            InitializeComponent();
            CheckRolePermissions();
            LoadDataFromLocal();
        }

        public void RefreshData()
        {
            CheckRolePermissions();
            LoadDataFromLocal();
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

        private void CheckRolePermissions()
        {
            bool hasAccess = CanManageSalesMaster();
            var currentUser = CurrentUserService.LoggedInUser;

            if (hasAccess)
            {
                BadgeAksesManager.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#ECFDF5"));
                BadgeAksesManager.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#10B981"));
                TeksAksesLevel.Text = $"🛡️ Akses Full: {currentUser?.Role ?? "MANAGER"} ({currentUser?.Name})";
                TeksAksesLevel.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#047857"));
                TombolTambah.IsEnabled = true;
            }
            else
            {
                BadgeAksesManager.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FEF2F2"));
                BadgeAksesManager.BorderBrush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#EF4444"));
                TeksAksesLevel.Text = $"🔒 Akses Dibatasi: {currentUser?.Role ?? "SALES"} (Khusus Manager / Owner)";
                TeksAksesLevel.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#B91C1C"));
                TombolTambah.IsEnabled = false;
            }
        }

        private async void LoadDataFromLocal()
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                _allSales = await localDb.Table<LocalSalesPerson>().ToListAsync();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal memuat data sales: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            string keyword = InputCari?.Text?.Trim().ToLowerInvariant() ?? "";

            var filtered = string.IsNullOrWhiteSpace(keyword)
                ? _allSales
                : _allSales.Where(s => s.Name.ToLowerInvariant().Contains(keyword) ||
                                       (s.Phone != null && s.Phone.ToLowerInvariant().Contains(keyword)) ||
                                       (s.Email != null && s.Email.ToLowerInvariant().Contains(keyword)) ||
                                       (s.Role != null && s.Role.ToLowerInvariant().Contains(keyword))).ToList();

            TabelSales.ItemsSource = filtered;
            TeksJumlah.Text = $"Menampilkan: {filtered.Count} dari {_allSales.Count} sales";
        }

        private void InputCari_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void TombolTambah_Click(object sender, RoutedEventArgs e)
        {
            if (!CanManageSalesMaster())
            {
                DialogHelper.ShowError("Hanya role MANAGER dan OWNER yang berhak menambah data sales baru.");
                return;
            }

            var form = new SalesPersonFormWindow { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                LoadDataFromLocal();
                TeksStatus.Text = "Data sales baru disimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
            }
        }

        private void TombolEditRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is LocalSalesPerson selected)
            {
                OpenEditForm(selected);
            }
        }

        private async void TombolHapusRow_Click(object sender, RoutedEventArgs e)
        {
            if (!CanManageSalesMaster())
            {
                DialogHelper.ShowError("Hanya role MANAGER dan OWNER yang berhak menghapus data sales.");
                return;
            }

            if (sender is Button btn && btn.DataContext is LocalSalesPerson selected)
            {
                await DeleteSalesPersonAsync(selected);
            }
        }

        private void TabelSales_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TabelSales.SelectedItem is LocalSalesPerson selected)
            {
                OpenEditForm(selected);
            }
        }

        private void OpenEditForm(LocalSalesPerson selected)
        {
            var form = new SalesPersonFormWindow(selected) { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                LoadDataFromLocal();
                TeksStatus.Text = "Data sales diperbarui. Gunakan menu Sync Sekarang untuk kirim ke server.";
            }
        }

        private async Task DeleteSalesPersonAsync(LocalSalesPerson sales)
        {
            if (string.Equals(sales.Role, "DEVELOPER", StringComparison.OrdinalIgnoreCase) || sales.Name == "blessstudiosie")
            {
                DialogHelper.ShowError("Akun Developer Khusus Sistem tidak dapat dihapus.");
                return;
            }

            bool confirm = DialogHelper.ShowConfirm(
                $"Apakah Anda yakin ingin MENGHAPUS sales '{sales.Name}' ({sales.Role})?\n\nTindakan ini akan menghapus akun sales dari database lokal dan server.",
                "Konfirmasi Hapus Sales");

            if (!confirm) return;

            try
            {
                var db = await LocalDatabaseService.GetConnection();
                await db.DeleteAsync(sales);

                // Hapus juga di remote Supabase jika terkoneksi
                try
                {
                    var supabase = await SupabaseService.GetClient();
                    await supabase.From<SalesPerson>().Where(x => x.Name == sales.Name).Delete();
                }
                catch (Exception exRemote)
                {
                    System.Diagnostics.Debug.WriteLine($"Remote delete failed (akan disinkronkan nanti): {exRemote.Message}");
                }

                await ActivityLogService.LogAsync("DELETE_SALES_PERSON", $"Menghapus data sales '{sales.Name}' ({sales.Role}).");

                DialogHelper.ShowInfo($"Data sales '{sales.Name}' berhasil dihapus.");
                LoadDataFromLocal();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menghapus data sales: {ex.Message}");
            }
        }

    }
}