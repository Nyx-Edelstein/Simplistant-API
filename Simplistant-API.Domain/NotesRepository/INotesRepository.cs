using Simplistant_API.Models.Data;

namespace Simplistant_API.Domain.NotesRepository
{
    public interface INotesRepository
    {
        public void Add(NoteData note);
        public void Delete(NoteData note);
        public void Update(NoteData note);
        public void Search(string[] searchTokens);
    }
}
