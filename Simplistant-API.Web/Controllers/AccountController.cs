using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Newtonsoft.Json;
using Simplistant_API.Domain.Attributes;
using Simplistant_API.Domain.Auth;
using Simplistant_API.Domain.Extensions;
using Simplistant_API.DTO;
using Simplistant_API.DTO.Account;
using Simplistant_API.Models.Repository;
using Simplistant_API.Models.System;
using Simplistant_API.Models.Users;
using Simplistant_API.Utility.Interface;

using static BCrypt.Net.BCrypt;

//Todo: cleanup/refactor

namespace Simplistant_API.Controllers
{
    [ApiController]
    [Route("/[controller]/[action]")]
    public class AccountController : ControllerBase
    {
        private IRepository<ConfigItem> _configItemRepository { get; }
        private IRepository<LoginData> _loginDataRepository { get; }
        private IRepository<AuthData> _authDataRepository { get; }
        private IRepository<EmailData> _emailDataRepository { get; }
        private IRepository<RecoveryData> _recoveryDataRepository { get; }

        private IEmailProvider _emailProvider { get; }
        private IUserAuthenticator _userAuthenticator { get; }

        public AccountController
        (
            IRepository<ConfigItem> configItemRepository,
            IRepository<LoginData> loginDataRepository,
            IRepository<AuthData> authDataRepository,
            IRepository<EmailData> emailDataRepository,
            IRepository<RecoveryData> recoveryDataRepository,
            IEmailProvider emailProvider,
            IUserAuthenticator userAuthenticator
        )
        {
            _configItemRepository = configItemRepository;
            _loginDataRepository = loginDataRepository;
            _authDataRepository = authDataRepository;
            _emailDataRepository = emailDataRepository;
            _recoveryDataRepository = recoveryDataRepository;
            _emailProvider = emailProvider;
            _userAuthenticator = userAuthenticator;
        }

        /// <summary>
        /// Register with username and password.
        /// Generates a login session upon success.
        /// </summary>
        [HttpPost]
        public MessageResponse Register(RegisterRequest request)
        {
            var response = new MessageResponse();

            //Validate request data
            if (string.IsNullOrWhiteSpace(request.Username))
                response.messages.Add("Username is required.");
            else if (request.Username.Length < 2)
            {
                response.messages.Add("Username must be at least 2 characters.");
            }
            else if (!request.Username.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
                response.messages.Add("Username must contain only alphanumeric characters and underscores.");
            else if (!char.IsAsciiLetter(request.Username[0]))
                response.messages.Add("Username must begin with a letter.");

            if (string.IsNullOrWhiteSpace(request.Password))
                response.messages.Add("Password is required.");

            if (!request.WaiveEmailRecovery)
            {
                var emailValid = new EmailAddressAttribute().IsValid(request.Email);
                if (string.IsNullOrWhiteSpace(request.Email))
                    response.messages.Add("No recovery email is specified and the account cannot be recovered if the password is lost. Please confirm this is intentional.");
                else if (!emailValid)
                    response.messages.Add("Provided email address is not a valid format.");
            }

            if (response.messages.Any())
            {
                response.status = ResponseStatus.Error;
                return response;
            }

            //Ensure strong password
            var passwordValidationError = ValidateStrongPassword(request.Password);
            if (passwordValidationError != null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add(passwordValidationError);
                return response;
            }

            //Ensure username is unique
            request.Username = request.Username.ToLowerInvariant();
            var existingAccount = _loginDataRepository.GetWhere(x => x.Username == request.Username).FirstOrDefault();
            if (existingAccount != null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add($"Username '{request.Username}' is taken.");
                return response;
            }

            //Ensure email isn't being used for an OAuth account
            var existingOAuthAccount = _loginDataRepository.GetWhere(x => x.Username == request.Email).FirstOrDefault();
            if (existingOAuthAccount != null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add($"Email address '{request.Email}' is in use by an OAuth account. Try signing in with Google instead.");
                return response;
            }

            //Generate and store login data
            var loginData = new LoginData
            {
                Username = request.Username,
                LoginType = (int)LoginType.UsernamePassword,
                HashedSecret = HashPassword(request.Password)
            };
            _loginDataRepository.Upsert(loginData);

            //Success; generate auth data and store securely
            _userAuthenticator.GenerateSession(HttpContext, request.Username);
            if (string.IsNullOrWhiteSpace(request.Email)) return response;

            //If email is provided, send a confirmation email and store data securely
            var confirmationToken = GenerateSalt();
            var success = _emailProvider.SendConfirmationEmail(request.Username, request.Email, confirmationToken);
            if (success)
            {
                response.messages.Add($"Follow instructions sent to '{request.Email}' in order to enable recovery of your account. You may need to check your spam folder.");
            }
            else
            {
                response.status = ResponseStatus.Warning;
                response.messages.Add("There was a problem sending the confirmation email to the specified address. Please retry later.");
            }
            var emailData = new EmailData
            {
                Username = request.Username,
                RecoveryEmail = request.Email,
                ConfirmationToken = HashPassword(confirmationToken),
                EmailConfirmed = false
            };
            _emailDataRepository.Upsert(emailData);
            return response;
        }

        /// <summary>
        /// Login with username and password.
        /// Generates a login session upon success.
        /// </summary>
        [HttpPost]
        public MessageResponse Login(LoginRequest request)
        {
            var response = new MessageResponse();

            //Lookup record and verify password
            var loginData = _loginDataRepository.GetWhere(x => x.Username == request.Username).FirstOrDefault();
            if (loginData == null || !Verify(request.Password, loginData.HashedSecret))
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Username or password is invalid.");
                return response;
            }

            //Success
            _userAuthenticator.GenerateSession(HttpContext, request.Username);
            return response;
        }

