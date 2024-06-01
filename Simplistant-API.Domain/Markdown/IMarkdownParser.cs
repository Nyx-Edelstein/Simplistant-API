namespace Simplistant_API.Domain.Markdown
{
    public interface IMarkdownParser
    {
        string[] GetTextTokens(string markdown);
    }
}
