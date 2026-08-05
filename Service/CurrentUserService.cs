using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class CurrentUserService
    {
        public static LocalSalesPerson? LoggedInUser { get; private set; }

        public static void SetUser(LocalSalesPerson user)
        {
            LoggedInUser = user;
        }

        public static void ClearUser()
        {
            LoggedInUser = null;
        }

        public static string CurrentUserName => LoggedInUser?.Name ?? "Unknown";
    }
}