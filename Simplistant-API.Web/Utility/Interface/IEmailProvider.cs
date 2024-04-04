namespace Simplistant_API.Utility.Interface
{
    public interface IEmailProvider
    {
        bool SendConfirmationEmail(string username, string email, string confirmationToken);
        bool SendRecoveryEmail(string username, string email, string recoveryToken);
    }
}
