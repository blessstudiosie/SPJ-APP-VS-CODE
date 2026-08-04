using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class DashboardPage : UserControl
    {
        public DashboardPage()
        {
            InitializeComponent();
            LoadDataFromLocal();
        }

        public async void LoadDataFromLocal()
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();

                // 1. Data Produk
                var products = await localDb.Table<LocalProduct>().ToListAsync();
                int totalProduk = products.Count;
                decimal totalStokReady = products.Sum(p => p.StokReady);
                decimal totalStokFisik = products.Sum(p => p.StokFisik);

                TeksTotalProduk.Text = totalProduk.ToString("N0");
                TeksStokReady.Text = totalStokReady.ToString("N0");
                TeksStokFisik.Text = totalStokFisik.ToString("N0");

                // 2. Data Omset berdasarkan Bulan Berjalan (DeliveryDate) dan Status (TEMPO / DONE)
                DateTime now = DateTime.Now;
                TeksBulanKirim.Text = $"Kirim: {now.ToString("MMMM yyyy", new CultureInfo("id-ID"))}";

                var sales = await localDb.Table<LocalSale>().ToListAsync();

                decimal omsetBulanIni = sales
                    .Where(s => s.DeliveryDate.HasValue &&
                                s.DeliveryDate.Value.Year == now.Year &&
                                s.DeliveryDate.Value.Month == now.Month &&
                                !string.IsNullOrEmpty(s.Status) &&
                                (s.Status.Equals("TEMPO", StringComparison.OrdinalIgnoreCase) ||
                                 s.Status.Equals("DONE", StringComparison.OrdinalIgnoreCase)))
                    .Sum(s => s.Total);

                TeksOmsetBulanIni.Text = string.Format(new CultureInfo("id-ID"), "Rp {0:N0}", omsetBulanIni);
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal memuat data: " + ex.Message;
            }
        }

        private void BtnBukaProduk_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void BtnSync_Click(object sender, RoutedEventArgs e)
        {
            BtnSync.IsEnabled = false;
            TeksStatus.Text = "Sedang sync...";

            try
            {
                var result = await SyncService.SyncProductsAsync();
                LoadDataFromLocal();

                if (result.Conflicts.Count > 0)
                {
                    TeksStatus.Text = $"Sync selesai. Push: {result.PushedCount}, Pull: {result.PulledCount}. Ada {result.Conflicts.Count} konflik.";

                    var conflictWindow = new ConflictResolutionWindow(result.Conflicts)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    conflictWindow.ShowDialog();

                    LoadDataFromLocal();
                }
                else
                {
                    TeksStatus.Text = $"Sync selesai. Push: {result.PushedCount}, Pull: {result.PulledCount}";
                }
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Sync");
                TeksStatus.Text = "Sync gagal.";
            }
            finally
            {
                BtnSync.IsEnabled = true;
            }
        }
    }
}