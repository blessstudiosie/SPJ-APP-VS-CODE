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

        private async void LoadPage()
        {
            var startDate = InputMulai.SelectedDate;
            var endDate = InputSelesai.SelectedDate;

            var (items, totalCount) = await SalesQueryService.GetPagedSalesAsync(_currentPage, startDate, endDate);
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