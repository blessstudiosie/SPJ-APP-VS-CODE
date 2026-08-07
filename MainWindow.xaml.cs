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
            var setupService = new InitialSetupService();
            var setupAction = await setupService.CheckAndRunInitialSetupIfNeededAsync();

            switch (setupAction)
            {
                case InitialSetupAction.ExitApplication:
                    Application.Current.Shutdown();
                    return;

                case InitialSetupAction.AdminCreated:
                    // User is already set, proceed directly to loading main content
                    break;

                case InitialSetupAction.NotSet:
                case InitialSetupAction.SyncAndLogin:
                    // These cases require initialization, then login.
                    LoadingOverlay.Visibility = Visibility.Visible;
                    MainContentPanel.IsEnabled = false;

                    AppInitializationService.InitializationProgressChanged += AppInitializationService_InitializationProgressChanged;
                    await AppInitializationService.InitializeAppAsync();
                    // The AppInitializationService_InitializationProgressChanged handler will hide the overlay.

                    var loginWindow = new LoginWindow();
                    if (loginWindow.ShowDialog() != true)
                    {
                        Application.Current.Shutdown();
                        return;
                    }
                    break;
            }
            
            // This code runs for AdminCreated, and after successful login for the other cases.
            Title = $"SPJ App - Selamat Datang, {CurrentUserService.LoggedInUser?.Name ?? "User"}!";
            
            // For AdminCreated, the initialization service hasn't run yet. We run it now.
            // For the other cases, it has already run.
            if (setupAction == InitialSetupAction.AdminCreated)
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                MainContentPanel.IsEnabled = false;
                AppInitializationService.InitializationProgressChanged += AppInitializationService_InitializationProgressChanged;
                await AppInitializationService.InitializeAppAsync();
            }
        }

        private void AppInitializationService_InitializationProgressChanged(object? sender, string message)
        {
            Dispatcher.Invoke(() =>
            {
                LoadingStatusText.Text = message;
                if (message.Contains("selesai") || message.Contains("Error"))
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MainContentPanel.IsEnabled = true;
                    AppInitializationService.InitializationProgressChanged -= AppInitializationService_InitializationProgressChanged;

                    // Refresh the home page to show newly synced data
                    if (AreaKonten.Content is HomePage)
                    {
                        (AreaKonten.Content as IRefreshablePage)?.RefreshData();
                    }
                    else
                    {
                        AreaKonten.Content = new HomePage();
                    }
                }
            });
        }


        private void MenuBeranda_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new HomePage();
        }

        private void MenuProduk_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new ProductListPage();
        }

        private void MenuSalesPerson_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new SalesPersonListPage();
        }

        private void MenuCustomer_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new CustomerListPage();
        }

        private void MenuAutoPO_Click(object sender, RoutedEventArgs e)
        {
            var window = new AutoPurchaseOrderWindow { Owner = this };
            window.ShowDialog();
        }

        private void MenuTransaksi_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new NotaListPage();
        }

        private void MenuInboxSO_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new InboxSalesOrderPage();
        }

        private void MenuInboxKunjungan_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new InboxVisitPage();
        }

        private void MenuKonfirmasiPembayaran_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new PaymentConfirmationPage();
        }
		
		private void MenuPengiriman_Click(object sender, RoutedEventArgs e)
		{
			AreaKonten.Content = new DeliveryPage();
		}

        private void MenuLaporan_Click(object sender, RoutedEventArgs e)
        {
            AreaKonten.Content = new ReportPage();
        }

        private void MenuPengaturan_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow { Owner = this };
            settingsWindow.ShowDialog();
        }

        private async void MenuSync_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem != null) menuItem.IsEnabled = false;
            
            StatusText.Text = "Memulai sinkronisasi manual...";
            SyncProgressBar.Visibility = Visibility.Visible;

            try
            {
                var (summary, conflicts) = await SyncService.SyncAllAsync();
                
                if (conflicts.Any())
                {
                    var conflictWindow = new ConflictResolutionWindow(conflicts) { Owner = this };
                    conflictWindow.ShowDialog();
                }

                DialogHelper.ShowInfo(summary.ToDisplayText(), "Ringkasan Sync");

                // Refresh halaman yang sedang aktif, kalau halaman itu mendukung refresh
                if (AreaKonten.Content is IRefreshablePage refreshable)
                {
                    refreshable.RefreshData();
                }
                StatusText.Text = "Sinkronisasi manual berhasil.";
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Sync");
                StatusText.Text = "Sinkronisasi manual gagal.";
            }
            finally
            {
                if (menuItem != null) menuItem.IsEnabled = true;
                SyncProgressBar.Visibility = Visibility.Collapsed;
            }
        }
    }
}
