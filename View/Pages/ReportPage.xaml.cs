using System.Windows.Controls;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class ReportPage : UserControl
    {
        public ReportPage()
        {
            InitializeComponent();
            Loaded += ReportPage_Loaded;
        }

        private async void ReportPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            InputMulai.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            InputSelesai.SelectedDate = DateTime.Today;
            await LoadReportsAsync();
        }

        private async System.Threading.Tasks.Task LoadReportsAsync()
        {
            var startDate = InputMulai.SelectedDate;
            var endDate = InputSelesai.SelectedDate;

            var kinerja = await ReportService.GetSalesPerformanceAsync(startDate, endDate);
            TabelKinerja.ItemsSource = kinerja;

            var gaji = await ReportService.GetSalaryPerformanceAsync(startDate, endDate);
            TabelGaji.ItemsSource = gaji;
        }

        private async void TombolFilter_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            await LoadReportsAsync();
        }
    }
}
