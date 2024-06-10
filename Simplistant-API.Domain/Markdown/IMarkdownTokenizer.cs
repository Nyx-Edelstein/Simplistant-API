namespace Simplistant_API.Domain.Markdown
{
    public interface IMarkdownTokenizer
    {
        string[] GetTextTokens(string markdown);
    }
}
