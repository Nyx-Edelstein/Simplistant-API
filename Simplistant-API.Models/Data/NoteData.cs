using Simplistant_API.Repository;

namespace Simplistant_API.Models.Data
{
    public class NoteData : DataItem
    {
        public Guid ItemId { get; set; }
        public int Version { get; set; }
        public string Title { get; set; }
        public string[] Tags { get; set; }
        public string Markdown { get; set; }
        public DateTime TimeStamp { get; set; }
        public string EditNotes { get; set; }
        public bool Archived { get; set; }
    }
}
