using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class PaymentFormWindow : Window
    {
        private readonly LocalSale _sale;
        private bool _isFormatting = false;

        public PaymentFormWindow(LocalSale sale)
        {
            InitializeComponent();
            _sale = sale;

            TeksInfoNota.Text = $"Nota: {sale.Nota}";
            TeksSisaTagihan.Text = $"Total: {sale.Total:N0} | Sudah Dibayar: {sale.Paid:N0} | Sisa: {sale.Remaining:N0}";

            InputMetode.SelectedIndex = 0;
        }

        private void InputJumlah_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;
            if (sender is not TextBox textBox) return;

            _isFormatting = true;

            int caretPosition = textBox.CaretIndex;
            int lengthBefore = textBox.Text.Length;

            string digitsOnly = new string(textBox.Text.Where(char.IsDigit).ToArray());

            if (string.IsNullOrEmpty(digitsOnly))
            {
                textBox.Text = "";
            }
            else
            {
                if (long.TryParse(digitsOnly, out long value))
                {
                    textBox.Text = value.ToString("N0", new CultureInfo("id-ID"));
                }
            }

            int lengthAfter = textBox.Text.Length;
            int newCaretPosition = caretPosition + (lengthAfter - lengthBefore);
            textBox.CaretIndex = Math.Max(0, Math.Min(newCaretPosition, textBox.Text.Length));

            _isFormatting = false;
        }

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string cleanJumlah = InputJumlah.Text.Replace(".", "");
                if (!decimal.TryParse(cleanJumlah, out decimal jumlah) || jumlah <= 0)
                {
                    DialogHelper.ShowError("Jumlah bayar harus angka positif.");
                    return;
                }

                if (jumlah > _sale.Remaining)
                {
                    bool confirmLebih = DialogHelper.ShowConfirm(
                        $"Jumlah bayar ({jumlah:N0}) lebih besar dari sisa tagihan ({_sale.Remaining:N0}). Tetap lanjutkan?",
                        "Konfirmasi");
                    if (!confirmLebih) return;
                }

                string? metode = (InputMetode.SelectedItem as ComboBoxItem)?.Content as string ?? "Tunai";

                var localDb = await LocalDatabaseService.GetConnection();

                var payment = new LocalPayment
                {
                    Id = Guid.NewGuid().ToString(),
                    SaleId = _sale.Id,
                    PaymentDate = DateTime.Now,
                    Amount = jumlah,
                    PaymentMethod = metode,
                    Status = "APPROVED",
                    Notes = InputCatatan.Text,
                    IsSynced = false
                };

               await localDb.RunInTransactionAsync(conn =>
{
    conn.Insert(payment);

    _sale.Paid += jumlah;
    _sale.Remaining = Math.Max(0, _sale.Total - _sale.Paid);
    _sale.Status = _sale.Remaining == 0 ? "DONE" : "TEMPO";
    _sale.UpdatedAt = DateTime.Now;
    _sale.IsSynced = false;
    conn.Update(_sale);
});

                DialogHelper.ShowInfo("Pembayaran berhasil dicatat lokal. Gunakan menu Sync Sekarang untuk kirim ke server.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Menyimpan");
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShortcutSave_Executed(object sender, ExecutedRoutedEventArgs e) => TombolSimpan_Click(sender, new RoutedEventArgs());
        private void ShortcutClose_Executed(object sender, ExecutedRoutedEventArgs e) => TombolBatal_Click(sender, new RoutedEventArgs());
    }
}
