using Markdig;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.Cms.Infrastructure.Strings;

public class MarkdownToHtmlConverter : IMarkdownToHtmlConverter
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownToHtmlConverter()
        : this(Options.Create(new MarkdownPipelinesOptions()))
    {
    }

    public MarkdownToHtmlConverter(IOptions<MarkdownPipelinesOptions> options)
    {
        var builder = new MarkdownPipelineBuilder();
        options?.Value?.Configure?.Invoke(builder);
        _pipeline = builder.Build();
    }

    public string ToHtml(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }
}
