using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public class CustomerDisplayItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? OwnerName { get; set; }
        public string? Phone { get; set; }
        public string? JalurPengiriman { get; set; }
        public string SalesPersonName { get; set; } = "-";
        public decimal LimitPiutang { get; set; }
        public LocalCustomer Original { get; set; } = null!;
    }

    public partial class CustomerListPage : UserControl, IRefreshablePage
    {
        public CustomerListPage()
        {
            InitializeComponent();
            LoadDataFromLocal();
        }

        public void RefreshData() => LoadDataFromLocal();

        private async void LoadDataFromLocal()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var customers = await localDb.Table<LocalCustomer>().ToListAsync();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

            var displayItems = customers.Select(c => new CustomerDisplayItem
            {
                Id = c.Id,
                Name = c.Name,
                OwnerName = c.OwnerName,
                Phone = c.Phone,
                JalurPengiriman = c.JalurPengiriman,
                SalesPersonName = salesPersons.FirstOrDefault(s => s.Id == c.SalesPersonId)?.Name ?? "-",
                LimitPiutang = c.LimitPiutang,
                Original = c
            }).ToList();

            TabelCustomer.ItemsSource = displayItems;
            TeksJumlah.Text = $"Total customer: {displayItems.Count}";
        }

        private void TombolTambah_Click(object sender, RoutedEventArgs e)
        {
            var form = new CustomerFormWindow { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                LoadDataFromLocal();
                TeksStatus.Text = "Data tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
            }
        }

        private void TabelCustomer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TabelCustomer.SelectedItem is CustomerDisplayItem selected)
            {
                var form = new CustomerFormWindow(selected.Original) { Owner = Window.GetWindow(this) };
                if (form.ShowDialog() == true)
                {
                    LoadDataFromLocal();
                    TeksStatus.Text = "Data tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
                }
            }
        }
    }
}