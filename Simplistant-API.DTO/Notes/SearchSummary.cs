namespace Simplistant_API.DTO.Notes
{
    public class SearchSummary
    {
        public string NoteId { get; set; }
        public string Title { get; set; }
        public List<string> Tags { get; set; }
        public int Score { get; set; }
        public bool Archived { get; set; }
    }
}
