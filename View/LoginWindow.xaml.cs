using System.Windows;
using System.Windows.Input;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class LoginWindow : Window
    {
        public bool LoginSuccess { get; private set; } = false;

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
                var salesPersons = await localDb.Table<LocalSalesPerson>().ToListAsync();
                InputNama.ItemsSource = salesPersons;
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal memuat daftar user: " + ex.Message;
            }
        }

        private void InputPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TombolLogin_Click(sender, new RoutedEventArgs());
            }
        }

        private async void TombolLogin_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = "";

            string namaInput = (InputNama.SelectedItem as LocalSalesPerson)?.Name ?? InputNama.Text.Trim();
            string password = InputPassword.Password;

            if (string.IsNullOrWhiteSpace(namaInput) || string.IsNullOrWhiteSpace(password))
            {
                TeksStatus.Text = "Nama dan password wajib diisi.";
                return;
            }

            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                var user = await localDb.Table<LocalSalesPerson>()
                    .Where(s => s.Name == namaInput)
                    .FirstOrDefaultAsync();

                if (user == null || user.Password != password)
                {
                    TeksStatus.Text = "Nama atau password salah.";
                    return;
                }

                CurrentUserService.SetUser(user);
                await ActivityLogService.LogAsync("LOGIN", $"User '{user.Name}' login ke aplikasi");

                LoginSuccess = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TeksStatus.Text = "Gagal login: " + DialogHelper.GetFullErrorDetail(ex);
            }
        }
    }
}