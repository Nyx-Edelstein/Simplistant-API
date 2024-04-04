using Simplistant_API.Repository;

namespace Simplistant_API.Data.Users
{
    [HasStringKey("Username", isUnique: false)]
    public class EmailData : DataItem
    {
        public string Username { get; set; }
        public string RecoveryEmail { get; set; }
        public string? ConfirmationToken { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