        /// <summary>
        /// Begin OAuth login; returuns redirect URL to Google OAuth2 API endpoint.
        /// </summary>
        [HttpGet]
        [GeneratorIgnore]
        public ActionResult LoginOAuth()
        {
            //Redirect to Google OAuth2 API endpoint
            var client_id = _configItemRepository.GetWhere(x => x.Key == "Google_OAuth_ClientID").FirstOrDefault()?.Value;
            var redirect = WebUtility.UrlEncode($"{Request.Scheme}://{Request.Host}{Url.Action("OAuth")}");
            var login_hint = HttpContext.GetOAuthEmail();
            var oauth_url = $"https://accounts.google.com/o/oauth2/v2/auth?&client_id={client_id}&redirect_uri={redirect}&response_type=code&access_type=online&scope=email&prompt=consent&login_hint={login_hint}";

            return new RedirectResult(oauth_url, false);
        }

        /// <summary>
        /// Callback method from Google OAuth2 API endpoint.
        /// Generates a login session upon success.
        /// </summary>
        [HttpGet]
        [GeneratorIgnore]
        public ActionResult OAuth(string code)
        {
            var response = new MessageResponse();

            //Use the code given to request an access token
            const string url = $"https://oauth2.googleapis.com/token";
            var client_id = _configItemRepository.GetWhere(x => x.Key == "Google_OAuth_ClientID").FirstOrDefault()?.Value;
            var client_secret = _configItemRepository.GetWhere(x => x.Key == "Google_OAuth_ClientSecret").FirstOrDefault()?.Value;
            var redirect = $"{Request.Scheme}://{Request.Host}{Url.Action("OAuth")}";
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            using var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", client_id),
                new KeyValuePair<string, string>("client_secret", client_secret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirect),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
            });
            content.Headers.Clear();
            content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            var json = client.PostAsync(url, content).Result.Content.ReadAsStringAsync().Result;

