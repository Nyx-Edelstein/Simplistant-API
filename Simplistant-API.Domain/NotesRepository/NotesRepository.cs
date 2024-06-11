using LiteDB;
using Simplistant_API.Domain.Markdown;
using Simplistant_API.Domain.Stemming;
using Simplistant_API.DTO.Notes;
using Simplistant_API.Models.Data;
using Simplistant_API.Models.Repository;

namespace Simplistant_API.Domain.NotesRepository
{
    public class NotesRepository : INotesRepository
    {
        private IRepository<IndexData> IndexDataRepository { get; }
        private IRepository<NoteData> NoteDataRepository { get; }
        private IMarkdownTokenizer MarkdownTokenizer { get; }
        private IStemmer Stemmer { get; }

        public NotesRepository
        (
            IRepository<IndexData> indexDataRepository,
            IRepository<NoteData> noteDataRepository,
            IMarkdownTokenizer markdownTokenizer,
            IStemmer stemmer
        )
        {
            IndexDataRepository = indexDataRepository;
            NoteDataRepository = noteDataRepository;
            MarkdownTokenizer = markdownTokenizer;
            Stemmer = stemmer;
        }

        public NoteData? Get(ObjectId noteId)
        {
            return NoteDataRepository.GetWhere(x => x.Id == noteId).FirstOrDefault();
        }

        public void Save(Note noteDTO)
        {
            //Todo: might be an issue with Guid.Parse
            var note = new NoteData
            {
                Id = ObjectId.NewObjectId(),
                HistoryId = Guid.Parse(noteDTO.HistoryId),
                Title = noteDTO.Title,
                Tags = noteDTO.Tags.ToArray(),
                Markdown = noteDTO.Markdown,
                Archived = noteDTO.Archived,
            };

            //Get the most recent version with the same revision history
            var existingNotes = NoteDataRepository.GetWhere(x => x.HistoryId == note.HistoryId);
            if (existingNotes.Count == 0)
            {
                //If there is no most recent, save this version as the first
                note.Version = 0;
                NoteDataRepository.Upsert(note);
                AddToIndex(note);
                return;
            }

            //Otherwise, get the latest version
            //Save this version as old version + 1
            var oldVersion = existingNotes.Max(x => x.Version);
            var oldNote = existingNotes.First(x => x.Version == oldVersion);
            note.Version = oldVersion + 1;

            //Autopopulate change notes if not specified
            if (string.IsNullOrWhiteSpace(note.Changes))
            {
                var changes = new List<string>();
                if (oldNote.Title != note.Title) changes.Add("title");
                if (!oldNote.Tags.SequenceEqual(note.Tags)) changes.Add("tags");
                if (oldNote.Markdown != note.Markdown) changes.Add("content");
                if (changes.Count != 0)
                {
                    note.Changes = $"Changed: {string.Join(", ", changes)}";
                }
            }

            note.TimeStamp = DateTime.UtcNow;

            NoteDataRepository.Upsert(note);

            //Remove the old version from the index and add the most recent version
            RemoveFromIndex(oldNote);
            AddToIndex(note);
        }

        public void Delete(Guid historyId)
        {
            //O(n) in NoteData collection + O(log(k))

            //Get all notes with the same version history
            var item_group = NoteDataRepository.GetWhere(x => x.HistoryId == historyId);
            if (item_group.Any())
            {
                //Get most recent version
                var most_recent_version = item_group.Max(x => x.Version);
                var most_recent = item_group.First(x => x.Version == most_recent_version);
                //Remove most version from the index
                RemoveFromIndex(most_recent);
            }
            //Remove all notes with the same version history from database
            NoteDataRepository.RemoveWhere(x => x.HistoryId == historyId);
        }

        public List<SearchSummary> Catalog() => NoteDataRepository.GetWhere(x => !x.Archived)
            .Select(noteData => new SearchSummary
            {
                NoteId = noteData.Id.ToString(),
                Title = noteData.Title,
                Tags = noteData.Tags.ToList(),
                Score = 1,
            }).OrderBy(x => x.Title)
            .ThenBy(x => x.Tags.FirstOrDefault() ?? "")
            .ToList();

