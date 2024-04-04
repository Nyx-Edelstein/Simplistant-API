using Simplistant_API.Repository;

namespace Simplistant_API.Data.Users
{
    [HasStringKey("Username", isUnique: true)]
    public class LoginData : DataItem
    {
        public string Username { get; set; }
        public int LoginType { get; set; }
        public string HashedSecret { get; set; }
    }

    public enum LoginType
    {
        UsernamePassword = 0,
        OAuth = 1
    }
}
