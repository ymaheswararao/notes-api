namespace NotesAPI
{
    public class Models
    {
        public record NoteItem(string UserId, string NoteId, string Text, string CreatedAt);
        public record CreateNoteRequest(string Text);
    }
}
