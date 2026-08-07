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
            SyncService.OnSyncProgress += SyncService_OnSyncProgress;
        }

        private void SyncService_OnSyncProgress(string statusMessage)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingStatusText.Text = statusMessage;
                SyncStatusTextRight.Text = statusMessage;
                SyncStatusTextRight.Visibility = Visibility.Visible;
                SyncProgressBar.Visibility = Visibility.Visible;
            });
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
        
        private async void BackgroundSyncService_SyncStatusChanged(object? sender, SyncStatusEventArgs e)
        {
            await Dispatcher.Invoke(async () =>
            {
                if (e.IsSyncing)
                {
                    SyncStatusTextRight.Text = string.IsNullOrWhiteSpace(e.Message) ? "Sinkronisasi otomatis..." : e.Message;
                    SyncStatusTextRight.Visibility = Visibility.Visible;
                    SyncProgressBar.Visibility = Visibility.Visible;
                }
                else
                {
                    SyncProgressBar.Visibility = Visibility.Collapsed;
                    SyncStatusTextRight.Text = $"✅ Sync selesai ({DateTime.Now:HH:mm:ss})";
                    SyncStatusTextRight.Visibility = Visibility.Visible;

                    await Task.Delay(5000);
                    if (SyncProgressBar.Visibility == Visibility.Collapsed)
                    {
                        SyncStatusTextRight.Visibility = Visibility.Collapsed;
                    }
                }
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentUser = CurrentUserService.LoggedInUser;
                Title = $"SPJ App - Selamat Datang, {currentUser?.Name ?? "User"}!";

                if (TeksUserBadge != null && currentUser != null)
                {
                    TeksUserBadge.Text = $"{currentUser.Name} ({currentUser.Role})";
                }

                // Tampilkan Developer Tools untuk Akun Developer / Admin
                var currentUserRole = currentUser?.Role?.ToUpperInvariant();
                var currentUserName = currentUser?.Name;
                if (currentUserRole == "DEVELOPER" ||
                    currentUserRole == "ADMIN" ||
                    string.Equals(currentUserName, "blessstudiosie", StringComparison.OrdinalIgnoreCase))
                {
                    MenuDeveloperTools.Visibility = Visibility.Visible;
                }
                else
                {
                    MenuDeveloperTools.Visibility = Visibility.Collapsed;
                }

                // Default tampilan utama adalah Beranda (HomePage)
                AreaKonten.Content = new HomePage();
                SetActiveMenu(NavBeranda, "Beranda Utama", typeof(HomePage));

            }
            catch (System.Exception ex)
            {
                App.LogAndShowError("Gagal Memuat Jendela Utama MainWindow", ex);
            }
        }

        private void MenuLogout_Click(object sender, RoutedEventArgs e)
        {
            var currentUser = CurrentUserService.LoggedInUser;
            string userName = currentUser?.Name ?? "User";

            bool confirm = DialogHelper.ShowConfirm(
                $"Apakah Anda yakin ingin keluar (Log Out) dari akun '{userName}'?",
                "Konfirmasi Log Out");

            if (!confirm) return;

            try
            {
                // Hentikan background sync timer
                BackgroundSyncService.Instance.Stop();

                // Reset user session
                CurrentUserService.LoggedInUser = null;

                // Atur ShutdownMode agar penutupan MainWindow tidak langsung menghentikan app
                Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                Close();

                // Buka dialog login kembali
                var loginWindow = new LoginWindow();
                bool? loginResult = loginWindow.ShowDialog();

                if (loginResult == true && CurrentUserService.LoggedInUser != null)
                {
                    BackgroundSyncService.Instance.Start();
                    var newMainWindow = new MainWindow();
                    Application.Current.MainWindow = newMainWindow;
                    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                    newMainWindow.Show();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal melakukan Log Out:\n{ex.Message}");
            }
        }


        private Type? _currentActivePageType;

        private bool SetActiveMenu(MenuItem activeItem, string pageName, Type pageType)
        {
            // Jika user menekan menu untuk halaman yang sedang terbuka saat ini, abaikan agar tidak reload berkali-kali
            if (_currentActivePageType == pageType)
            {
                return false;
            }

            _currentActivePageType = pageType;

            // Reset style visual semua menu ke kondisi normal
            ResetAllMenuStyles();

            // Highlight menu yang sedang aktif agar terlihat menonjol
            if (activeItem != null)
            {
                activeItem.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4F46E5"));
                activeItem.Foreground = System.Windows.Media.Brushes.White;
                activeItem.FontWeight = FontWeights.Bold;
            }

            var currentUser = CurrentUserService.LoggedInUser;
            StatusText.Text = $"Menu Aktif: {pageName} | User: {currentUser?.Name ?? "User"} ({currentUser?.Role ?? "SALES"})";
            return true;
        }

        private void ResetAllMenuStyles()
        {
            MenuItem[] topMenus = new[]
            {
                NavBeranda, NavProduk, NavMasterData, NavTransaksi,
                NavInbox, NavPembayaran, NavPengiriman, NavLaporan,
                NavPengaturan, MenuDeveloperTools
            };

            foreach (var menu in topMenus)
            {
                if (menu != null)
                {
                    menu.Background = System.Windows.Media.Brushes.Transparent;
                    menu.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1E293B"));
                    menu.FontWeight = FontWeights.SemiBold;
                }
            }
        }

        private void MenuBeranda_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavBeranda, "Beranda Utama", typeof(HomePage)))
            {
                AreaKonten.Content = new HomePage();
            }
        }

        private void MenuProduk_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavProduk, "Daftar Produk & Stok", typeof(ProductListPage)))
            {
                AreaKonten.Content = new ProductListPage();
            }
        }

        private void MenuSalesPerson_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavMasterData, "Master Data Sales", typeof(SalesPersonListPage)))
            {
                AreaKonten.Content = new SalesPersonListPage();
            }
        }

        private void MenuCustomer_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavMasterData, "Master Data Customer", typeof(CustomerListPage)))
            {
                AreaKonten.Content = new CustomerListPage();
            }
        }

        private void MenuAutoPO_Click(object sender, RoutedEventArgs e)
        {
            var window = new AutoPurchaseOrderWindow { Owner = this };
            window.ShowDialog();
        }

        private void MenuTransaksi_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavTransaksi, "Daftar Nota Penjualan", typeof(NotaListPage)))
            {
                AreaKonten.Content = new NotaListPage();
            }
        }

        private void MenuInboxSO_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavInbox, "Inbox Sales Order Mobile", typeof(InboxSalesOrderPage)))
            {
                AreaKonten.Content = new InboxSalesOrderPage();
            }
        }

        private void MenuInboxKunjungan_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavInbox, "Inbox Kunjungan Sales", typeof(InboxVisitPage)))
            {
                AreaKonten.Content = new InboxVisitPage();
            }
        }

        private void MenuKonfirmasiPembayaran_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavPembayaran, "Konfirmasi Pembayaran", typeof(PaymentConfirmationPage)))
            {
                AreaKonten.Content = new PaymentConfirmationPage();
            }
        }

        private void MenuPengiriman_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavPengiriman, "Log Pengiriman", typeof(DeliveryPage)))
            {
                AreaKonten.Content = new DeliveryPage();
            }
        }

        private void MenuLaporan_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(NavLaporan, "Laporan Penjualan", typeof(ReportPage)))
            {
                AreaKonten.Content = new ReportPage();
            }
        }

        private void MenuPengaturan_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }

        private void MenuUbahPassword_Click(object sender, RoutedEventArgs e)
        {
            var changePassWindow = new ChangePasswordWindow { Owner = this };
            changePassWindow.ShowDialog();
        }

        private void MenuDatabaseInspector_Click(object sender, RoutedEventArgs e)
        {
            if (SetActiveMenu(MenuDeveloperTools, "Database Inspector (Developer)", typeof(DatabaseInspectorPage)))
            {
                AreaKonten.Content = new DatabaseInspectorPage();
            }
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
                SyncProgressBar.Visibility = Visibility.Collapsed;
                SyncStatusTextRight.Text = $"✅ Sync selesai ({DateTime.Now:HH:mm:ss})";
                SyncStatusTextRight.Visibility = Visibility.Visible;
                SetActiveMenu(NavBeranda, "Beranda Utama", typeof(HomePage));


                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000);
                    Dispatcher.Invoke(() =>
                    {
                        if (SyncProgressBar.Visibility == Visibility.Collapsed)
                        {
                            SyncStatusTextRight.Visibility = Visibility.Collapsed;
                        }
                    });
                });
            }
        }


    }
}
