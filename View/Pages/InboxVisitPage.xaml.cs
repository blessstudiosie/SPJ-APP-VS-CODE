using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class InboxVisitPage : UserControl, IRefreshablePage
    {
        private List<LocalVisitLogQueue> _allVisits = new();
        private LocalVisitLogQueue? _selectedVisit;

        public InboxVisitPage()
        {
            InitializeComponent();
            LoadDataAsync();
        }

        public void RefreshData()
        {
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            try
            {
                var db = await LocalDatabaseService.GetConnection();

                _allVisits = await db.Table<LocalVisitLogQueue>()
                                     .OrderByDescending(v => v.CreatedAt)
                                     .ToListAsync();

                var customers = await db.Table<LocalCustomer>().ToListAsync();
                var salesPersons = await db.Table<LocalSalesPerson>().ToListAsync();

                var custById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var custByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var c in customers)
                {
                    if (!string.IsNullOrEmpty(c.Id)) custById[c.Id.Trim()] = c.Name?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(c.Name)) custByName[c.Name.Trim()] = c.Name.Trim();
                }

                var spById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var spByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var sp in salesPersons)
                {
                    if (!string.IsNullOrEmpty(sp.Id)) spById[sp.Id.Trim()] = sp.Name?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(sp.Name)) spByName[sp.Name.Trim()] = sp.Name.Trim();
                }

                foreach (var v in _allVisits)
                {
                    string custInput = !string.IsNullOrWhiteSpace(v.CustomerName) ? v.CustomerName : v.CustomerId ?? "";
                    string spInput = !string.IsNullOrWhiteSpace(v.SalesPersonName) ? v.SalesPersonName : v.SalesPersonId ?? "";

                    v.CustomerName = SalesResolutionService.ResolveCustomerName(custInput, custById, custByName);
                    v.SalesPersonName = SalesResolutionService.ResolveSalesPersonName(spInput, spById, spByName);
                }

                ApplyFilter();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal memuat log kunjungan sales: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            string queryText = TxtCari?.Text?.Trim() ?? string.Empty;

            IEnumerable<LocalVisitLogQueue> query = _allVisits;
            if (!string.IsNullOrWhiteSpace(queryText))
            {
                query = query.Where(v =>
                    (v.SalesPersonName != null && v.SalesPersonName.Contains(queryText, StringComparison.OrdinalIgnoreCase)) ||
                    (v.CustomerName != null && v.CustomerName.Contains(queryText, StringComparison.OrdinalIgnoreCase)) ||
                    (v.Notes != null && v.Notes.Contains(queryText, StringComparison.OrdinalIgnoreCase))
                );
            }

            TabelQueue.ItemsSource = query.ToList();
            PanelDetail.IsEnabled = false;
            _selectedVisit = null;
            ResetDetailPanel();
        }

        private void ResetDetailPanel()
        {
            TeksSales.Text = "Sales: -";
            TeksCustomer.Text = "Customer: -";
            TeksIsNewCustomer.Text = "Status Pelanggan: -";
            TeksWaktu.Text = "Waktu Kunjungan: -";
            TeksGPS.Text = "Lokasi GPS: 0.0, 0.0";
            TeksCatatan.Text = "-";
            GambarFotoKunjungan.Source = null;
            TeksFotoStatus.Visibility = Visibility.Visible;
            TeksFotoStatus.Text = "Tidak ada foto kunjungan";
        }

        private void TxtCari_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                ApplyFilter();
        }

        private async void TombolRefresh_Click(object sender, RoutedEventArgs e)
        {
            TombolRefresh.IsEnabled = false;
            try
            {
                await SyncService.SyncVisitLogsQueueAsync();
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal refresh data kunjungan: {ex.Message}");
            }
            finally
            {
                TombolRefresh.IsEnabled = true;
            }
        }

        private void TabelQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedVisit = TabelQueue.SelectedItem as LocalVisitLogQueue;
            if (_selectedVisit == null)
            {
                PanelDetail.IsEnabled = false;
                ResetDetailPanel();
                return;
            }

            PanelDetail.IsEnabled = true;
            TeksSales.Text = $"Sales: {_selectedVisit.SalesPersonName}";
            TeksCustomer.Text = $"Customer: {_selectedVisit.CustomerName}";
            TeksIsNewCustomer.Text = _selectedVisit.IsNewCustomer
                ? "Status Pelanggan: PELANGGAN BARU (Diinput dari Lapangan)"
                : "Status Pelanggan: Pelanggan Terdaftar";
            TeksWaktu.Text = $"Waktu Kunjungan: {_selectedVisit.CreatedAt:dd/MM/yyyy HH:mm}";
            TeksGPS.Text = $"Lokasi GPS: {_selectedVisit.Latitude:F6}, {_selectedVisit.Longitude:F6}";
            TeksCatatan.Text = string.IsNullOrWhiteSpace(_selectedVisit.Notes) ? "(Tanpa catatan)" : _selectedVisit.Notes;

            // Load photo preview if valid URL
            if (!string.IsNullOrWhiteSpace(_selectedVisit.PhotoUrl))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(_selectedVisit.PhotoUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    GambarFotoKunjungan.Source = bitmap;
                    TeksFotoStatus.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    GambarFotoKunjungan.Source = null;
                    TeksFotoStatus.Visibility = Visibility.Visible;
                    TeksFotoStatus.Text = "Gagal memuat preview foto";
                }
            }
            else
            {
                GambarFotoKunjungan.Source = null;
                TeksFotoStatus.Visibility = Visibility.Visible;
                TeksFotoStatus.Text = "Tidak ada foto dilaporkan";
            }
        }

        private void TombolBukaPeta_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVisit == null) return;
            string mapsUrl = $"https://www.google.com/maps/search/?api=1&query={_selectedVisit.Latitude},{_selectedVisit.Longitude}";
            try
            {
                Process.Start(new ProcessStartInfo(mapsUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal membuka browser: {ex.Message}");
            }
        }
    }
}
