using Simplistant_API.Domain.Markdown;
using Simplistant_API.Domain.Stemming;
using Simplistant_API.Models.Data;
using Simplistant_API.Repository;

namespace Simplistant_API.Domain.NotesRepository
{
    public class NotesRepository : INotesRepository
    {
        private IRepository<IndexKeys> IndexKeysRepository { get; }
        private IRepository<IndexEntries> IndexEntriesRepository { get; }
        private IRepository<NoteData> NoteDataRepository { get; }
        private IMarkdownParser MarkdownParser { get; }
        private IStemmer Stemmer { get; }

        public NotesRepository
        (
            IRepository<IndexKeys> indexKeysRepository,
            IRepository<IndexEntries> indexEntriesRepository,
            IRepository<NoteData> noteDataRepository,
            IMarkdownParser markdownParser,
            IStemmer stemmer
        )
        {
            IndexKeysRepository = indexKeysRepository;
            IndexEntriesRepository = indexEntriesRepository;
            NoteDataRepository = noteDataRepository;
            MarkdownParser = markdownParser;
            Stemmer = stemmer;
        }

        public void Add(NoteData note)
        {
            throw new NotImplementedException();
        }

        public void Delete(NoteData note)
        {
            throw new NotImplementedException();
        }

        public void Update(NoteData note)
        {
            throw new NotImplementedException();
        }

        public void Search(string[] searchTokens)
        {
            throw new NotImplementedException();
        }
    }
}
