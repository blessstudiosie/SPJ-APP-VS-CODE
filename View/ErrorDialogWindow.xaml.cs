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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
