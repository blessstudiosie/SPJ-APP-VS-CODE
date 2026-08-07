using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public enum ImportType
    {
        Customers,
        Products
    }

    public partial class ImportDataWindow : Window
    {
        private string? _selectedCsvPath;
        private readonly Dictionary<string, ImportType> _importTypeMappings;

        public ImportDataWindow()
        {
            InitializeComponent();
            _importTypeMappings = new Dictionary<string, ImportType>
            {
                { "Pelanggan", ImportType.Customers },
                { "Produk", ImportType.Products }
            };
            ComboBoxTipeData.ItemsSource = _importTypeMappings.Keys;
        }

        private void ConfigureWindowForImportType(ImportType importType)
        {
            switch (importType)
            {
                case ImportType.Customers:
                    LabelTipeData.Text = "Impor Data Pelanggan";
                    FormatColumns.Text = "Id,Name,Phone,Address";
                    break;
                case ImportType.Products:
                    LabelTipeData.Text = "Impor Data Produk";
                    FormatColumns.Text = "Id,Name,StokFisik,HargaR,Kategori,Status";
                    break;
            }
            BorderFormat.Visibility = Visibility.Visible;
            ButtonPilihFile.IsEnabled = true;
        }

        private void ComboBoxTipeData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboBoxTipeData.SelectedItem == null) return;
            
            var selectedKey = ComboBoxTipeData.SelectedItem.ToString()!;
            if (_importTypeMappings.TryGetValue(selectedKey, out var selectedType))
            {
                ConfigureWindowForImportType(selectedType);
            }
        }

        private void ButtonPilihFile_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxTipeData.SelectedItem == null)
            {
                MessageBox.Show("Silakan pilih tipe data terlebih dahulu.", "Tipe Data Belum Dipilih", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedKey = ComboBoxTipeData.SelectedItem.ToString();
            var dialog = new OpenFileDialog
            {
                Title = $"Pilih File CSV untuk {selectedKey}",
                Filter = "CSV Files (*.csv)|*.csv",
            };

            if (dialog.ShowDialog() == true)
            {
                _selectedCsvPath = dialog.FileName;
                TextBlockNamaFile.Text = System.IO.Path.GetFileName(_selectedCsvPath);
                TextBlockNamaFile.FontStyle = FontStyles.Normal;
                ButtonMulaiImpor.IsEnabled = true;
            }
        }

        private async void ButtonMulaiImpor_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxTipeData.SelectedItem == null || string.IsNullOrEmpty(_selectedCsvPath))
            {
                MessageBox.Show("Silakan pilih tipe data dan file CSV terlebih dahulu.", "Input Tidak Lengkap", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Disable buttons and show progress
            SetUiInProgress(true);

            try
            {
                var selectedKey = ComboBoxTipeData.SelectedItem.ToString()!;
                if (!_importTypeMappings.TryGetValue(selectedKey, out var importType))
                {
                    throw new InvalidOperationException("Tipe impor tidak ditemukan atau tidak valid.");
                }
                ImportResult result;

                switch (importType)
                {
                    case ImportType.Customers:
                        result = await ImportService.ImportCustomersFromCsvAsync(_selectedCsvPath);
                        break;
                    case ImportType.Products:
                        result = await ImportService.ImportProductsFromCsvAsync(_selectedCsvPath);
                        break;
                    default:
                        throw new InvalidOperationException("Tipe impor tidak diketahui.");
                }

                DisplayResults(result);
            }
            catch (Exception ex)
            {
                TextBlockHasil.Text = $"Terjadi kesalahan fatal saat impor:\n{ex.Message}";
            }
            finally
            {
                SetUiInProgress(false);
            }
        }

        private void SetUiInProgress(bool inProgress)
        {
            ProgressBarImpor.IsIndeterminate = inProgress;
            ButtonMulaiImpor.IsEnabled = !inProgress;
            ButtonPilihFile.IsEnabled = !inProgress;
            ComboBoxTipeData.IsEnabled = !inProgress;
        }

        private void DisplayResults(ImportResult result)
        {
            var resultText = new StringBuilder();
            resultText.AppendLine("Proses Impor Selesai.");
            resultText.AppendLine();
            resultText.AppendLine($"Total Baris: {result.TotalRows}");
            resultText.AppendLine($"Berhasil: {result.SuccessCount}");
            resultText.AppendLine($"Gagal: {result.FailCount}");

            if (result.Errors.Count > 0)
            {
                resultText.AppendLine("\nDetail Error (maksimal 20 baris pertama):");
                foreach (var error in result.Errors.Take(20))
                {
                    resultText.AppendLine(error);
                }
                if (result.Errors.Count > 20)
                {
                    resultText.AppendLine($"... dan {result.Errors.Count - 20} error lainnya.");
                }
            }
            TextBlockHasil.Text = resultText.ToString();
        }
    }
}
