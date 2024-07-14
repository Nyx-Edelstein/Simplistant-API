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
            //var catalog = NotesRepository.Catalog();
            //For testing:
            var catalog = new List<SearchSummary>
            {
                new()
                {
                    NoteId = ObjectId.NewObjectId().ToString(),
                    Title = Guid.NewGuid().ToString(),
                    Tags =
                    [
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString()
                    ],
                    Score = 10,
                },
                new()
                {
                    NoteId = ObjectId.NewObjectId().ToString(),
                    Title = Guid.NewGuid().ToString(),
                    Tags =
                    [
                        Guid.NewGuid().ToString(),
                    ],
                    Score = 10,
                },
                new()
                {
                    NoteId = ObjectId.NewObjectId().ToString(),
                    Title = Guid.NewGuid().ToString(),
                    Tags =
                    [
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString()
                    ],
                    Score = 5,
                },
                new()
                {
                    NoteId = ObjectId.NewObjectId().ToString(),
                    Title = Guid.NewGuid().ToString(),
                    Tags = [],
                    Score = 3,
                },
            };

            return new SearchSummaryResponse
            {
                Summaries = catalog
            };
        }

        [HttpGet]
        public SearchSummaryResponse SearchNotes(string searchString, bool includeArchived = false)
        {
            //var tokens = searchString.ToLower().Split("");
            //var results = NotesRepository.Search(tokens, includeArchived);

            var results = new List<SearchSummary>
            {
                new()
                {
                    NoteId = ObjectId.NewObjectId().ToString(),
                    Title = searchString,
                    Tags =
                    [
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString(),
                        Guid.NewGuid().ToString()
                    ],
                    Score = 10,
                },
                new()
                {
                    NoteId = ObjectId.NewObjectId().ToString(),
                    Title = Guid.NewGuid().ToString(),
                    Tags =
                    [
                        searchString,
                    ],
                    Score = 5,
                },
            };

            return new SearchSummaryResponse
            {
                Summaries = results
            };
        }

        [HttpGet]
        public NoteResponse GetNote(string noteId)
        {
            //ObjectId objectId;
            //try
            //{
            //    objectId = new ObjectId(noteId);
            //}
            //catch (Exception) //Todo: which exceptions?
            //{
            //    return new NoteResponse
            //    {
            //        Status = ResponseStatus.Error,
            //    };
            //}
            //var noteData = NotesRepository.Get(objectId);
            //var note = new Note
            //{
            //    NoteId = noteData.Id.ToString(),
            //    HistoryId = noteData.HistoryId.ToString(),
            //    Title = noteData.Title,
            //    Tags = noteData.Tags.ToList(),
            //    Markdown = noteData.Markdown,
            //};
            var note = new Note
            {
                NoteId = noteId,
                HistoryId = Guid.NewGuid().ToString(),
                Title = Guid.NewGuid().ToString(),
                Tags = [Guid.NewGuid().ToString(), Guid.NewGuid().ToString()],
                Markdown =
                    """
                    ## Blockquotes

                    You can indicate blockquotes with a >.

                    ```md
                    In the words of Abraham Lincoln:
                        
                    > Pardon my french
                    ```

                    Blockquotes can have multiple paragraphs and can have other block elements inside.

                    ```md
                    > A paragraph of text
                    >
                    > Another paragraph
                    >
                    > - A list
                    > - with items
                    ```

                    ## Bold and Italic

                    You can make text bold or italic.
                    
                        *This text will be italic*
                        **This text will be bold**

                    Both bold and italic can use either a \* or an \_ around the text for styling. This allows you to combine both bold and italic if needed.
                    
                        **Everyone _must_ attend the meeting at 5 o'clock today.**

                    ## Strikethrough

                    With the option **`strikethrough`** enabled, Showdown supports strikethrough elements.
                    The syntax is the same as GFM, that is, by adding two tilde (`~~`) characters around
                    a word or groups of words.

                    ```md
                    a ~~strikethrough~~ element
                    ```

                    a ~~strikethrough~~ element
                    """,
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
            //NotesRepository.Save(note);
            return new MessageResponse { status = ResponseStatus.Success };
        }

        /// <summary>
        /// Deletes all notes with the shared version history
        /// </summary>
        [HttpPost]
        public MessageResponse DeleteNote(string historyId)
        {
            //var parsed = Guid.TryParse(historyId, out var historyGuid);
            //if (!parsed)
            //{
            //    var response = new MessageResponse { status = ResponseStatus.Error };
            //    response.messages.Add($"'{historyId}' is not a valid guid");
            //    return response;
            //}

            //NotesRepository.Delete(historyGuid);
            return new MessageResponse { status = ResponseStatus.Success };
        }
    }
}
