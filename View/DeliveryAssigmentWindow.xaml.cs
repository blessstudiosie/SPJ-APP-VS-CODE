using System.Windows;
using SPJ_APP.Model;
using SPJ_APP.Service;
using SPJ_APP.View.Pages;

namespace SPJ_APP.View
{
    public partial class DeliveryAssignmentWindow : Window
    {
        private readonly List<DeliverySelectableItem> _selectedSales;

        public DeliveryAssignmentWindow(List<DeliverySelectableItem> selectedSales)
        {
            InitializeComponent();
            _selectedSales = selectedSales;
            TeksJumlahNota.Text = $"{selectedSales.Count} nota akan dimasukkan ke pengiriman ini.";
            Loaded += DeliveryAssignmentWindow_Loaded;
        }

        private async void DeliveryAssignmentWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();

            InputSopir.ItemsSource = salesPersons;
            InputHelper.ItemsSource = salesPersons;
            InputChecker.ItemsSource = salesPersons;
        }

        private async void TombolBuat_Click(object sender, RoutedEventArgs e)
        {
            if (InputSopir.SelectedItem is not LocalSalesPerson sopir)
            {
                DialogHelper.ShowError("Pilih sopir terlebih dahulu.");
                return;
            }

            if (InputChecker.SelectedItem is not LocalSalesPerson checker)
            {
                DialogHelper.ShowError("Pilih checker (penanggung jawab) terlebih dahulu.");
                return;
            }

            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                string deliveryId = Guid.NewGuid().ToString();
                string helperId = (InputHelper.SelectedItem as LocalSalesPerson)?.Id ?? "";

                var delivery = new LocalDelivery
                {
                    Id = deliveryId,
                    DeliveryNumber = CreateDeliveryNumber(),
                    DriverId = sopir.Id,
                    HelperId = string.IsNullOrEmpty(helperId) ? null : helperId,
                    CheckerId = checker.Id,
                    Status = "OPEN",
                    UpdatedAt = DateTime.Now,
                    IsSynced = false
                };

                await localDb.RunInTransactionAsync(conn =>
                {
                    conn.Insert(delivery);

                    foreach (var item in _selectedSales)
                    {
                        var sale = item.Original.Original;
                        sale.Status = "DALAM PENGIRIMAN";
                        sale.UpdatedAt = DateTime.Now;
                        sale.IsSynced = false;
                        conn.Update(sale);

                        var detail = new LocalDeliveryDetail
                        {
                            Id = Guid.NewGuid().ToString(),
                            DeliveryId = deliveryId,
                            SaleId = sale.Id
                        };
                        conn.Insert(detail);
                    }
                });

                DialogHelper.ShowInfo($"Nota Pengiriman '{delivery.DeliveryNumber}' berhasil dibuat dengan {_selectedSales.Count} nota.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Membuat Pengiriman");
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static string CreateDeliveryNumber() =>
            $"{DateTime.Now:ddMMyy-HHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}";
    }
}
