using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class NotaListPage : UserControl, IRefreshablePage
    {
        private int _currentPage = 1;
        private int _totalCount = 0;

        public NotaListPage()
        {
            InitializeComponent();
            Loaded += NotaListPage_Loaded;
        }

        private void NotaListPage_Loaded(object sender, RoutedEventArgs e)
        {
            InputMulai.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            InputSelesai.SelectedDate = DateTime.Today;
            LoadPage();
        }

        public void RefreshData() => LoadPage();

        private List<string> GetSelectedStatuses()
        {
            var list = new List<string>();
            if (ChkStatusSo?.IsChecked == true) list.Add("SO");
            if (ChkStatusOnProses?.IsChecked == true) list.Add("ON PROSES");
            if (ChkStatusDalamPengiriman?.IsChecked == true) list.Add("DALAM PENGIRIMAN");
            if (ChkStatusTempo?.IsChecked == true) list.Add("TEMPO");
            if (ChkStatusDone?.IsChecked == true) list.Add("DONE");
            return list;
        }

        private void UpdateStatusButtonHeader()
        {
            if (TombolFilterStatus == null) return;
            var selected = GetSelectedStatuses();
            if (selected.Count == 5 || selected.Count == 0)
            {
                TombolFilterStatus.Content = "Semua Status (5) ▼";
            }
            else
            {
                TombolFilterStatus.Content = $"Status ({selected.Count} Terpilih) ▼";
            }
        }

        private async void LoadPage()
        {
            var startDate = InputMulai.SelectedDate;
            var endDate = InputSelesai.SelectedDate;
            var searchQuery = InputCariCustomer.Text;
            var selectedStatuses = GetSelectedStatuses();

            var (items, totalCount) = await SalesQueryService.GetPagedSalesAsync(_currentPage, startDate, endDate, searchQuery, selectedStatuses);
            _totalCount = totalCount;

            TabelNota.ItemsSource = items;

            int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)SalesQueryService.PageSize));
            TeksHalaman.Text = $"Halaman {_currentPage} dari {totalPages} (Total: {totalCount} nota)";

            TombolSebelumnya.IsEnabled = _currentPage > 1;
            TombolBerikutnya.IsEnabled = _currentPage < totalPages;
        }
        
        private void TombolFilter_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadPage();
        }

        private void InputCariCustomer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _currentPage = 1;
                LoadPage();
            }
        }

        private void FilterStatus_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            UpdateStatusButtonHeader();
            _currentPage = 1;
            LoadPage();
        }

        private void TombolPilihSemuaStatus_Click(object sender, RoutedEventArgs e)
        {
            ChkStatusSo.IsChecked = true;
            ChkStatusOnProses.IsChecked = true;
            ChkStatusDalamPengiriman.IsChecked = true;
            ChkStatusTempo.IsChecked = true;
            ChkStatusDone.IsChecked = true;
            UpdateStatusButtonHeader();
            _currentPage = 1;
            LoadPage();
        }

        private void TombolResetStatus_Click(object sender, RoutedEventArgs e)
        {
            ChkStatusSo.IsChecked = false;
            ChkStatusOnProses.IsChecked = false;
            ChkStatusDalamPengiriman.IsChecked = false;
            ChkStatusTempo.IsChecked = false;
            ChkStatusDone.IsChecked = false;
            UpdateStatusButtonHeader();
            _currentPage = 1;
            LoadPage();
        }



        private void TombolSebelumnya_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadPage();
            }
        }

        private void TombolBerikutnya_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = Math.Max(1, (int)Math.Ceiling(_totalCount / (double)SalesQueryService.PageSize));
            if (_currentPage < totalPages)
            {
                _currentPage++;
                LoadPage();
            }
        }

        private void TombolTambah_Click(object sender, RoutedEventArgs e)
        {
            var form = new NotaFormWindow { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                _currentPage = 1;
                LoadPage();
            }
        }

        private void TabelNota_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TabelNota.SelectedItem is SaleDisplayItem selected)
            {
                var form = new NotaFormWindow(selected.Original) { Owner = Window.GetWindow(this) };
                if (form.ShowDialog() == true)
                {
                    LoadPage();
                }
            }
        }
    }
}