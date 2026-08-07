using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Win32;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void TombolUbahPassword_Click(object sender, RoutedEventArgs e)
        {
            var changePassWindow = new ChangePasswordWindow { Owner = this };
            changePassWindow.ShowDialog();
        }


        private async void TombolTarikFullData_Click(object sender, RoutedEventArgs e)
        {
            var confirm = MessageBox.Show(
                "Apakah Anda yakin ingin menarik SELURUH data dari Supabase cloud? Data lokal akan diperbarui dengan data terbaru dari server Supabase.",
                "Konfirmasi Tarik Full Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            TombolTarikFullData.IsEnabled = false;
            TeksProgressTarikData.Visibility = Visibility.Visible;
            TeksProgressTarikData.Text = "⚡ Memulai proses penarikan data penuh...";
            var originalCursor = Cursor;
            Cursor = System.Windows.Input.Cursors.Wait;

            Action<string> progressHandler = (statusMessage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    TeksProgressTarikData.Text = statusMessage;
                });
            };

            try
            {
                SyncService.OnSyncProgress += progressHandler;
                string result = await SyncService.PullAllFromSupabaseAsync();
                MessageBox.Show(result, "Tarik Full Data Selesai", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menarik data dari Supabase:\n{ex.Message}");
            }
            finally
            {
                SyncService.OnSyncProgress -= progressHandler;
                TombolTarikFullData.IsEnabled = true;
                Cursor = originalCursor;
                TeksProgressTarikData.Text = "✅ Penarikan data selesai.";
            }
        }


        private async void TombolBackupLokal_Click(object sender, RoutedEventArgs e)

        {
            var dialog = new SaveFileDialog
            {
                Title = "Simpan Backup Database Lokal",
                Filter = "Database Files (*.db3)|*.db3",
                FileName = $"spj_local_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db3"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await BackupService.BackupLocalDatabaseAsync(dialog.FileName);
                    MessageBox.Show($"Backup database lokal berhasil disimpan di:\n{dialog.FileName}", 
                                    "Backup Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    DialogHelper.ShowError($"Gagal melakukan backup:\n{ex.Message}");
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void TombolImporCsv_Click(object sender, RoutedEventArgs e)
        {
            var importWindow = new ImportDataWindow()
            {
                Owner = this
            };
            importWindow.ShowDialog();
        }

        private async void TombolEksporJson_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Ekspor Data Lokal sebagai JSON",
                Filter = "JSON Files (*.json)|*.json",
                FileName = $"data_export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await BackupService.ExportLocalDataAsJsonAsync(dialog.FileName);
                    MessageBox.Show($"Ekspor data lokal berhasil disimpan di:\n{dialog.FileName}",
                                    "Ekspor Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    DialogHelper.ShowError($"Gagal melakukan ekspor:\n{ex.Message}");
                }
            }
        }

        private async void TombolRestoreJson_Click(object sender, RoutedEventArgs e)
        {
            var confirmation = MessageBox.Show(
                "Anda yakin ingin merestore data? Semua data lokal saat ini akan DIHAPUS dan digantikan oleh data dari file backup. Aksi ini tidak bisa dibatalkan.",
                "Konfirmasi Restore Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Pilih File Backup JSON untuk di-restore",
                Filter = "JSON Files (*.json)|*.json",
                FileName = "data_export.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await BackupService.RestoreDataFromJsonAsync(dialog.FileName);
                    MessageBox.Show("Restore data berhasil. Aplikasi mungkin perlu di-restart untuk melihat semua perubahan.",
                                    "Restore Berhasil", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    DialogHelper.ShowError($"Gagal melakukan restore:\n{ex.Message}");
                }
            }
        }
    }
}
