using SPJ_APP.Model;

namespace SPJ_APP.Service
{
    public static class CurrentUserService
    {
        private static LocalSalesPerson? _loggedInUser;
        public static LocalSalesPerson? LoggedInUser => _loggedInUser;

        public static void SetUser(LocalSalesPerson user) => _loggedInUser = user;
    }
}