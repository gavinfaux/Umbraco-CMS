using System;
using Markdig;

namespace Umbraco.Cms.Infrastructure.Strings;

public class MarkdownPipelinesOptions
{
    public Action<MarkdownPipelineBuilder>? Configure { get; set; }
}
