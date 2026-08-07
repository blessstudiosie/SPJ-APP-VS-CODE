using System;
using System.Windows;
using SPJ_APP.Model;
using SPJ_APP.Service;

namespace SPJ_APP.View
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
            Loaded += ChangePasswordWindow_Loaded;
        }

        private void ChangePasswordWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var user = CurrentUserService.LoggedInUser;
            if (user != null)
            {
                TeksNamaUser.Text = $"{user.Name} ({user.Role})";
            }
            else
            {
                TeksNamaUser.Text = "Pengguna Tidak Terdaftar";
            }
            InputPasswordLama.Focus();
        }

        private async void TombolSimpan_Click(object sender, RoutedEventArgs e)
        {
            TeksStatus.Text = string.Empty;

            var currentUser = CurrentUserService.LoggedInUser;
            if (currentUser == null)
            {
                TeksStatus.Text = "Tidak ada pengguna yang sedang aktif.";
                return;
            }

            string passLama = InputPasswordLama.Password;
            string passBaru = InputPasswordBaru.Password;
            string passKonfirmasi = InputKonfirmasiPassword.Password;

            if (string.IsNullOrWhiteSpace(passLama))
            {
                TeksStatus.Text = "Password lama tidak boleh kosong.";
                InputPasswordLama.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(passBaru))
            {
                TeksStatus.Text = "Password baru tidak boleh kosong.";
                InputPasswordBaru.Focus();
                return;
            }

            if (passBaru.Length < 4)
            {
                TeksStatus.Text = "Password baru minimal terdiri dari 4 karakter.";
                InputPasswordBaru.Focus();
                return;
            }

            if (passBaru != passKonfirmasi)
            {
                TeksStatus.Text = "Konfirmasi password baru tidak cocok.";
                InputKonfirmasiPassword.Focus();
                return;
            }

            // Verifikasi Password Lama
            bool isOldPasswordValid = PasswordHasherService.VerifyPassword(passLama, currentUser.Password);
            if (!isOldPasswordValid)
            {
                TeksStatus.Text = "Password lama yang Anda masukkan salah.";
                InputPasswordLama.Focus();
                return;
            }

            TombolSimpan.IsEnabled = false;
            TombolBatal.IsEnabled = false;
            TeksStatus.Foreground = System.Windows.Media.Brushes.MediumBlue;
            TeksStatus.Text = "Menyimpan password baru...";

            try
            {
                string newHashedPassword = PasswordHasherService.HashPassword(passBaru);

                var db = await LocalDatabaseService.GetConnection();
                var userInDb = await db.Table<LocalSalesPerson>()
                                       .Where(u => u.Id == currentUser.Id || u.Name == currentUser.Name)
                                       .FirstOrDefaultAsync();

                if (userInDb != null)
                {
                    userInDb.Password = newHashedPassword;
                    userInDb.IsSynced = false;
                    await db.UpdateAsync(userInDb);
                }

                // Update memori akun yang sedang login
                currentUser.Password = newHashedPassword;

                // Sync ke server Supabase jika bukan akun developer khusus lokal
                if (currentUser.Role != "DEVELOPER" && currentUser.Name != "blessstudiosie")
                {
                    _ = SyncService.SyncSalesPersonsAsync();
                }

                await ActivityLogService.LogAsync("CHANGE_PASSWORD", $"User '{currentUser.Name}' berhasil mengubah password.");

                DialogHelper.ShowInfo("Password Anda berhasil diperbarui! Gunakan password baru ini untuk login berikutnya.", "Ubah Password Berhasil");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                TeksStatus.Foreground = System.Windows.Media.Brushes.Red;
                TeksStatus.Text = $"Gagal mengubah password: {ex.Message}";
            }
            finally
            {
                TombolSimpan.IsEnabled = true;
                TombolBatal.IsEnabled = true;
            }
        }

        private void TombolBatal_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
