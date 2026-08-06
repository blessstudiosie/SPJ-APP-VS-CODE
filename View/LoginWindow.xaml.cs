using SPJ_APP.Model;
using SPJ_APP.Service;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SPJ_APP.View
{
    public partial class LoginWindow : Window
    {
        private List<LocalSalesPerson> _users = new();

        public LoginWindow()
        {
            InitializeComponent();
            Loaded += LoginWindow_Loaded;
        }

        private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                _users = await localDb.Table<LocalSalesPerson>().ToListAsync();
                InputNama.ItemsSource = _users;
            }
            catch (System.Exception ex)
            {
                TeksStatus.Text = $"Gagal memuat data user lokal: {ex.Message}";
                TombolLogin.IsEnabled = false;
            }
        }

        private async void TombolLogin_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "";
            string? inputName = InputNama.Text;
            string inputPassword = InputPassword.Password;

            if (string.IsNullOrWhiteSpace(inputName))
            {
                TeksStatus.Text = "Nama user tidak boleh kosong.";
                return;
            }

            var user = _users.FirstOrDefault(u => u.Name.Equals(inputName, System.StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                TeksStatus.Text = "Nama user tidak ditemukan.";
                return;
            }

            // TODO: Ganti validasi plain text dengan verifikasi hash (e.g., BCrypt)
            if (user.Password != inputPassword)
            {
                TeksStatus.Text = "Password salah.";
                return;
            }

            // Login sukses
            CurrentUserService.SetUser(user);
            await ActivityLogService.LogAsync("LOGIN", $"User '{user.Name}' berhasil login.");
            DialogResult = true;
            Close();
        }

        private void InputPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TombolLogin_Click(sender, new RoutedEventArgs());
            }
        }
    }
}