            //Validate the response
            if (string.IsNullOrWhiteSpace(json) || !json.Contains("id_token"))
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Unexpected response from OAuth server. Please contact the site administrator.");
            }

            //Parse out the id_token; this is a jwt
            var json_obj = JsonDocument.Parse(json);
            var id_token = json_obj.RootElement.GetProperty("id_token").ToString();
            if (string.IsNullOrWhiteSpace(id_token))
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Id token could not be parsed. Please contact the site administrator.");
            }

            //Parse the email field from the jwt
            var handler = new JsonWebTokenHandler();
            var jwt = handler.ReadJsonWebToken(id_token);
            var email = jwt.GetClaim("email").Value;

            //Lookup existing user by recovery email
            //(They may already have an account with this email address)
            var emailData = _emailDataRepository.GetWhere(x => x.RecoveryEmail == email).FirstOrDefault();
            if (emailData == null)
            {
                //Create new login data if it doesn't already exist
                var loginData = _loginDataRepository.GetWhere(x => x.Username == email).FirstOrDefault();
                if (loginData == null)
                {
                    loginData = new LoginData
                    {
                        Username = email,
                        LoginType = (int)LoginType.OAuth,
                    };
                    _loginDataRepository.Upsert(loginData);
                }

                //Success
                _userAuthenticator.GenerateSession(HttpContext, loginData.Username);
            }
            else
            {
                //Proceed with login as if they used username/password
                _userAuthenticator.GenerateSession(HttpContext, emailData.Username);
            }

            return response.status == ResponseStatus.Success
                ? new RedirectResult("https://simplistant.azurewebsites.net", false)
                : Content($"{JsonConvert.SerializeObject(response)}", "application/json");
        }

        /// <summary>
        /// Begin password recovery using email address.
        /// </summary>
        [HttpPost]
        public MessageResponse BeginRecoverAccount(BeginRecoverAccountRequest request)
        {
            //Lookup record by username or email
            var emailData = _emailDataRepository.GetWhere(x => x.Username == request.UserData).FirstOrDefault()
                ?? _emailDataRepository.GetWhere(x => x.RecoveryEmail == request.UserData).FirstOrDefault();
            if (emailData != null)
            {
                //Generate and store recovery data securely
                var recoveryToken = GenerateSalt();
                var secureRecoveryData = new RecoveryData
                {
                    Username = emailData.Username,
                    RecoveryToken = HashPassword(recoveryToken),
                    Expiry = DateTime.UtcNow.AddHours(1)
                };
                _recoveryDataRepository.Upsert(secureRecoveryData);

                //Send recovery token to email
                _emailProvider.SendRecoveryEmail(emailData.Username, emailData.RecoveryEmail, recoveryToken);
            }

            //"Success" in any case -- do not reveal data unnecessarily
            var response = new MessageResponse();
            response.messages.Add("If the provided information is on record, a recovery email has been sent. You may need to check your spam folder.");
            return response;
        }

        /// <summary>
        /// Complete password recovery using email address.
        /// Generates a login session upon success.
        /// </summary>
        [HttpPost]
        public MessageResponse FinishRecoverAccount(FinishRecoverAccountRequest request)
        {
            var response = new MessageResponse();

            //Lookup login record
            var loginData = _loginDataRepository.GetWhere(x => x.Username == request.Username).FirstOrDefault();
            if (loginData == null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Username or recovery code is invalid.");
                return response;
            }
            if (loginData.LoginType != (int)LoginType.UsernamePassword)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("This is an OAuth account. Use OAuth login instead.");
                return response;
            }

            //Lookup recovery record
            var recoveryData = _recoveryDataRepository.GetWhere(x => x.Username == request.Username).FirstOrDefault();
            if (recoveryData == null || !Verify(request.RecoveryToken, recoveryData.RecoveryToken))
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Username or recovery code is invalid.");
                return response;
            }

            //Validate password
            var passwordValidationError = ValidateStrongPassword(request.Password);
            if (passwordValidationError != null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add(passwordValidationError);
                return response;
            }

            //Change password
            loginData.HashedSecret = HashPassword(request.Password);
            _loginDataRepository.Upsert(loginData);

            //Cleanup old records
            _recoveryDataRepository.RemoveWhere(x => x.Username == request.Username);
            _authDataRepository.RemoveWhere(x => x.Username == request.Username);

            //Success
            _userAuthenticator.GenerateSession(HttpContext, request.Username);
            response.messages.Add("Password has been updated.");
            return response;
        }

        /// <summary>
        /// Confirm an email address using a token supplied during the registration process.
        /// Requires active session.
        /// </summary>
        [HttpPost]
        [Authorize]
        public MessageResponse ConfirmEmail(ConfirmEmailRequest request)
        {
            var response = new MessageResponse();
            var username = HttpContext.GetCurrentUser();

            //Lookup record
            var emailData = _emailDataRepository.GetWhere(x => x.Username == username).FirstOrDefault();
            if (emailData == null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("No recovery email on record.");
                return response;
            }
            
            //Validate confirmation token
            var tokenIsValid = Verify(request.ConfirmationToken, emailData.ConfirmationToken);
            if (!tokenIsValid)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("The confirmation code could not be verified. Try resending the confirmation email.");
                return response;
            }

            //Success; set email confirmed flag
            emailData.ConfirmationToken = null;
            emailData.EmailConfirmed = true;
            _emailDataRepository.Upsert(emailData);
            response.messages.Add("Recovery email has been confirmed.");
            return response;
        }

        /// <summary>
        /// Resend the confirmation email sent during the registration process.
        /// </summary>
        [HttpGet]
        [Authorize]
        public MessageResponse ResendConfirmationEmail()
        {
            var response = new MessageResponse();
            var username = HttpContext.GetCurrentUser();

            //Lookup record
            var emailData = _emailDataRepository.GetWhere(x => x.Username == username).FirstOrDefault();
            if (emailData == null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("No recovery email is on record for this account.");
                return response;
            }
            if (emailData.EmailConfirmed)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("This email address has already been confirmed.");
                return response;
            }

            //Generate a new confirmation token and store new hashed token on the database
            var confirmationToken = GenerateSalt();
            emailData.ConfirmationToken = HashPassword(confirmationToken);
            _emailDataRepository.Upsert(emailData);

            //Resend recovery email
            var success = _emailProvider.SendConfirmationEmail(username, emailData.RecoveryEmail, confirmationToken);
            if (!success)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("There was a problem sending the confirmation email. Try again later.");
                return response;
            }

            //Success
            response.messages.Add("A confirmation email has been sent. You may need to check your spam folder.");
            return response;
        }

        /// <summary>
        /// Clears all login sessions associated with the account.
        /// Requires an active session.
        /// </summary>
        [HttpGet]
        [Authorize]
        public MessageResponse Logout()
        {
            var username = HttpContext.GetCurrentUser();
            _authDataRepository.RemoveWhere(x => x.Username == username);
            HttpContext.Response.Cookies.Delete("USER_IDENTITY");
            return new MessageResponse();
        }

        /// <summary>
        /// Change the password for the account.
        /// The account cannot be an OAuth account.
        /// Generates an active session upon success.
        /// </summary>
        [HttpPost]
        [Authorize]
        public MessageResponse ChangePassword(ChangePasswordRequest request)
        {
            var response = new MessageResponse();
            var username = HttpContext.GetCurrentUser();

            //Lookup login record
            var loginData = _loginDataRepository.GetWhere(x => x.Username == username).FirstOrDefault();
            if (loginData == null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("User account error. Contact site admin if the problem persists.");
                return response;
            }
            if (loginData.LoginType != (int)LoginType.UsernamePassword)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("This is an OAuth account. Use OAuth login instead.");
                return response;
            }

            //Verify old password
            if (!Verify(request.OldPassword, loginData.HashedSecret))
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Password is invalid.");
                return response;
            }

            //Validate new password
            var passwordValidationError = ValidateStrongPassword(request.NewPassword);
            if (passwordValidationError != null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add(passwordValidationError);
                return response;
            }

            //Change password on database
            loginData.HashedSecret = HashPassword(request.NewPassword);
            _loginDataRepository.Upsert(loginData);

            //Clear existing auth records
            _authDataRepository.RemoveWhere(x => x.Username == username);

            //Success; generate and store new auth data
            _userAuthenticator.GenerateSession(HttpContext, username);
            response.messages.Add("Password updated successfully.");
            return response;
        }

        /// <summary>
        /// Change the recovery email address for the account and send a validation email.
        /// Requires an active session.
        /// </summary>
        [HttpPost]
        [Authorize]
        public MessageResponse ChangeEmail(ChangeEmailRequest request)
        {
            var response = new MessageResponse();
            var username = HttpContext.GetCurrentUser();

            //Lookup login record
            var loginData = _loginDataRepository.GetWhere(x => x.Username == username).FirstOrDefault();
            if (loginData == null)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("User account error. Contact site administrator if the problem persists.");
                return response;
            }
            if (loginData.LoginType != (int)LoginType.UsernamePassword)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("This is an OAuth account. Use OAuth login instead.");
                return response;
            }

            //Validate password
            var passwordValid = Verify(request.Password, loginData.HashedSecret);
            if (!passwordValid)
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Password is invalid.");
                return response;
            }

            //Validate email
            if (!new EmailAddressAttribute().IsValid(request.NewEmail))
            {
                response.status = ResponseStatus.Error;
                response.messages.Add("Provided email address is not a correct format.");
                return response;
            }

            //Lookup email record
            var emailData = _emailDataRepository.GetWhere(x => x.Username == username).FirstOrDefault();
            if (emailData == null)
            {
                //Create a new record
                emailData = new EmailData
                {
                    Username = username,
                    RecoveryEmail = request.NewEmail
                };
            }
            else
            {
                //Update old record
                emailData.RecoveryEmail = request.NewEmail;
            }

            //Generate confirmation token and store updated record
            var confirmationToken = GenerateSalt();
            emailData.ConfirmationToken = HashPassword(confirmationToken);
            emailData.EmailConfirmed = false;
            _emailDataRepository.Upsert(emailData);

            //Clear old recovery records
            _recoveryDataRepository.RemoveWhere(x => x.Username == username);

            //Send confirmation email
            var success = _emailProvider.SendConfirmationEmail(username, request.NewEmail, confirmationToken);
            if (success)
            {
                response.messages.Add($"Follow instructions sent to '{request.NewEmail}' to confirm the new address. You may need to check your spam folder.");
            }
            else
            {
                response.status = ResponseStatus.Warning;
                response.messages.Add("There was a problem sending the confirmation email to the specified address. Please retry later.");
            }

            //Success
            return response;
        }

        /// <summary>
        /// Check to see if the current user is logged in.
        /// </summary>
        [HttpGet]
        public bool LoggedIn()
        {
            return HttpContext.User.Identity?.IsAuthenticated == true;
        }

        /// <summary>
        /// Retrieves user account information.
        /// Requires an active session.
        /// </summary>
        [HttpGet]
        [Authorize]
        public AccountInfo GetAccountInfo()
        {
            var username = HttpContext.GetCurrentUser();
            
            var loginData = _loginDataRepository.GetWhere(x => x.Username == username).FirstOrDefault();
            if (loginData == null)
            {
                //This shouldn't happen since the user is authenticated, but need to do the check anyway for type safety / best practices
                throw new Exception($"Couldn't find the user '{username}'. Something's probably borked.");
            }

            var accountInfo = new AccountInfo
            {
                Username = username,
                IsOAuthAccount = loginData.LoginType == (int)LoginType.OAuth
            };

            var emailData = _emailDataRepository.GetWhere(x => x.Username == username).FirstOrDefault();
            if (emailData != null)
            {
                accountInfo.Email = emailData.RecoveryEmail;
                accountInfo.EmailConfirmed = emailData.EmailConfirmed;
            }
            else
            {
                accountInfo.Email = "";
                accountInfo.EmailConfirmed = false;
            }

            return accountInfo;
        }

        /// <summary>
        /// Check to see if the current user is logged in.
        /// </summary>
        [HttpGet]
        public string GetCurrentUser()
        {
            return HttpContext.GetCurrentUser();
        }

        /// <summary>
        /// Clear database data for testing purposes.
        /// </summary>
        [HttpGet]
        public bool ClearData()
        {
            _loginDataRepository.RemoveWhere(x => true);
            _emailDataRepository.RemoveWhere(x => true);
            _authDataRepository.RemoveWhere(x => true);
            _recoveryDataRepository.RemoveWhere(x => true);
            Repositories.System<ExceptionLog>().RemoveWhere(x => true);
            return true;
        }

        private static string? ValidateStrongPassword(string password)
        {
            var passwordStrength = 0.0;
            foreach (var c in password)
            {
                if (char.IsDigit(c)) passwordStrength += 3.322;
                else if (char.IsLower(c)) passwordStrength += 4.7;
                else if (char.IsUpper(c)) passwordStrength += 4.7;
                else passwordStrength += 5.04;
            }
            return passwordStrength < 28 ? $"Minimum password strength: {Math.Round(passwordStrength, 1)}/28.0 bits of entropy." : null;
        }
    }
}
