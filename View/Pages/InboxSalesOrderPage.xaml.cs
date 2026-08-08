using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class InboxSalesOrderPage : UserControl, IRefreshablePage
    {
        private List<LocalSalesOrderQueue> _allQueues = new();
        private LocalSalesOrderQueue? _selectedQueue;
        private ObservableCollection<SalesOrderItemDTO> _currentItems = new();

        public InboxSalesOrderPage()
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

                _allQueues = await db.Table<LocalSalesOrderQueue>()
                                     .OrderByDescending(q => q.CreatedAt)
                                     .ToListAsync();

                foreach (var q in _allQueues)
                {
                    string custInput = !string.IsNullOrWhiteSpace(q.CustomerName) ? q.CustomerName : q.CustomerId ?? "";
                    string spInput = !string.IsNullOrWhiteSpace(q.SalesPersonName) ? q.SalesPersonName : q.SalesPersonId ?? "";

                    q.CustomerName = NameLookupService.GetCustomerName(custInput);
                    q.SalesPersonName = NameLookupService.GetSalesPersonName(spInput);
                }

                ApplyFilter();

            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal memuat antrian sales order: {ex.Message}");
            }
        }

        private void ApplyFilter()
        {
            var selectedItem = ComboFilterStatus.SelectedItem as ComboBoxItem;
            string filterStatus = selectedItem?.Content?.ToString() ?? "PENDING";

            IEnumerable<LocalSalesOrderQueue> query = _allQueues;
            if (filterStatus != "SEMUA")
            {
                query = query.Where(q => q.Status.Equals(filterStatus, StringComparison.OrdinalIgnoreCase));
            }

            TabelQueue.ItemsSource = query.ToList();
            PanelDetail.IsEnabled = false;
            _selectedQueue = null;
            _currentItems.Clear();
            TabelItems.ItemsSource = null;
            TeksInfoCustomer.Text = "Customer: -";
            TeksInfoWaktu.Text = "Waktu Masuk: -";
            TeksInfoCatatan.Text = "Catatan: -";
            TeksTotalGrand.Text = "Total: Rp 0";
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
                await SyncService.SyncSalesOrdersQueueAsync();
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal refresh queue: {ex.Message}");
            }
            finally
            {
                TombolRefresh.IsEnabled = true;
            }
        }

        private void TabelQueue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedQueue = TabelQueue.SelectedItem as LocalSalesOrderQueue;
            if (_selectedQueue == null)
            {
                PanelDetail.IsEnabled = false;
                return;
            }

            PanelDetail.IsEnabled = true;
            TeksInfoCustomer.Text = $"Customer: {_selectedQueue.CustomerName}";
            TeksInfoWaktu.Text = $"Waktu Masuk: {_selectedQueue.CreatedAt:dd/MM/yyyy HH:mm}";
            TeksInfoCatatan.Text = $"Catatan: {(string.IsNullOrWhiteSpace(_selectedQueue.Notes) ? "-" : _selectedQueue.Notes)}";

            // Parse items JSON
            try
            {
                var items = JsonSerializer.Deserialize<List<SalesOrderItemDTO>>(_selectedQueue.ItemsJson) ?? new();
                _currentItems = new ObservableCollection<SalesOrderItemDTO>(items);
                TabelItems.ItemsSource = _currentItems;
                RecalculateTotal();
            }
            catch
            {
                _currentItems.Clear();
                TabelItems.ItemsSource = _currentItems;
                RecalculateTotal();
            }

            // Enable/disable action buttons depending on status
            bool isPending = _selectedQueue.Status.Equals("PENDING", StringComparison.OrdinalIgnoreCase);
            TombolApprove.IsEnabled = isPending;
            TombolTolak.IsEnabled = isPending;
            TombolHapusItem.IsEnabled = isPending;
        }

        private void RecalculateTotal()
        {
            decimal total = _currentItems.Sum(i => i.Subtotal);
            TeksTotalGrand.Text = $"Total: Rp {total:N0}";
        }

        private void TabelItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                RecalculateTotal();
            }));
        }

        private void TombolHapusItem_Click(object sender, RoutedEventArgs e)
        {
            if (TabelItems.SelectedItem is SalesOrderItemDTO selectedItem)
            {
                _currentItems.Remove(selectedItem);
                RecalculateTotal();
            }
            else
            {
                DialogHelper.ShowInfo("Pilih item barang yang ingin dihapus terlebih dahulu.");
            }
        }

        private async void TombolApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQueue == null) return;
            if (!_currentItems.Any())
            {
                DialogHelper.ShowError("Tidak ada item barang dalam pesanan ini.");
                return;
            }

            var result = MessageBox.Show(
                $"Setujui pesanan dari '{_selectedQueue.CustomerName}' dengan total Rp {TeksTotalGrand.Text.Replace("Total: Rp ", "")}?\n\nItem akan dimasukkan ke Nota baru berstatus SO.",
                "Konfirmasi Approval SO",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var db = await LocalDatabaseService.GetConnection();

                // Build new Sale
                string saleId = Guid.NewGuid().ToString();
                string notaNumber = $"SO-{DateTime.Now:yyyyMMddHHmmss}";
                decimal grandTotal = _currentItems.Sum(i => i.Subtotal);

                var newSale = new LocalSale
                {
                    Id = saleId,
                    Nota = notaNumber,
                    CustomerId = _selectedQueue.CustomerId,
                    SalesPersonId = _selectedQueue.SalesPersonId,
                    OrderDate = DateTime.Now,
                    Status = "SO",
                    Total = grandTotal,
                    Paid = 0,
                    Remaining = grandTotal,
                    Description = $"Dibuat otomatis dari Mobile Queue ({_selectedQueue.CustomerName})",
                    CreatedAt = DateTime.Now
                };

                var newDetails = _currentItems.Select(item => new LocalSalesDetail
                {
                    Id = Guid.NewGuid().ToString(),
                    SaleId = saleId,
                    ProductId = item.ProductId,
                    Qty = item.Qty,
                    Price = item.UnitPrice,
                    Subtotal = item.Subtotal
                }).ToList();

                // Save atomically in local DB transaction
                await db.RunInTransactionAsync(conn =>
                {
                    conn.Insert(newSale);
                    conn.InsertAll(newDetails);

                    // Update Queue Status locally
                    _selectedQueue.Status = "APPROVED";
                    _selectedQueue.TotalAmount = grandTotal;
                    _selectedQueue.ItemsJson = JsonSerializer.Serialize(_currentItems);
                    _selectedQueue.UpdatedAt = DateTime.Now;
                    conn.Update(_selectedQueue);
                });

                // Push status update to Supabase
                _ = SyncService.PushQueueStatusToSupabaseAsync(_selectedQueue.Id, "APPROVED", "SO");

                await ActivityLogService.LogAsync("APPROVE_SO_QUEUE", $"Menyetujui SO Mobile queue '{_selectedQueue.Id}' -> Nota '{notaNumber}'.");

                DialogHelper.ShowInfo($"Sales Order berhasil disetujui! Nota baru '{notaNumber}' telah dibuat di sistem.");
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menyetujui Sales Order: {ex.Message}");
            }
        }

        private async void TombolTolak_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedQueue == null) return;

            var result = MessageBox.Show(
                $"Apakah Anda yakin ingin MENOLAK pesanan dari '{_selectedQueue.CustomerName}'?",
                "Konfirmasi Tolak SO",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var db = await LocalDatabaseService.GetConnection();

                _selectedQueue.Status = "REJECTED";
                _selectedQueue.UpdatedAt = DateTime.Now;
                await db.UpdateAsync(_selectedQueue);

                _ = SyncService.PushQueueStatusToSupabaseAsync(_selectedQueue.Id, "REJECTED", "SO");

                await ActivityLogService.LogAsync("REJECT_SO_QUEUE", $"Menolak SO Mobile queue '{_selectedQueue.Id}'.");

                DialogHelper.ShowInfo("Pesanan telah ditolak.");
                LoadDataAsync();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError($"Gagal menolak Sales Order: {ex.Message}");
            }
        }
    }
}
