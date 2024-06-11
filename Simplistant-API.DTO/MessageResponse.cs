namespace Simplistant_API.DTO
{
    public class MessageResponse
    {
        public ResponseStatus status { get; set; } = ResponseStatus.Success;
        public List<string> messages { get; } = new();
    }
}
