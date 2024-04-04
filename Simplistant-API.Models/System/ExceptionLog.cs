using Simplistant_API.Repository;

namespace Simplistant_API.Data.System
{
    public class ExceptionLog : DataItem
    {
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
