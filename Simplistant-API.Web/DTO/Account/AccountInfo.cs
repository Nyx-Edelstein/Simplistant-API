using Simplistant_API.Models.Users;

namespace Simplistant_API.DTO.Account
{
    public class AccountInfo
    {
        public string Username { get; set; }
        public bool IsOAuthAccount { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
