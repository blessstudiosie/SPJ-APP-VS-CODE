using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class SalesPersonListPage : UserControl, IRefreshablePage
    {
        public SalesPersonListPage()
        {
            InitializeComponent();
            LoadDataFromLocal();
        }

        public void RefreshData() => LoadDataFromLocal();

        private async void LoadDataFromLocal()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var items = await localDb.Table<LocalSalesPerson>().ToListAsync();

            TabelSales.ItemsSource = items;
            TeksJumlah.Text = $"Total sales: {items.Count}";
        }

        private void TombolTambah_Click(object sender, RoutedEventArgs e)
        {
            var form = new SalesPersonFormWindow { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                LoadDataFromLocal();
                TeksStatus.Text = "Data tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
            }
        }

        private void TabelSales_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TabelSales.SelectedItem is LocalSalesPerson selected)
            {
                var form = new SalesPersonFormWindow(selected) { Owner = Window.GetWindow(this) };
                if (form.ShowDialog() == true)
                {
                    LoadDataFromLocal();
                    TeksStatus.Text = "Data tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
                }
            }
        }
    }
}