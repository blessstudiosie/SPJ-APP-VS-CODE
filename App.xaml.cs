using SPJ_APP.Service;
using System.IO;
using System.Text;
using System.Windows;
using SPJ_APP.View;

namespace SPJ_APP
{
    public partial class App : Application
    {
        private BackgroundSyncService? _backgroundSyncService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Tangkap Unhandled Exception pada UI Thread
            DispatcherUnhandledException += (sender, args) =>
            {
                LogAndShowError("Dispatcher Unhandled Exception", args.Exception);
                args.Handled = true;
            };

            // 2. Tangkap Unhandled Exception pada AppDomain (Background Threads)
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                if (args.ExceptionObject is System.Exception ex)
                {
                    LogAndShowError("AppDomain Unhandled Exception", ex);
                }
            };

            // 3. Tangkap Unobserved Task Exceptions (Async Background Tasks)
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                LogAndShowError("TaskScheduler Unobserved Exception", args.Exception);
                args.SetObserved();
            };

            try
            {
                var loginWindow = new LoginWindow();
                bool? loginResult = loginWindow.ShowDialog();

                if (loginResult == true && CurrentUserService.LoggedInUser != null)
                {
                    try
                    {
                        // Inisialisasi Background Sync setelah login berhasil
                        _backgroundSyncService = BackgroundSyncService.Instance;
                        _backgroundSyncService.Start();
                    }
                    catch (System.Exception ex)
                    {
                        LogAndShowError("Gagal Memulai Background Sync", ex);
                    }

                    var mainWindow = new MainWindow();
                    MainWindow = mainWindow;
                    mainWindow.Show();
                }
                else
                {
                    Shutdown();
                }
            }
            catch (System.Exception ex)
            {
                LogAndShowError("Fatal Startup Error", ex);
                Shutdown();
            }
        }

        public static void LogAndShowError(string contextTitle, System.Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== ERROR DETAIL [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ===");
            sb.AppendLine($"Konstruk: {contextTitle}");
            sb.AppendLine($"Pesan: {ex.Message}");
            sb.AppendLine($"Tipe Exception: {ex.GetType().FullName}");
            sb.AppendLine($"Stack Trace:\n{ex.StackTrace}");

            var inner = ex.InnerException;
            int depth = 1;
            while (inner != null)
            {
                sb.AppendLine($"\n--- Inner Exception ({depth}) ---");
                sb.AppendLine($"Pesan: {inner.Message}");
                sb.AppendLine($"Tipe: {inner.GetType().FullName}");
                sb.AppendLine($"Stack Trace:\n{inner.StackTrace}");
                inner = inner.InnerException;
                depth++;
            }

            string fullErrorText = sb.ToString();

            // Simpan ke file log crash lokal
            try
            {
                string appDataPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SPJ APP");
                Directory.CreateDirectory(appDataPath);
                string logFile = System.IO.Path.Combine(appDataPath, "crash_logs.txt");
                File.AppendAllText(logFile, fullErrorText + "\n\n");
            }
            catch
            {
                // Abaikan kesalahan penulisan log
            }

            // Tampilkan dialog error ke user secara aman di UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                DialogHelper.ShowError(fullErrorText, $"Error: {contextTitle}");
            });
        }
    }
}