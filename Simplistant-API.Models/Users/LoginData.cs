namespace Simplistant_API.Models.Users
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
