using System.Net;
using Simplistant_API.Data.System;
using Simplistant_API.Repository;
using Simplistant_API.Utility.Interface;
using System.Net.Mail;
using System.Text;
using Simplistant_API.Extensions;

namespace Simplistant_API.Utility
{
    public class EmailProvider : IEmailProvider
    {
        private IRepository<ConfigItem> _configItemRepository { get; }
        private IRepository<ExceptionLog> _exceptionLogRepository { get; }

        public EmailProvider
        (
            IRepository<ConfigItem> configItemRepository,
            IRepository<ExceptionLog> exceptionLogRepository
        )
        {
            _configItemRepository = configItemRepository;
            _exceptionLogRepository = exceptionLogRepository;
        }

        public bool SendConfirmationEmail(string username, string email, string confirmationToken)
            => SendEmail(username, email, confirmationToken, ConfirmationTemplate);

        public bool SendRecoveryEmail(string username, string email, string recoveryToken)
            => SendEmail(username, email, recoveryToken, RecoveryTemplate);

        private bool SendEmail(string username, string email, string token, Func<string, string, string> template)
        {
            var credentials = GetCredentials();
            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = credentials,
                Timeout = 10000
            };
            using var message = new MailMessage(credentials.UserName, email)
            {
                Subject = $"Simplistant - Password Recovery For {username}",
                Body = template(username, token),
                IsBodyHtml = true
            };
            try
            {
                client.Send(message);
                return true;
            }
            catch (SmtpException ex)
            {
                var detailedMessage = ex.DetailedExceptionMessage();
                var logItem = new ExceptionLog
                {
                    Message = $"Failure to send email for user: {username}; email: {email}.\r\n\r\n-----{detailedMessage}",
                    ExceptionType = ex.GetType().FullName ?? "Unknown",
                    TimeStamp = DateTime.UtcNow
                };
                _exceptionLogRepository.Upsert(logItem);
                return false;
            }
        }

        private NetworkCredential GetCredentials()
        {
            var email = _configItemRepository.GetWhere(x => x.Key == "AdminEmailAddress").FirstOrDefault()?.Value;
            var password = _configItemRepository.GetWhere(x => x.Key == "AdminEmailPassword").FirstOrDefault()?.Value;
            return new NetworkCredential(email, password);
        }

        private static string ConfirmationTemplate(string username, string confirmationToken) => $@"
<!DOCTYPE html>
<html>
<body>
<h3>Hello, {username}!</h3>
To confirm this email address, copy and paste the following token to the email confirmation form:<br><br>
<strong>{confirmationToken}</strong><br><br>
</body>
</html>";

        private static string RecoveryTemplate(string username, string recoveryToken) => $@"
<!DOCTYPE html>
<html>
<body>
<h3>Hello, {username}!</h3>
To recover your account, copy and paste the following recovery token to the password recovery form:<br><br>
<strong>{recoveryToken}</strong><br><br>
<font size=""2"">(If you did not request this email, please ignore and delete this message, and notify the site administrator.)</font>
</body>
</html>";
    }
}
