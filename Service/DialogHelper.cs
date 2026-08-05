using System.Windows;
using SPJ_APP.View;

namespace SPJ_APP.Service
{
    public static class DialogHelper
    {
        public static void ShowError(string message, string title = "Error")
        {
            var errorWindow = new ErrorDialogWindow(message, title)
            {
                Owner = Application.Current.MainWindow
            };
            errorWindow.ShowDialog();
        }

        public static void ShowInfo(string message, string title = "Informasi")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public static bool ShowConfirm(string message, string title = "Konfirmasi")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public static string GetFullErrorDetail(System.Exception ex)
        {
            string detail = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                detail += "\n\nInner: " + inner.Message;
                inner = inner.InnerException;
            }
            return detail;
        }
    }
}