using System.Windows;
using SPJ_APP.Service;
using SPJ_APP.View.Pages;
using SPJ_APP.View;
using System.Windows.Controls;
using System.Windows.Input;

namespace SPJ_APP
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AreaKonten.Content = new HomePage();
            
            BackgroundSyncService.Instance.SyncStatusChanged += BackgroundSyncService_SyncStatusChanged;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // F5: Refresh current page
            if (e.Key == Key.F5)
            {
                if (AreaKonten.Content is IRefreshablePage refreshablePage)
                {
                    refreshablePage.RefreshData();
                }
            }

            // Ctrl+N: New item (context-sensitive)
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                Window? form = null;
                if (AreaKonten.Content is ProductListPage)
                    form = new ProductFormWindow { Owner = this };
                else if (AreaKonten.Content is NotaListPage)
                    form = new NotaFormWindow { Owner = this };
                else if (AreaKonten.Content is CustomerListPage)
                    form = new CustomerFormWindow { Owner = this };
                else if (AreaKonten.Content is SalesPersonListPage)
                    form = new SalesPersonFormWindow { Owner = this };
                
                if (form != null && form.ShowDialog() == true)
                {
                    (AreaKonten.Content as IRefreshablePage)?.RefreshData();
                }
            }
        }
        
        private void BackgroundSyncService_SyncStatusChanged(object? sender, SyncStatusEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = e.Message;
                SyncProgressBar.Visibility = e.IsSyncing ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentUser = CurrentUserService.LoggedInUser;
                Title = $"SPJ App - Selamat Datang, {currentUser?.Name ?? "User"}!";

                // Tampilkan Developer Tools untuk Akun Developer / Admin
                var currentUserRole = currentUser?.Role?.ToUpperInvariant();
                var currentUserName = currentUser?.Name;
                if (currentUserRole == "DEVELOPER" ||
                    string.Equals(currentUserName, "blessstudiosie", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(currentUserName, "Developer", StringComparison.OrdinalIgnoreCase) ||
                    currentUserRole == "ADMIN")
                {
                    MenuDeveloperTools.Visibility = Visibility.Visible;
                }

                // Default tampilan utama adalah Beranda (HomePage)
                AreaKonten.Content = new HomePage();
                SetActiveMenu(NavBeranda, "Beranda Utama");
            }
            catch (System.Exception ex)
            {
                App.LogAndShowError("Gagal Memuat Jendela Utama MainWindow", ex);
            }
        }

        private void SetActiveMenu(MenuItem activeItem, string pageName)
        {
            var currentUser = CurrentUserService.LoggedInUser;
            StatusText.Text = $"Menu Aktif: {pageName} | User: {currentUser?.Name ?? "User"} ({currentUser?.Role ?? "SALES"})";
        }

        private void MenuBeranda_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new HomePage();
            SetActiveMenu(NavBeranda, "Beranda Utama");
        }

        private void MenuProduk_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new ProductListPage();
            SetActiveMenu(NavProduk, "Daftar Produk & Stok");
        }

        private void MenuSalesPerson_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new SalesPersonListPage();
            SetActiveMenu(NavMasterData, "Master Data Sales");
        }

        private void MenuCustomer_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new CustomerListPage();
            SetActiveMenu(NavMasterData, "Master Data Customer");
        }

        private void MenuAutoPO_Click(object sender, RoutedEventArgs e)
        {
            var window = new AutoPurchaseOrderWindow { Owner = this };
            window.ShowDialog();
        }

        private void MenuTransaksi_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new NotaListPage();
            SetActiveMenu(NavTransaksi, "Daftar Nota Penjualan");
        }

        private void MenuInboxSO_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new InboxSalesOrderPage();
            SetActiveMenu(NavInbox, "Inbox Sales Order Mobile");
        }

        private void MenuInboxKunjungan_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new InboxVisitPage();
            SetActiveMenu(NavInbox, "Inbox Kunjungan Sales");
        }

        private void MenuKonfirmasiPembayaran_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new PaymentConfirmationPage();
            SetActiveMenu(NavPembayaran, "Konfirmasi Pembayaran");
        }
		
		private void MenuPengiriman_Click(object sender, RoutedEventArgs e)
		{
			AreaKonten.Content = new DeliveryPage();
            SetActiveMenu(NavPengiriman, "Pengiriman Barang");
		}

        private void MenuLaporan_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new ReportPage();
            SetActiveMenu(NavLaporan, "Laporan Penjualan");
        }

        private void MenuPengaturan_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
            SetActiveMenu(NavPengaturan, "Pengaturan System");
        }

        private void MenuDatabaseInspector_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new DatabaseInspectorPage();
            SetActiveMenu(MenuDeveloperTools, "Database Inspector (Developer)");
        }

        private async void MenuSync_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null) menuItem.IsEnabled = false;

            // Tampilkan Modal Blocking Loading Overlay agar user menunggu hingga selesai 100%
            LoadingHeaderTitle.Text = "Sedang Melakukan Sinkronisasi Data...";
            LoadingHeaderSubtitle.Text = "Harap tunggu, sistem sedang bertukar data dengan server cloud Supabase.";
            LoadingStatusText.Text = "Menghubungkan ke server...";
            LoadingOverlay.Visibility = Visibility.Visible;
            MainContentPanel.IsEnabled = false;

            try
            {
                var (summary, conflicts) = await SyncService.SyncAllAsync();
                
                if (conflicts.Any())
                {
                    var conflictWindow = new ConflictResolutionWindow(conflicts) { Owner = this };
                    conflictWindow.ShowDialog();
                }

                // Refresh halaman yang sedang aktif, kalau halaman itu mendukung refresh
                if (AreaKonten.Content is IRefreshablePage refreshable)
                {
                    refreshable.RefreshData();
                }

                DialogHelper.ShowInfo(summary.ToDisplayText(), "Sinkronisasi Berhasil Selesai");
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Sinkronisasi");
            }
            finally
            {
                if (menuItem != null) menuItem.IsEnabled = true;
                LoadingOverlay.Visibility = Visibility.Collapsed;
                MainContentPanel.IsEnabled = true;
                SetActiveMenu(NavBeranda, "Beranda Utama");
            }
        }

    }
}
