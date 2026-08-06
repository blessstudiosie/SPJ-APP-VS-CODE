using SPJ_APP.Model;
using SPJ_APP.Service;
using System.Windows;

namespace SPJ_APP.View
{
    public partial class CreateAdminWindow : Window
    {
        public LocalSalesPerson? NewAdmin { get; private set; }

        public CreateAdminWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text;
            var password = PasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(name))
            {
                DialogHelper.ShowError("Nama Admin tidak boleh kosong.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                DialogHelper.ShowError("Password tidak boleh kosong.");
                return;
            }

            if (password != confirmPassword)
            {
                DialogHelper.ShowError("Password dan konfirmasi password tidak cocok.");
                return;
            }

            NewAdmin = new LocalSalesPerson
            {
                Id = System.Guid.NewGuid().ToString(),
                Name = name,
                Password = password, // Note: In a real app, this should be hashed.
                Role = "Admin",
                IsSynced = false
            };

            DialogResult = true;
            Close();
        }
    }
}
