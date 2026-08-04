using SPJ_APP.Service;
using System.Windows;

namespace SPJ_APP
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            BackgroundSyncService.Instance.Start();

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
        }

        protected override void OnExit(ExitEventArgs e)
        {
            BackgroundSyncService.Instance.Stop();
            base.OnExit(e);
        }
    }
}