namespace Simplistant_API.DTO
{
    public class MessageResponse
    {
        public ResponseStatus Status { get; set; } = ResponseStatus.Success;
        public List<string> Messages { get; } = new();
    }
}
