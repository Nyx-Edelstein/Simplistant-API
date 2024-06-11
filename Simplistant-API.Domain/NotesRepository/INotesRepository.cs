using LiteDB;
using Simplistant_API.DTO.Notes;
using Simplistant_API.Models.Data;

namespace Simplistant_API.Domain.NotesRepository
{
    public interface INotesRepository
    {
        public NoteData? Get(ObjectId noteId);
        public void Save(Note note);
        public void Delete(Guid historyId);
        public List<SearchSummary> Catalog();
        public List<SearchSummary> Search(string[] searchTokens, bool includeArchived);
    }
}
