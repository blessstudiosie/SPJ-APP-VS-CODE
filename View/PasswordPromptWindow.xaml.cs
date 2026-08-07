using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class PasswordPromptWindow : Window
    {
        public string Password => PasswordBox.Password;

        public PasswordPromptWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Meminta konfirmasi password otorisasi pengguna dengan role MANAGER, OWNER, DEVELOPER, atau ADMIN.
        /// Mengembalikan true jika password valid, atau false jika dibatalkan / salah.
        /// </summary>
        public static async Task<bool> VerifyManagerOrOwnerPasswordAsync(Window ownerWindow, string actionDescription = "menimpa data lokal")
        {
            var prompt = new PasswordPromptWindow
            {
                Owner = ownerWindow
            };
            prompt.TeksDeskripsiAksi.Text = $"Perhatian: Aksi {actionDescription} memerlukan otorisasi.";

            if (prompt.ShowDialog() != true)
            {
                return false;
            }

            string enteredPassword = prompt.Password;
            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                DialogHelper.ShowError("Password otorisasi tidak boleh kosong.");
                return false;
            }

            // 1. Cek pengguna yang sedang login di sesi aktif
            var currentUser = CurrentUserService.LoggedInUser;
            if (currentUser != null && 
                IsManagerRole(currentUser.Role) && 
                string.Equals(currentUser.Password, enteredPassword))
            {
                return true;
            }

            // 2. Cek database lokal sales_persons untuk akun apapun yang ber-role MANAGER, OWNER, DEVELOPER, atau ADMIN
            try
            {
                var localDb = await LocalDatabaseService.GetConnection();
                var allSales = await localDb.Table<LocalSalesPerson>().ToListAsync();

                var authorizedUser = allSales.FirstOrDefault(sp => 
                    IsManagerRole(sp.Role) && 
                    string.Equals(sp.Password, enteredPassword));

                if (authorizedUser != null)
                {
                    return true;
                }
            }
            catch
            {
                // Fallback jika database lokal belum terinisialisasi
            }

            DialogHelper.ShowError($"Password otorisasi salah! Anda tidak memiliki akses untuk {actionDescription}.");
            return false;
        }

        private static bool IsManagerRole(string? role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            string r = role.Trim().ToUpperInvariant();
            return r == "MANAGER" || r == "OWNER" || r == "DEVELOPER" || r == "ADMIN";
        }
    }
}
