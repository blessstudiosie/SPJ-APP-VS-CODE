using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class SyncProgressWindow : Window
    {
        private bool _isSyncFinished = false;

        public SyncSummary? SyncSummaryResult { get; private set; }
        public List<ConflictItem> Conflicts { get; private set; } = new();

        public SyncProgressWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SyncService.OnSyncProgress += SyncService_OnSyncProgress;
            await StartSyncProcessAsync();
        }

        private void SyncService_OnSyncProgress(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TeksStatusSaatIni.Text = message;
                TxtLogDetails.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                TxtLogDetails.ScrollToEnd();
            });
        }

        private async Task StartSyncProcessAsync()
        {
            try
            {
                TxtLogDetails.AppendText($"[{DateTime.Now:HH:mm:ss}] ⚡ Memulai Sinkronisasi Manual dengan Cloud Supabase...\n");

                var (summary, conflicts) = await SyncService.SyncAllAsync();
                SyncSummaryResult = summary;
                Conflicts = conflicts;

                _isSyncFinished = true;
                ProgressBarSync.Visibility = Visibility.Collapsed;
                TeksStatusSaatIni.Text = "✅ Sinkronisasi Berhasil Selesai!";
                TeksStatusSaatIni.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

                TxtLogDetails.AppendText($"\n[{DateTime.Now:HH:mm:ss}] 🎉 {summary.ToDisplayText()}\n");
                TxtLogDetails.ScrollToEnd();

                TombolSelesai.IsEnabled = true;
                TombolSelesai.Focus();
            }
            catch (Exception ex)
            {
                _isSyncFinished = true;
                ProgressBarSync.Visibility = Visibility.Collapsed;
                TeksStatusSaatIni.Text = "❌ Sinkronisasi Gagal!";
                TeksStatusSaatIni.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                TxtLogDetails.AppendText($"\n[{DateTime.Now:HH:mm:ss}] ❌ ERROR: {ex.Message}\n");
                TxtLogDetails.ScrollToEnd();

                TombolSelesai.IsEnabled = true;
            }
            finally
            {
                SyncService.OnSyncProgress -= SyncService_OnSyncProgress;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cegah penutupan jendela jika proses sync masih berjalan
            if (!_isSyncFinished)
            {
                e.Cancel = true;
                MessageBox.Show("Mohon tunggu hingga proses sinkronisasi selesai 100%.", 
                                "Sinkronisasi Sedang Berlangsung", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Warning);
            }
        }

        private void TombolSelesai_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
