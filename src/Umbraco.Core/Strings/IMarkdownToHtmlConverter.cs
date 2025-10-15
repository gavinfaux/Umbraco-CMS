namespace Umbraco.Cms.Core.Strings;

public interface IMarkdownToHtmlConverter
{
    string ToHtml(string markdown);
}
