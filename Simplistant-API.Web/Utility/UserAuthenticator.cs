using Simplistant_API.Data.Users;
using Simplistant_API.Utility.Interface;
using System.Security.Claims;
using Simplistant_API.Extensions;
using Simplistant_API.Repository;

using static BCrypt.Net.BCrypt;

namespace Simplistant_API.Utility
{
    public class UserAuthenticator : IUserAuthenticator
    {
        private IRepository<AuthData> _authDataRepository { get; }

        public UserAuthenticator(IRepository<AuthData> authDataRepository)
        {
            _authDataRepository = authDataRepository;
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

            //Set cookie
            context.SetIdentity(authData);
        }

        public async Task Authenticate(HttpContext context, Func<Task> next)
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
                context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, username),
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
            
            await next();
        }
    }
}
