using System.Windows;

namespace SPJ_APP.View
{
    public partial class ErrorDialogWindow : Window
    {
        public string ErrorTitle { get; set; }
        public string ErrorMessage { get; set; }

        public ErrorDialogWindow(string message, string title)
        {
            InitializeComponent();
            ErrorTitle = title;
            ErrorMessage = message;
            DataContext = this;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(ErrorMessage);
                MessageBox.Show("Detail error telah berhasil disalin ke Clipboard!", "Tersalin", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Gagal menyalin detail error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

