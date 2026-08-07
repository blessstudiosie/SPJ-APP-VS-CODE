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

                foreach (var v in _allVisits)
                {
                    var sp = salesPersons.FirstOrDefault(s => s.Id == v.SalesPersonId || string.Equals(s.Name, v.SalesPersonId, StringComparison.OrdinalIgnoreCase));
                    v.SalesPersonName = sp?.Name ?? (string.IsNullOrWhiteSpace(v.SalesPersonName) ? (v.SalesPersonId ?? "-") : v.SalesPersonName);

                    var cust = customers.FirstOrDefault(c => c.Id == v.CustomerId || string.Equals(c.Name, v.CustomerId, StringComparison.OrdinalIgnoreCase) || string.Equals(c.Name, v.CustomerName, StringComparison.OrdinalIgnoreCase));
                    v.CustomerName = cust?.Name ?? (string.IsNullOrWhiteSpace(v.CustomerName) ? (v.CustomerId ?? "-") : v.CustomerName);
                }

                ApplyFilter();

            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal memuat antrian kunjungan: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            var selectedItem = ComboFilterStatus.SelectedItem as ComboBoxItem;
            string filterStatus = selectedItem?.Content?.ToString() ?? "PENDING";

            IEnumerable<LocalVisitLogQueue> query = _allVisits;
            if (filterStatus != "SEMUA")
            {
                query = query.Where(v => v.Status.Equals(filterStatus, StringComparison.OrdinalIgnoreCase));
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

        private void ComboFilterStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
                DialogHelper.ShowError($"Gagal refresh queue kunjungan: {ex.Message}");
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

            bool isPending = _selectedVisit.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
            TombolApprove.IsEnabled = isPending;
            TombolTolak.IsEnabled = isPending;
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

        private async void TombolApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVisit == null) return;

            string confirmMsg = $"Setujui log kunjungan dari '{_selectedVisit.SalesPersonName}' ke '{_selectedVisit.CustomerName}'?";
            if (_selectedVisit.IsNewCustomer)
            {
                confirmMsg += "\n\nCatatan: Pelanggan ini diinput baru dari lapangan dan akan otomatis ditambahkan ke Master Data Customer!";
            }

            var result = MessageBox.Show(confirmMsg, "Konfirmasi Approval Kunjungan", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var db = await LocalDatabaseService.GetConnection();

                // If new customer, auto register to LocalCustomer table if not already exists
                if (_selectedVisit.IsNewCustomer)
                {
                    var existingCustomer = await db.Table<LocalCustomer>()
                                                    .Where(c => c.Name.ToLower() == _selectedVisit.CustomerName.ToLower())
                                                    .FirstOrDefaultAsync();

                    if (existingCustomer == null)
                    {
                        var newCust = new LocalCustomer
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = _selectedVisit.CustomerName,
                            Address = $"Dari Kunjungan Sales ({_selectedVisit.SalesPersonName})",
                            SalesPersonId = _selectedVisit.SalesPersonId,
                            Latitude = _selectedVisit.Latitude,
                            Longitude = _selectedVisit.Longitude,
                            UpdatedAt = DateTime.Now,
                            IsSynced = false
                        };
                        await db.InsertAsync(newCust);
                    }
                }

                _selectedVisit.Status = "APPROVED";
                _selectedVisit.UpdatedAt = DateTime.Now;
                await db.UpdateAsync(_selectedVisit);

                _ = SyncService.PushQueueStatusToSupabaseAsync(_selectedVisit.Id, "APPROVED", "VISIT");

                await ActivityLogService.LogAsync("APPROVE_VISIT_QUEUE", $"Menyetujui Kunjungan Mobile '{_selectedVisit.Id}' ({_selectedVisit.CustomerName}).");

                DialogHelper.ShowInfo("Log kunjungan berhasil disetujui!");
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menyetujui kunjungan: {ex.Message}");
            }
        }

        private async void TombolTolak_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedVisit == null) return;

            var result = MessageBox.Show($"Menolak log kunjungan dari '{_selectedVisit.SalesPersonName}'?", "Konfirmasi Tolak Kunjungan", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var db = await LocalDatabaseService.GetConnection();

                _selectedVisit.Status = "REJECTED";
                _selectedVisit.UpdatedAt = DateTime.Now;
                await db.UpdateAsync(_selectedVisit);

                _ = SyncService.PushQueueStatusToSupabaseAsync(_selectedVisit.Id, "REJECTED", "VISIT");

                await ActivityLogService.LogAsync("REJECT_VISIT_QUEUE", $"Menolak Kunjungan Mobile '{_selectedVisit.Id}'.");

                DialogHelper.ShowInfo("Log kunjungan telah ditolak.");
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menolak kunjungan: {ex.Message}");
            }
        }
    }
}
