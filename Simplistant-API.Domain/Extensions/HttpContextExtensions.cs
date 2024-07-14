using LiteDB;
using Microsoft.AspNetCore.Http;
using Simplistant_API.Models.Users;
using System.Security.Claims;

namespace Simplistant_API.Domain.Extensions
{
    public static class HttpContextExtensions
    {
        private const string USER_IDENTITY_KEY = "USER_IDENTITY";
        private const string OAUTH_EMAIL_KEY = "OAUTH_EMAIL";

        /// <summary>
        /// Not to be used outside [Authorize] methods.
        /// </summary>
        public static string GetCurrentUser(this HttpContext httpContext)
        {
            return httpContext.GetIdentity().Username;
        }

        /// <summary>
        /// Not to be used outside [Authorize] methods.
        /// </summary>
        public static AuthData GetUserAuthData(this HttpContext httpContext)
        {
            return httpContext.GetIdentity();
        }

        /// <summary>
        /// Not to be used outside [Authorize] methods.
        /// </summary>
        public static ObjectId GetCurrentUserId(this HttpContext httpContext)
        {
            var id = httpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value ?? "";
            return new ObjectId(id);
        }


        /// <summary>
        /// Not to be used for any purpose except as a hint to the OAuth api.
        /// </summary>
        public static string GetOAuthEmail(this HttpContext httpContext)
        {
            return httpContext.Request.Cookies[OAUTH_EMAIL_KEY] ?? "";
        }


        private static AuthData GetIdentity(this HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.ContainsKey(USER_IDENTITY_KEY))
            {
                return new AuthData
                {
                    Username = "Guest",
                };
            }

            var serialized = httpContext.Request.Cookies[USER_IDENTITY_KEY];
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<UserIdentity>(serialized).ToAuthData();
            }
            catch
            {
                httpContext.Response.Cookies.Delete(USER_IDENTITY_KEY);
                return new AuthData
                {
                    Username = "Guest",
                };
            }
            
        }

        public static void SetIdentity(this HttpContext httpContext, AuthData authData)
        {
            if (httpContext.Request.Cookies.ContainsKey(USER_IDENTITY_KEY))
                httpContext.Response.Cookies.Delete(USER_IDENTITY_KEY);

            var userIdentity = new UserIdentity(authData);
            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(userIdentity);
            httpContext.Response.Cookies.Append(USER_IDENTITY_KEY, serialized, new CookieOptions
            {
                Expires = authData.Expiry
            });

            //We've already validated the username so if it contains an "@" symbol it's an email address
            //i.e. - OAuth account
            //Persist this value to cookies so that we can use it as the hint email in the OAuth API
            var isOAuth = authData.Username.Contains("@");
            if (isOAuth)
            {
                httpContext.Response.Cookies.Append(OAUTH_EMAIL_KEY, authData.Username, new CookieOptions
                {
                    Expires = DateTime.MaxValue
                });
            }
        }


        //We need to store the BSON Id as a string as a workaround for JsonConvert not handling it properly.
        public class UserIdentity
        {
            public string Id { get; set; }
            public string Username { get; set; }
            public string AuthToken { get; set; }
            public DateTime Expiry { get; set; }

            internal UserIdentity() { }

            internal UserIdentity(AuthData authData)
            {
                Id = authData.Id.ToString();
                Username = authData.Username;
                AuthToken = authData.AuthToken;
                Expiry = authData.Expiry;
            }

            public AuthData ToAuthData()
            {
                return new AuthData
                {
                    Id = new ObjectId(Id),
                    Username = Username,
                    AuthToken = AuthToken,
                    Expiry = Expiry
                };
            }
        }
    }
}
