namespace Simplistant_API.Models.System
{
    public class ExceptionLog : DataItem
    {
        public string Message { get; set; }
        public string ExceptionType { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
