using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class CurrentUserService
    {
        private static LocalSalesPerson? _loggedInUser;
        public static LocalSalesPerson? LoggedInUser
        {
            get => _loggedInUser;
            set => _loggedInUser = value;
        }

        public static void SetUser(LocalSalesPerson? user) => _loggedInUser = user;
        public static void Logout() => _loggedInUser = null;
    }

}