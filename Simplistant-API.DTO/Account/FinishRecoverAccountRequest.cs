namespace Simplistant_API.DTO.Account
{
    public class FinishRecoverAccountRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string RecoveryToken { get; set; }
    }
}