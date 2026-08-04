using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public class ConflictDisplayItem
    {
        public string ProductName { get; set; } = "";
        public string LocalSummary { get; set; } = "";
        public string ServerSummary { get; set; } = "";
        public LocalProduct LocalVersion { get; set; } = null!;
        public Product ServerVersion { get; set; } = null!;
    }

    public partial class ConflictResolutionWindow : Window
    {
        private List<ConflictDisplayItem> _items = new();

        public ConflictResolutionWindow(List<ConflictItem> conflicts)
        {
            InitializeComponent();

            _items = conflicts.Select(c => new ConflictDisplayItem
            {
                ProductName = c.LocalVersion.Name,
                LocalVersion = c.LocalVersion,
                ServerVersion = c.ServerVersion,
                LocalSummary = FormatSummary(c.LocalVersion.Kategori, c.LocalVersion.HargaR, c.LocalVersion.Satuan, c.LocalVersion.SatuanBesar),
                ServerSummary = FormatSummary(c.ServerVersion.Kategori, c.ServerVersion.HargaR, c.ServerVersion.Satuan, c.ServerVersion.SatuanBesar)
            }).ToList();

            DaftarKonflik.ItemsSource = _items;
        }

        private string FormatSummary(string? kategori, decimal hargaR, string? satuan, string? satuanBesar)
        {
            return $"Kategori: {kategori}\nHarga R: {hargaR:N0}\nSatuan: {satuan} / {satuanBesar}";
        }

        private async void PakaiLokal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ConflictDisplayItem item)
            {
                try
                {
                    await SyncService.ForcePushAsync(item.LocalVersion);
                    RemoveItemFromList(item);
                    DialogHelper.ShowInfo($"Data '{item.ProductName}' berhasil ditimpa dengan versi Anda.");
                }
                catch (Exception ex)
                {
                    DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex));
                }
            }
        }

        private async void PakaiServer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is ConflictDisplayItem item)
            {
                try
                {
                    await SyncService.AcceptServerVersionAsync(item.ServerVersion);
                    RemoveItemFromList(item);
                    DialogHelper.ShowInfo($"Data '{item.ProductName}' diganti dengan versi server.");
                }
                catch (Exception ex)
                {
                    DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex));
                }
            }
        }

        private void RemoveItemFromList(ConflictDisplayItem item)
        {
            _items.Remove(item);
            DaftarKonflik.ItemsSource = null;
            DaftarKonflik.ItemsSource = _items;
        }

        private void TombolTutup_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}