        public List<SearchSummary> Search(string[] searchTokens, bool includeArchived)
        {
            //Get matches for each token
            var matches = new Dictionary<string, SearchSummary>();
            foreach (var token in searchTokens)
            {
                var submatches = Search(token, includeArchived);
                foreach (var submatch in submatches)
                {
                    //If we match on multiple words, simply add the scores
                    if (matches.ContainsKey(submatch.NoteId))
                    {
                        matches[submatch.NoteId].Score += submatch.Score;
                    }
                    //Otherwise just record the match
                    else
                    {
                        matches.Add(submatch.NoteId, submatch);
                    }
                }
            }
            return matches.Values.OrderByDescending(x => x.Score).ToList();
        }

        private List<SearchSummary> Search(string token, bool includeArchived)
        {
            var hit = IndexDataRepository.GetWhere(x => x.Key == token).FirstOrDefault();
            if (hit == null) return [];

            //Combine note data with match data for search hits
            //The frontend can use this to show summaries
            //Full data can be pulled with the noteid
            var summaries = hit.Data.Matches.Select(kvp =>
            {
                var noteId = kvp.Key;
                var matchData = kvp.Value;
                var noteData = NoteDataRepository.GetWhere(note => note.Id == noteId).First();
                return new SearchSummary
                {
                    NoteId = noteId.ToString(),
                    Title = noteData.Title,
                    Tags = noteData.Tags.ToList(),
                    Score = matchData.Score(),
                    Archived = noteData.Archived
                };
            }).Where(x => includeArchived || !x.Archived).ToList();
            return summaries;
        }

        private void AddToIndex(NoteData note)
        {
            var index = ComputeIndex(note);
            foreach (var (key, matches) in index)
            {
                var indexData = IndexDataRepository.GetWhere(x => x.Key == key).FirstOrDefault();
                if (indexData == null)
                {
                    indexData = new IndexData
                    {
                        Key = key,
                        Data = new MatchDataCollection
                        {
                            Matches = matches,
                        }
                    };
                }
                else
                {
                    indexData.Data.Combine(matches);
                }
                IndexDataRepository.Upsert(indexData);
            }
        }

        private void RemoveFromIndex(NoteData note)
        {
            var index = ComputeIndex(note);
            foreach (var (key, matches) in index)
            {
                var indexData = IndexDataRepository.GetWhere(x => x.Key == key).FirstOrDefault();
                if (indexData == null) return;

                indexData.Data.Remove(matches);

                if (indexData.Data.Matches.Count > 0)
                {
                    IndexDataRepository.Upsert(indexData);
                }
                else
                {
                    IndexDataRepository.RemoveWhere(x => x.Key == key);
                }
            }
        }

        private Dictionary<string, Dictionary<ObjectId, MatchData>> ComputeIndex(NoteData note)
        {
            var index = new Dictionary<string, Dictionary<ObjectId, MatchData>>();

            //Title
            ComputeIndex(index, note, [note.Title], (token, matchData) =>
            {
                if (token == note.Title) matchData.TitleFullMatches += 1;
                else matchData.TitlePartialMatches += 1;
            });

            //Tags
            ComputeIndex(index, note, note.Tags, (token, matchData) =>
            {
                if (note.Tags.Contains(token)) matchData.TagFullMatches += 1;
                else matchData.TagPartialMatches += 1;
            });

            //Text
            var text = MarkdownTokenizer.GetTextTokens(note.Markdown);
            ComputeIndex(index, note, text, (token, matchData) =>
            {
                if (text.Contains(token)) matchData.TextFullMatches += 1;
                else matchData.TextPartialMatches += 1;
            });

            return index;
        }

        private void ComputeIndex
        (
            Dictionary<string, Dictionary<ObjectId, MatchData>> index,
            NoteData note,
            IEnumerable<string> field,
            Action<string, MatchData> countMatches
        )
        {
            foreach (var token in Tokenize(field))
            {
                if (!index.ContainsKey(token)) index.Add(token, []);

                var entry = index[token];
                if (!entry.ContainsKey(note.Id)) entry.Add(note.Id, new MatchData());

                countMatches(token, entry[note.Id]);
            }
        }

        private HashSet<string> Tokenize(IEnumerable<string> strs)
        {
            var set = new HashSet<string>();

            var stems = strs.SelectMany(x => Stemmer.Stem(x).Union([x]))
                .Where(x => x.Length >= 3)
                .Distinct();

            foreach (var str in stems)
            {
                for (var i = 3; i <= str.Length; i++)
                {
                    set.Add(str[..i]);
                }
            }

            return set;
        }
    }
}
