namespace Simplistant_API.Models.Data
{
    [HasGuidKey("HistoryId", isUnique: false)]
    public class NoteData : DataItem
    {
        public Guid HistoryId { get; set; }
        public int Version { get; set; }
        public string Title { get; set; }
        public string[] Tags { get; set; }
        public string Markdown { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Changes { get; set; }
        public bool Archived { get; set; }
    }
}
