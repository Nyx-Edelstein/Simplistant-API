using LiteDB;
using Simplistant_API.Repository;

namespace Simplistant_API.Models.Data
{
    public class IndexEntries : DataItem
    {
        public BsonValue IndexKeyId { get; set; }
        public Guid NotesItemId { get; set; }

        public int TitleFullMatches { get; set; }
        public int TitlePartialMatches { get; set; }
        public int TagFullMatches { get; set; }
        public int TagPartialMatches { get; set; }
        public int TextFullMatches { get; set; }
        public int TextPartialMatches { get; set; }
    }
}
