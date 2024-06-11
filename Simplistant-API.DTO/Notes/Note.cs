namespace Simplistant_API.DTO.Notes
{
    public class Note
    {
        public string NoteId { get; set; }
        public string HistoryId { get; set; }
        public string Title { get; set; }
        public List<string> Tags { get; set; }
        public string Markdown { get; set; }
        public bool Archived { get; set; }
    }
}
