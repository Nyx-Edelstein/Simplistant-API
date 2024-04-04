namespace Simplistant_API.Utility.Interface
{
    public interface IUserAuthenticator
    {
        /// <summary>
        /// Stores authentication data on the database and sets session cookie accordingly.
        /// </summary>
        void GenerateSession(HttpContext context, string username);

        /// <summary>
        /// Authenticates a session cookie and sets user claims for authorization
        /// </summary>
        Task Authenticate(HttpContext httpContext, Func<Task> next);
    }
}
