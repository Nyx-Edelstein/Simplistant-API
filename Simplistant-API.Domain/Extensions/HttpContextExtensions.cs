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
        /// Not to be used outside of [Authorize] methods.
        /// </summary>
        public static string GetCurrentUser(this HttpContext httpContext)
        {
            return httpContext.GetIdentity().Username;
        }

        /// <summary>
        /// Not to be used outside of [Authorize] methods.
        /// </summary>
        public static string GetUserAuthToken(this HttpContext httpContext)
        {
            return httpContext.GetIdentity().AuthToken;
        }

        /// <summary>
        /// Not to be used outside of [Authorize] methods.
        /// </summary>
        public static ObjectId GetCurrentUserId(this HttpContext httpContext)
        {
            var id = httpContext.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).FirstOrDefault()?.Value ?? "";
            return new ObjectId(id);
        }


        /// <summary>
        /// Not to be used for any purpose except as a hint to the OAuth api.
        /// </summary>
        public static string GetOAuthEmail(this HttpContext httpContext)
        {
            return httpContext.Request.Cookies[OAUTH_EMAIL_KEY] ?? "";
        }


        private static UserIdentity GetIdentity(this HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.ContainsKey(USER_IDENTITY_KEY))
            {
                return new UserIdentity
                {
                    Username = "Guest",
                };
            }

            var serialized = httpContext.Request.Cookies[USER_IDENTITY_KEY];
            return Newtonsoft.Json.JsonConvert.DeserializeObject<UserIdentity>(serialized);
        }

        public static void SetIdentity(this HttpContext httpContext, AuthData authData)
        {
            if (httpContext.Request.Cookies.ContainsKey(USER_IDENTITY_KEY))
                httpContext.Response.Cookies.Delete(USER_IDENTITY_KEY);

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(new UserIdentity(authData));
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

        //We need to not store the BSON Id from the dataitem object.
        public class UserIdentity
        {
            public string Username { get; set; }
            public string AuthToken { get; set; }
            public DateTime Expiry { get; set; }

            internal UserIdentity() { }

            internal UserIdentity(AuthData authData)
            {
                Username = authData.Username;
                AuthToken = authData.AuthToken;
                Expiry = authData.Expiry;
            }
        }
    }
}
