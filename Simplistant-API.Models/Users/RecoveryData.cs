using Simplistant_API.Repository;

namespace Simplistant_API.Models.Users
{
    [HasStringKey("Username", isUnique: false)]
    public class RecoveryData : DataItem
    {
        public string Username { get; set; }
        public string RecoveryToken { get; set; }
        public DateTime Expiry { get; set; }
    }
}
