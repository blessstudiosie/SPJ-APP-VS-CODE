using SPJ_APP.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SPJ_APP.View.Pages

{
    public partial class DatabaseInspectorPage : UserControl
    {
        private readonly DatabaseInspectorService _inspectorService = new();
        private Type? _selectedTableType;

        public DatabaseInspectorPage()
        {
            InitializeComponent();
            Loaded += DatabaseInspectorPage_Loaded;
        }

        private void DatabaseInspectorPage_Loaded(object sender, RoutedEventArgs e)
        {
            ComboTables.ItemsSource = _inspectorService.InspectableTables.Keys;
        }

        private async void ComboTables_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboTables.SelectedItem is not string selectedKey) return;

            if (_inspectorService.InspectableTables.TryGetValue(selectedKey, out var tableType))
            {
                _selectedTableType = tableType;
                try
                {
                    var data = await _inspectorService.GetAllRowsAsync(_selectedTableType);
                    DataGridContent.ItemsSource = data;
                }
                catch (Exception ex)
                {
                    DialogHelper.ShowError($"Gagal memuat data: {ex.Message}");
                }
            }
        }

        private async void BtnSaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridContent.ItemsSource == null) return;

            var confirm = DialogHelper.ShowConfirm(
                "Anda yakin ingin menyimpan semua perubahan di tabel ini? Aksi ini tidak bisa dibatalkan dan dapat merusak konsistensi data.",
                "Konfirmasi Simpan Perubahan");

            if (!confirm) return;

            try
            {
                foreach (var item in DataGridContent.ItemsSource)
                {
                    await _inspectorService.UpdateRowAsync(item);
                }
                DialogHelper.ShowInfo("Perubahan berhasil disimpan.");
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menyimpan perubahan: {ex.Message}");
            }
        }

        private async void BtnDeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridContent.SelectedItems.Count == 0)
            {
                DialogHelper.ShowInfo("Pilih satu atau lebih baris untuk dihapus.");
                return;
            }

            var confirm = DialogHelper.ShowConfirm(
                $"Anda yakin ingin menghapus {DataGridContent.SelectedItems.Count} baris terpilih? Aksi ini tidak bisa dibatalkan.",
                "Konfirmasi Hapus Baris");

            if (!confirm) return;

            try
            {
                var itemsToDelete = DataGridContent.SelectedItems.Cast<object>().ToList();
                await _inspectorService.DeleteRowsAsync(itemsToDelete);

                DialogHelper.ShowInfo($"{itemsToDelete.Count} baris berhasil dihapus.");

                // Refresh data grid
                if (_selectedTableType != null)
                {
                    var data = await _inspectorService.GetAllRowsAsync(_selectedTableType);
                    DataGridContent.ItemsSource = data;
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menghapus baris: {ex.Message}");
            }
        }
    }
}