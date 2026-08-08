using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly CancellationTokenSource _cts = new();

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

                var (summary, conflicts) = await SyncService.SyncAllAsync(_cts.Token);
                SyncSummaryResult = summary;
                Conflicts = conflicts;

                _isSyncFinished = true;
                ProgressBarSync.Visibility = Visibility.Collapsed;
                TeksStatusSaatIni.Text = "✅ Sinkronisasi Berhasil Selesai!";
                TeksStatusSaatIni.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

                TxtLogDetails.AppendText($"\n[{DateTime.Now:HH:mm:ss}] 🎉 {summary.ToDisplayText()}\n");
                TxtLogDetails.ScrollToEnd();

                TombolForceStop.IsEnabled = false;
                TombolSelesai.IsEnabled = true;
                TombolSelesai.Focus();
            }
            catch (OperationCanceledException)
            {
                _isSyncFinished = true;
                ProgressBarSync.Visibility = Visibility.Collapsed;
                TeksStatusSaatIni.Text = "⏹️ Sinkronisasi Dihentikan Paksa (Force Stop).";
                TeksStatusSaatIni.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));

                TxtLogDetails.AppendText($"\n[{DateTime.Now:HH:mm:ss}] ⏹️ Sinkronisasi telah dihentikan paksa oleh pengguna.\n");
                TxtLogDetails.ScrollToEnd();

                TombolForceStop.IsEnabled = false;
                TombolSelesai.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _isSyncFinished = true;
                ProgressBarSync.Visibility = Visibility.Collapsed;
                TeksStatusSaatIni.Text = "❌ Sinkronisasi Gagal!";
                TeksStatusSaatIni.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444"));

                TxtLogDetails.AppendText($"\n[{DateTime.Now:HH:mm:ss}] ❌ ERROR: {ex.Message}\n");
                TxtLogDetails.ScrollToEnd();

                TombolForceStop.IsEnabled = false;
                TombolSelesai.IsEnabled = true;
            }
            finally
            {
                SyncService.OnSyncProgress -= SyncService_OnSyncProgress;
            }
        }

        private void TombolForceStop_Click(object sender, RoutedEventArgs e)
        {
            TombolForceStop.IsEnabled = false;
            TeksStatusSaatIni.Text = "⚠️ Menghentikan sinkronisasi...";
            TxtLogDetails.AppendText($"[{DateTime.Now:HH:mm:ss}] ⚠️ Permintaan Force Stop dikirim. Menghentikan proses...\n");
            _cts.Cancel();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_isSyncFinished && !_cts.IsCancellationRequested)
            {
                var confirm = MessageBox.Show(
                    "Sinkronisasi sedang berjalan. Apakah Anda yakin ingin menghentikan paksa (Force Stop) dan menutup jendela?",
                    "Konfirmasi Force Stop Sync",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm == MessageBoxResult.Yes)
                {
                    _cts.Cancel();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void TombolSelesai_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
