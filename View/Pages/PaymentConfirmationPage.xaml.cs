using System.Windows;
using System.Windows.Controls;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View.Pages
{
    public partial class PaymentConfirmationPage : UserControl, IRefreshablePage
    {
        public PaymentConfirmationPage()
        {
            InitializeComponent();
            RefreshData();
        }

        public async void RefreshData()
        {
            try
            {
                var payments = await SyncService.PullPendingPaymentsAsync();
                TabelPembayaran.ItemsSource = payments;
                TeksStatus.Text = $"Menunggu konfirmasi: {payments.Count} pembayaran";
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Memuat Pembayaran");
            }
        }

        private void TombolMuat_Click(object sender, RoutedEventArgs e) => RefreshData();

        private async void TombolSetujui_Click(object sender, RoutedEventArgs e) =>
            await ProcessConfirmation(sender, true);

        private async void TombolTolak_Click(object sender, RoutedEventArgs e) =>
            await ProcessConfirmation(sender, false);

        private async Task ProcessConfirmation(object sender, bool approve)
        {
            if (sender is not Button { Tag: LocalPayment payment }) return;
            if (!DialogHelper.ShowConfirm($"{(approve ? "Setujui" : "Tolak")} pembayaran {payment.Amount:N0}?")) return;
            try
            {
                await SyncService.ConfirmPaymentAsync(payment, approve);
                RefreshData();
            }
            catch (Exception ex)
            {
                DialogHelper.ShowError(DialogHelper.GetFullErrorDetail(ex), "Gagal Mengonfirmasi Pembayaran");
            }
        }
    }
}
