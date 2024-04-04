namespace Simplistant_API.DTO.Account
{
    public class RegisterUsernamePasswordRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string? Email { get; set; }
        public bool? WaiveEmailRecovery { get; set; }
    }
}