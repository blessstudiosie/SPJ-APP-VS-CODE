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
    }
}
