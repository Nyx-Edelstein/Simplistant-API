using Simplistant_API.Domain.Search;
using Simplistant_API.Models.Data;

namespace Simplistant_API.Domain.NotesRepository
{
    public interface INotesRepository
    {
        public void Save(NoteData note);
        public void Delete(Guid noteItemId);
        public List<SearchSummary> Search(string[] searchTokens, bool includeArchived);
    }
}
