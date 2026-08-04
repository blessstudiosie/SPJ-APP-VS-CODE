using System.Windows;
using SPJ_APP.Service;
using SPJ_APP.View.Pages;
using System.Windows.Controls;

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

        private void BackgroundSyncService_SyncStatusChanged(object sender, SyncStatusEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = e.Message;
                SyncProgressBar.Visibility = e.IsSyncing ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            MainContentPanel.IsEnabled = false;

            AppInitializationService.InitializationProgressChanged += AppInitializationService_InitializationProgressChanged;
            await AppInitializationService.InitializeAppAsync();
        }

        private void AppInitializationService_InitializationProgressChanged(object sender, string message)
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
            // Menu Pengaturan belum dibangun
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
