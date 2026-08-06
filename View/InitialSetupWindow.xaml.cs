using System.Windows;

namespace SPJ_APP.View
{
    public enum InitialSetupAction
    {
        NotSet,         // No setup needed, proceed to normal startup
        SyncAndLogin,   // Sync is needed, then login
        AdminCreated,   // Admin was created, user is already set, proceed to main app
        ExitApplication // User chose to exit or cancelled
    }

    public partial class InitialSetupWindow : Window
    {
        public InitialSetupAction SelectedAction { get; private set; } = InitialSetupAction.ExitApplication; // Default to exit

        public InitialSetupWindow()
        {
            InitializeComponent();
        }

        private void CreateAdminButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = InitialSetupAction.AdminCreated; // This will be handled by the service
            DialogResult = true;
            Close();
        }

        private void SyncFromServerButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = InitialSetupAction.SyncAndLogin;
            DialogResult = true;
            Close();
        }
    }
}
