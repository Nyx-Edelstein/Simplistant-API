using HtmlAgilityPack;

namespace Simplistant_API.Domain.Markdown
{
    public class MarkdownTokenizer : IMarkdownTokenizer
    {
        /// <summary>
        /// Gets raw text, ignoring markdown symbols
        /// </summary>
        public string[] GetTextTokens(string markdown)
        {
            var html = Markdig.Markdown.ToHtml(markdown);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var rawText = htmlDocument.DocumentNode
                .SelectNodes("//text()")
                .Aggregate("", (s, node) => s + node.InnerText.ToLower() + " ");
            var tokens = rawText.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            return tokens;
        }
    }
}
