using System.Windows;
using SPJ_APP.View;

namespace SPJ_APP
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += (sender, args) =>
            {
                var ex = args.Exception;
                string detail = ex.Message;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    detail += "\n\n--- Inner ---\n" + ex.Message;
                }
                MessageBox.Show(detail, "Error Detail", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };

            var loginWindow = new LoginWindow();
            bool? loginResult = loginWindow.ShowDialog();

            if (loginResult == true)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            else
            {
                Shutdown();
            }
        }
    }
}