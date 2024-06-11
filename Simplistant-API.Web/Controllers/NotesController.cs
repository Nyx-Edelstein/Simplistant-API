using LiteDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Simplistant_API.Domain.NotesRepository;
using Simplistant_API.DTO;
using Simplistant_API.DTO.Notes;

namespace Simplistant_API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("/[controller]/[action]")]
    public class NotesController : ControllerBase
    {
        private INotesRepository NotesRepository { get; }

        public NotesController(INotesRepository notesRepository)
        {
            NotesRepository = notesRepository;
        }

        [HttpGet]
        public SearchSummaryResponse GetNotesCatalog()
        {
            var catalog = NotesRepository.Catalog();
            return new SearchSummaryResponse
            {
                Summaries = catalog
            };
        }

        [HttpGet]
        public SearchSummaryResponse SearchNotes(string searchString, bool includeArchived = false)
        {
            var tokens = searchString.ToLower().Split("");
            var results = NotesRepository.Search(tokens, includeArchived);
            return new SearchSummaryResponse
            {
                Summaries = results
            };
        }

        [HttpGet]
        public NoteResponse GetNote(string noteId)
        {
            ObjectId objectId;
            try
            {
                objectId = new ObjectId(noteId);
            }
            catch (Exception) //Todo: which exceptions?
            {
                return new NoteResponse
                {
                    Status = ResponseStatus.Error,
                };
            }
            var noteData = NotesRepository.Get(objectId);
            var note = new Note
            {
                NoteId = noteData.Id.ToString(),
                HistoryId = noteData.HistoryId.ToString(),
                Title = noteData.Title,
                Tags = noteData.Tags.ToList(),
                Markdown = noteData.Markdown,
            };
            return new NoteResponse
            {
                Status = ResponseStatus.Success,
                Note = note
            };
        }

        [HttpPost]
        public MessageResponse SaveNote(Note note)
        {
            NotesRepository.Save(note);
            return new MessageResponse { status = ResponseStatus.Success };
        }

        /// <summary>
        /// Deletes all notes with the shared version history
        /// </summary>
        [HttpPost]
        public MessageResponse DeleteNote(string historyId)
        {
            var parsed = Guid.TryParse(historyId, out var historyGuid);
            if (!parsed)
            {
                var response = new MessageResponse { status = ResponseStatus.Error };
                response.messages.Add($"'{historyId}' is not a valid guid");
                return response;
            }

            NotesRepository.Delete(historyGuid);
            return new MessageResponse { status = ResponseStatus.Success };
        }
    }
}
