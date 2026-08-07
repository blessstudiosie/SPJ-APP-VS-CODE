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

                // Pastikan Akun Developer Lokal (blessstudiosie) Selalu Ada di Perangkat Ini (Tidak Di-push ke Supabase)
                var devUser = _users.FirstOrDefault(u => string.Equals(u.Name, "blessstudiosie", StringComparison.OrdinalIgnoreCase));
                if (devUser == null)
                {
                    devUser = new LocalSalesPerson
                    {
                        Id = "dev-blessstudiosie-id",
                        Name = "blessstudiosie",
                        Password = PasswordHasherService.HashPassword("jemblem1993"),
                        Role = "DEVELOPER",
                        IsSynced = true // Tandai synced agar tidak didorong ke Supabase oleh SyncService
                    };
                    await localDb.InsertOrReplaceAsync(devUser);
                    _users = await localDb.Table<LocalSalesPerson>().ToListAsync();
                }


                // jika database lokal masih kosong, coba tarik dari Supabase atau buat default Admin
                if (_users.Count <= 1)
                {
                    try
                    {
                        var supabase = await SupabaseService.GetClient();
                        var remoteList = (await supabase.From<SalesPerson>().Get()).Models;
                        if (remoteList.Count > 0)
                        {
                            foreach (var remote in remoteList)
                            {
                                if (string.Equals(remote.Name, "Developer", StringComparison.OrdinalIgnoreCase)) continue;

                                var localUser = new LocalSalesPerson
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = remote.Name,
                                    Phone = remote.Phone,
                                    Email = remote.Email,
                                    TargetOmset = remote.TargetOmset,
                                    Role = remote.Role ?? "SALES",
                                    Password = remote.Password,
                                    IsSynced = true
                                };
                                await localDb.InsertAsync(localUser);
                            }
                            _users = await localDb.Table<LocalSalesPerson>().ToListAsync();
                        }
                    }
                    catch
                    {
                        // Supabase offline / offline first fallback
                    }

                    // Jika belum ada admin, tambahkan default admin
                    if (!_users.Any(u => u.Role == "ADMIN" || u.Role == "Admin"))
                    {
                        var defaultAdmin = new LocalSalesPerson
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = "Admin",
                            Password = PasswordHasherService.HashPassword("admin123"),
                            Role = "ADMIN",
                            IsSynced = false
                        };
                        await localDb.InsertAsync(defaultAdmin);
                        _users.Add(defaultAdmin);
                    }
                }

                InputNama.ItemsSource = _users;
                if (_users.Count > 0)
                {
                    InputNama.SelectedIndex = 0;
                }
            }
            catch (System.Exception ex)
            {
                TeksStatus.Text = $"Gagal memuat data user lokal: {ex.Message}";
                TombolLogin.IsEnabled = false;
            }
        }


        private async void TombolLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TeksStatus.Text = "";
                string? inputName = InputNama.Text;
                string inputPassword = InputPassword.Password;

                if (string.IsNullOrWhiteSpace(inputName))
                {
                    TeksStatus.Text = "Nama user tidak boleh kosong.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(inputPassword))
                {
                    TeksStatus.Text = "Password tidak boleh kosong. Silakan masukkan password Anda.";
                    return;
                }


                LocalSalesPerson? user = InputNama.SelectedItem as LocalSalesPerson;
                if (user == null)
                {
                    string searchName = inputName.Trim();
                    user = _users.FirstOrDefault(u => string.Equals(u.Name?.Trim(), searchName, System.StringComparison.OrdinalIgnoreCase));
                }

                if (user == null)
                {
                    TeksStatus.Text = $"Nama user '{inputName}' tidak ditemukan.";
                    return;
                }

                // Gunakan PasswordHasherService.VerifyPassword yang aman & fleksibel
                if (!PasswordHasherService.VerifyPassword(inputPassword, user.Password))
                {
                    TeksStatus.Text = "Password salah. Silakan periksa kembali (coba default: admin123 / ganti123 / 123456 / dev123).";
                    return;
                }

                // Login sukses
                CurrentUserService.SetUser(user);

                try
                {
                    await ActivityLogService.LogAsync("LOGIN", $"User '{user.Name}' berhasil login.");
                }
                catch
                {
                    // Abaikan kesalahan log aktivitas agar login tetap berjalan lancar
                }

                DialogResult = true;
                Close();
            }
            catch (System.Exception ex)
            {
                TeksStatus.Text = $"Terjadi kesalahan saat login: {ex.Message}";
                App.LogAndShowError("Proses Login Terhenti", ex);
            }
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