using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Simplistant_API.Domain.Extensions;
using Simplistant_API.Models.Users;
using Simplistant_API.Models.Repository;
using LiteDB;

using static BCrypt.Net.BCrypt;


namespace Simplistant_API.Domain.Auth
{
    public class UserAuthenticator : IUserAuthenticator
    {
        private IRepository<AuthData> _authDataRepository { get; }
        private IRepository<LoginData> _loginDataRepository { get; }

        public UserAuthenticator(IRepository<AuthData> authDataRepository, IRepository<LoginData> loginDataRepository)
        {
            _authDataRepository = authDataRepository;
            _loginDataRepository = loginDataRepository;
        }

        public void GenerateSession(HttpContext context, string username)
        {
            //Generate auth data and store securely
            var authData = new AuthData
            {
                Username = username,
                AuthToken = GenerateSalt(),
                Expiry = DateTime.UtcNow.AddDays(14)
            };
            var secureAuthData = new AuthData
            {
                Username = authData.Username,
                AuthToken = HashPassword(authData.AuthToken),
                Expiry = authData.Expiry,
            };
            _authDataRepository.Upsert(secureAuthData);

            //Set auth cookie
            context.SetIdentity(authData);
        }

        public bool Authenticate(HttpContext context)
        {
            var username = context.GetCurrentUser();
            var authToken = context.GetUserAuthToken();

            //Remove expired auth data
            _authDataRepository.RemoveWhere(x => x.Expiry < DateTime.UtcNow);

            //Get existing data
            var existingAuthData = _authDataRepository.GetWhere(x => x.Username == username);

            //Find if any are valid
            var authData = existingAuthData.FirstOrDefault(x => Verify(authToken, x.AuthToken));
            if (authData != null)
            {
                //User is authenticated
                //Get userId (for resolving Data repositories)
                var userId = _loginDataRepository.GetWhere(x => x.Username == username)
                    .FirstOrDefault()?.Id ?? ObjectId.Empty;

                //Set claims
                context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                }, "Authenticated"));

                //Determine if a session needs to be generated
                //This prevents the user from having to login again if they use the service regularly
                if (authData.Expiry < DateTime.UtcNow.AddDays(7))
                {
                    GenerateSession(context, username);
                }
            }
            else
            {
                context.User = new ClaimsPrincipal(Array.Empty<ClaimsIdentity>());
            }

            return context.User.Identity?.IsAuthenticated == true;
        }
    }
}
