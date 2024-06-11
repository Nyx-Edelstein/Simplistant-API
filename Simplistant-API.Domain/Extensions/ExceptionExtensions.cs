namespace Simplistant_API.Domain.Extensions
{
    public static class ExceptionExtensions
    {
        public static string DetailedExceptionMessage(this Exception? exception)
        {
            var s = "";
            while (exception != null)
            {
                s += exception.Message;
                s += $"\r\nStack Trace:\r\n{exception.StackTrace}\r\n----------\r\n";
                exception = exception.InnerException;
            }
            return s;
        }
    }
}
