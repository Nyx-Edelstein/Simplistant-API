using LiteDB;
using Newtonsoft.Json;

namespace Simplistant_API.Models.Data
{
    [HasStringKey("Key", isUnique: true)]
    public class IndexData : DataItem
    {
        public string Key { get; set; }
        public MatchDataCollection Data { get; set; }
    }

    public class MatchDataCollection
    {
        public Dictionary<ObjectId, MatchData> Matches { get; set; }

        public MatchDataCollection() { }

        public MatchDataCollection(BsonValue value)
        {
            Matches = JsonConvert.DeserializeObject<Dictionary<ObjectId, MatchData>>(value.AsString);
        }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(Matches);
        }

        public void Combine(Dictionary<ObjectId, MatchData> matches)
        {
            foreach (var (noteId, matchData) in matches)
            {
                if (Matches.ContainsKey(noteId))
                {
                    var match = Matches[noteId];
                    match.TitleFullMatches += matchData.TitleFullMatches;
                    match.TitlePartialMatches += matchData.TitlePartialMatches;
                    match.TagFullMatches += matchData.TagFullMatches;
                    match.TagPartialMatches += matchData.TagPartialMatches;
                    match.TextFullMatches += matchData.TextFullMatches;
                    match.TextPartialMatches += matchData.TextPartialMatches;
                }
                else
                {
                    Matches.Add(noteId, matchData);
                }
            }
        }

        public void Remove(Dictionary<ObjectId, MatchData> matches)
        {
            foreach (var (noteId, matchData) in matches)
            {
                if (Matches.ContainsKey(noteId))
                {
                    var match = Matches[noteId];
                    match.TitleFullMatches = Math.Max(match.TitleFullMatches - matchData.TitleFullMatches, 0);
                    match.TitlePartialMatches = Math.Max(match.TitlePartialMatches - matchData.TitlePartialMatches, 0);
                    match.TagFullMatches = Math.Max(match.TagFullMatches - matchData.TagFullMatches, 0);
                    match.TagPartialMatches = Math.Max(match.TagPartialMatches - matchData.TagPartialMatches, 0);
                    match.TextFullMatches = Math.Max(match.TextFullMatches - matchData.TextFullMatches, 0);
                    match.TextPartialMatches = Math.Max(match.TextPartialMatches - matchData.TextPartialMatches, 0);

                    //If there aren't any more matches, remove the entry
                    if (match.Empty())
                    {
                        Matches.Remove(noteId);
                    }
                }
                else
                {
                    Matches.Add(noteId, matchData);
                }
            }
        }
    }

    public class MatchData
    {
        public int TitleFullMatches { get; set; }
        public int TitlePartialMatches { get; set; }
        public int TagFullMatches { get; set; }
        public int TagPartialMatches { get; set; }
        public int TextFullMatches { get; set; }
        public int TextPartialMatches { get; set; }

        public int Score() => TitleFullMatches * 10
            + TagFullMatches * 5
            + TextFullMatches * 3
            + TitlePartialMatches * 3
            + TagPartialMatches * 2
            + TextPartialMatches * 1;

        public bool Empty() => TitleFullMatches == 0
            && TitlePartialMatches == 0
            && TagFullMatches == 0
            && TagPartialMatches == 0
            && TextFullMatches == 0
            && TextPartialMatches == 0;
    }
}
