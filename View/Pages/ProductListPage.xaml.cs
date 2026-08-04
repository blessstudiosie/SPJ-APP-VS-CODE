using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;
using System.Linq;
using Microsoft.Win32;
using System.Collections.Generic;

namespace SPJ_APP.View.Pages
{
    public partial class ProductListPage : UserControl, IRefreshablePage
    {
        public ProductListPage()
        {
            InitializeComponent();
            Loaded += ProductListPage_Loaded;
        }

        private async void ProductListPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadKategoriAsync();
            LoadDataFromLocal();
        }

        public void RefreshData() => LoadDataFromLocal();

        private async System.Threading.Tasks.Task LoadKategoriAsync()
        {
            var localDb = await LocalDatabaseService.GetConnection();
            var categories = await localDb.Table<LocalProduct>().ToListAsync();
            
            var distinctCategories = categories
                .Where(p => !string.IsNullOrWhiteSpace(p.Kategori))
                .Select(p => p.Kategori)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            distinctCategories.Insert(0, "SEMUA");
            ComboKategori.ItemsSource = distinctCategories;
            ComboKategori.SelectedIndex = 0;
        }

        private async void LoadDataFromLocal()
        {
            if (!IsLoaded) return;

            var localDb = await LocalDatabaseService.GetConnection();
            var query = localDb.Table<LocalProduct>();

            var searchText = TeksPencarian.Text;
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                query = query.Where(p => p.Name.ToLower().Contains(searchText.ToLower()));
            }

            var selectedCategory = ComboKategori.SelectedItem as string;
            if (!string.IsNullOrWhiteSpace(selectedCategory) && selectedCategory != "SEMUA")
            {
                query = query.Where(p => p.Kategori == selectedCategory);
            }

            var products = await query.ToListAsync();

            TabelProduk.ItemsSource = products;
            TeksJumlah.Text = $"Total produk: {products.Count}";
        }
        
        private void TeksPencarian_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadDataFromLocal();
        }

        private void ComboKategori_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadDataFromLocal();
        }

        private void TombolExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var products = TabelProduk.ItemsSource as List<LocalProduct>;
            if (products == null || !products.Any())
            {
                MessageBox.Show("Tidak ada data untuk di-export.", "Informasi", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PDF Document (*.pdf)|*.pdf",
                FileName = $"Daftar Harga-{DateTime.Now:yyyyMMdd}.pdf"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    ReportService.GeneratePriceListPdf(products, saveFileDialog.FileName);
                    MessageBox.Show($"PDF berhasil disimpan di:\n{saveFileDialog.FileName}", "Sukses", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Terjadi kesalahan saat membuat PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void TombolTambah_Click(object sender, RoutedEventArgs e)
        {
            var form = new ProductFormWindow { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                LoadDataFromLocal();
                TeksStatus.Text = "Produk tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
            }
        }

        private void TombolBarangMasuk_Click(object sender, RoutedEventArgs e)
        {
            var form = new GoodsReceiptFormWindow { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                LoadDataFromLocal();
                TeksStatus.Text = "Barang masuk tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
            }
        }

        private void TombolOpname_Click(object sender, RoutedEventArgs e)
        {
            var form = new StockOpnameWindow { Owner = Window.GetWindow(this) };
            if (form.ShowDialog() == true)
            {
                LoadDataFromLocal();
                TeksStatus.Text = "Stok opname tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
            }
        }

        private void TabelProduk_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TabelProduk.SelectedItem is LocalProduct selected)
            {
                var form = new ProductFormWindow(selected) { Owner = Window.GetWindow(this) };
                if (form.ShowDialog() == true)
                {
                    LoadDataFromLocal();
                    TeksStatus.Text = "Produk tersimpan lokal. Gunakan menu Sync Sekarang untuk kirim ke server.";
                }
            }
        }
    }
}