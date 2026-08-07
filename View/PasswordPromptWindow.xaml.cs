using System.Windows;

namespace SPJ_APP.View
{
    public partial class PasswordPromptWindow : Window
    {
        public string Password => PasswordBox.Password;

        public PasswordPromptWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow; 
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
