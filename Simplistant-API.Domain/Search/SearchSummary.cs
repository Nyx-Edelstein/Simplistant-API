using LiteDB;

namespace Simplistant_API.Domain.Search
{
    public class SearchSummary
    {
        public ObjectId NoteId { get; set; }
        public string Title { get; set; }
        public string[] Tags { get; set; }
        public int Score { get; set; }
        public bool Archived { get; set; }
    }
}
