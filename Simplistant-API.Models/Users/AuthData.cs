using Simplistant_API.Repository;

namespace Simplistant_API.Data.Users
{
    [HasStringKey("Username", isUnique: false)]
    public class AuthData : DataItem
    {
        public string Username { get; set; }
        public string AuthToken { get; set; }
        public DateTime Expiry { get; set; }
    }
}
