using Simplistant_API.Data.Users;

namespace Simplistant_API.Extensions
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Not to be used outside of [Authorize] methods.
        /// </summary>
        public static string GetCurrentUser(this HttpContext httpContext)
        {
            return GetIdentity(httpContext).Username;
        }

        /// <summary>
        /// Not to be used outside of [Authorize] methods.
        /// </summary>
        public static string GetUserAuthToken(this HttpContext httpContext)
        {
            return GetIdentity(httpContext).AuthToken;
        }

        private static UserIdentity GetIdentity(this HttpContext httpContext)
        {
            if (!httpContext.Request.Cookies.ContainsKey("USER_IDENTITY"))
            {
                return new UserIdentity
                {
                    Username = "Guest"
                };
            }

            var serialized = httpContext.Request.Cookies["USER_IDENTITY"];
            return Newtonsoft.Json.JsonConvert.DeserializeObject<UserIdentity>(serialized);
        }

        public const string USER_IDENTITY_KEY = "USER_IDENTITY";
        public static void SetIdentity(this HttpContext httpContext, AuthData authData)
        {
            if (httpContext.Request.Cookies.ContainsKey(USER_IDENTITY_KEY))
                httpContext.Response.Cookies.Delete(USER_IDENTITY_KEY);

            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(new UserIdentity(authData));
            httpContext.Response.Cookies.Append(USER_IDENTITY_KEY, serialized, new CookieOptions
            {
                Expires = authData.Expiry
            });
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
