# Markdown Pipelines (Markdig)

See also: [Migration Plan](markdown-markdig-migration-plan.md)

Last verified against commit `fba1d4d70046dfb921050b0041aac58152c4c629`.

This document describes how Umbraco configures Markdig and how you can customize the Markdown processing pipeline via DI (and how to build private pipelines when needed).

## Breaking Changes

- Interface moved: `IMarkdownToHtmlConverter` now lives in `Umbraco.Cms.Core.Strings` and exposes a single `ToHtml(string markdown)` method. Update namespaces and remove any pipeline arguments in calling code.
- Builder extension: continue configuring the pipeline via `builder.ConfigureMarkdownPipelines(Action<MarkdownPipelinesOptions>)` inside a composer; the options callback now receives a single `MarkdownPipelineBuilder` to mutate.

## Defaults

- Service: `Umbraco.Cms.Core.Strings.IMarkdownToHtmlConverter`.
- Pipeline: A single cached Markdig pipeline built at startup. By default it mirrors the legacy HeyRed behaviour (HTML allowed, no optional extensions enabled).

## Current Defaults and Policy (Up‑to‑date)

- Default pipeline
  - HTML allowed by default to match the legacy HeyRed renderer and keep existing email output unchanged.
  - No Markdig extensions are enabled unless you add them via the options callback (`options.Configure`).
  - Call `b.DisableHtml()` inside the callback if you need to harden server-side rendering.
- Delivery API
  - Returns raw markdown unchanged for the Markdown editor (no HTML processing).
- Rendering on site
  - `MarkdownEditorValueConverter` injects `IMarkdownToHtmlConverter` and renders using the configured pipeline.

## Overriding Pipelines

Use the Umbraco builder extension to configure pipelines:

```
using Markdig;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.Strings;

public class MarkdownComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.ConfigureMarkdownPipelines(opt =>
        {
            opt.Configure = b =>
            {
                // Example: disable HTML and enable pipe tables
                b.DisableHtml();
                b.UsePipeTables();
            };
        });
    }
}
```

Notes:
- The default pipeline now allows HTML by default; call `DisableHtml()` if you need to sanitize separately.
- Any extensions you add through `options.Configure` remain active unless you remove them.

## Consuming the Converter

Inject and use `IMarkdownToHtmlConverter` in your code:

```
using Umbraco.Cms.Core.Strings;

public class MyService
{
    private readonly IMarkdownToHtmlConverter _converter;
    public MyService(IMarkdownToHtmlConverter converter) => _converter = converter;

    public string Render(string markdown)
        => _converter.ToHtml(markdown);
}
```

The converter reuses a single Markdig pipeline that you configure via DI.

## Preview Parity (marked)

- Backoffice preview uses `marked` and sanitizes output via a `sanitizeHTML` utility (DOMPurify under the hood).
- Minimal GFM‑lite features commonly seen in preview:
  - Pipe tables → `b.UsePipeTables(new PipeTableOptions { UseHeaderForColumnCount = true })`
  - Task lists → `b.UseTaskLists()`
  - Autolinks → `b.UseAutoLinks()`
- Line breaks (“breaks” in marked):
  - marked `breaks` is false by default; to match rendering single newlines as `<br>`, enable:
    - `b.UseSoftlineBreakAsHardlineBreak()` in Markdig.

## Sanitizing HTML (server‑side)

The default pipeline allows HTML; sanitize server-side when rendering untrusted content or disable HTML via configuration if needed:

```
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Strings;

public class HtmlEnabledRenderer
{
    private readonly IMarkdownToHtmlConverter _converter;
    private readonly IHtmlSanitizer _sanitizer;

    public HtmlEnabledRenderer(IMarkdownToHtmlConverter converter, IHtmlSanitizer sanitizer)
    {
        _converter = converter;
        _sanitizer = sanitizer;
    }

    public string Render(string markdown)
    {
        var html = _converter.ToHtml(markdown); // HTML currently allowed
        return _sanitizer.Sanitize(html);
    }
}
```

## Example: blogContent (its own pipeline, advanced + HTML, sanitized)

You can reuse the built‑in `Umbraco.MarkdownEditor` data editor with a custom value converter scoped to an alias (e.g., `blogContent`) and build a private Markdig pipeline — without modifying the shared Default pipeline. This keeps site‑specific behavior isolated.

Value converter (alias‑scoped, builds its own pipeline, enables HTML + sanitizes):

```
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Templates;
using Markdig;

[DefaultPropertyValueConverter]
public class BlogContentValueConverter : PropertyValueConverterBase
{
    private readonly HtmlLocalLinkParser _localLinks;
    private readonly HtmlUrlParser _urls;
    private readonly IHtmlSanitizer _sanitizer;

    public BlogContentValueConverter(HtmlLocalLinkParser localLinks, HtmlUrlParser urls, IHtmlSanitizer sanitizer)
    {
        _localLinks = localLinks;
        _urls = urls;
        _sanitizer = sanitizer;
    }

    public override bool IsConverter(IPublishedPropertyType pt) => pt.EditorAlias == "blogContent";
    public override Type GetPropertyValueType(IPublishedPropertyType pt) => typeof(IHtmlEncodedString);
    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType pt) => PropertyCacheLevel.Snapshot;

    public override object? ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType pt, object? source, bool preview)
    {
        if (source is null) return null;
        var s = source.ToString()!;
        s = _localLinks.EnsureInternalLinks(s, preview);
        s = _urls.EnsureUrls(s);
        return s;
    }

    public override object ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType pt, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
    {
        var markdown = inter as string;
        if (string.IsNullOrEmpty(markdown))
            return new HtmlEncodedString(string.Empty);

        // Build a private Markdig pipeline for blogContent
        // HTML is allowed (no DisableHtml); enable the advanced syntax required for blog content
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            // Optional parity for single newlines -> <br>
            // .UseSoftlineBreakAsHardlineBreak()
            .Build();

        var html = Markdig.Markdown.ToHtml(markdown, pipeline);
        var safe = _sanitizer.Sanitize(html); // Always sanitize when HTML is allowed
        return new HtmlEncodedString(safe);
    }
}
```

Notes:
- Default pipeline allows HTML unless you disable it via configuration and does not add extensions by default.
- The example above builds a private pipeline so the shared Default pipeline remains unchanged.
- Always sanitize when enabling HTML for editor content.

## DI-based blogContent example (renderer + composer + converter)

Rather than constructing a pipeline in the value converter, you can register a small service that caches its own pipeline and inject it where needed. This keeps the shared Default pipeline untouched and improves performance by reusing one pipeline instance.

Renderer service:

```
using Markdig;

public interface IBlogContentMarkdownRenderer
{
    string ToHtml(string markdown);
}

public sealed class BlogContentMarkdownRenderer : IBlogContentMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public BlogContentMarkdownRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            // Optional parity for single newlines -> <br>
            // .UseSoftlineBreakAsHardlineBreak()
            .Build();
    }

    public string ToHtml(string markdown) =>
        string.IsNullOrEmpty(markdown) ? string.Empty : Markdig.Markdown.ToHtml(markdown, _pipeline);
}
```

Composer registration:

```
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

public sealed class BlogContentComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IBlogContentMarkdownRenderer, BlogContentMarkdownRenderer>();
        // IHtmlSanitizer is already registered (Noop by default) – replace it if you need strict sanitization.
    }
}
```

Alias-scoped value converter:

```
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Templates;

[DefaultPropertyValueConverter]
public sealed class BlogContentValueConverter : PropertyValueConverterBase
{
    private readonly HtmlLocalLinkParser _localLinks;
    private readonly HtmlUrlParser _urls;
    private readonly IBlogContentMarkdownRenderer _renderer;
    private readonly IHtmlSanitizer _sanitizer;

    public BlogContentValueConverter(
        HtmlLocalLinkParser localLinks,
        HtmlUrlParser urls,
        IBlogContentMarkdownRenderer renderer,
        IHtmlSanitizer sanitizer)
    {
        _localLinks = localLinks;
        _urls = urls;
        _renderer = renderer;
        _sanitizer = sanitizer;
    }

    public override bool IsConverter(IPublishedPropertyType pt) => pt.EditorAlias == "blogContent";
    public override Type GetPropertyValueType(IPublishedPropertyType pt) => typeof(IHtmlEncodedString);
    public override PropertyCacheLevel GetPropertyCacheLevel(IPublishedPropertyType pt) => PropertyCacheLevel.Snapshot;

    public override object? ConvertSourceToIntermediate(IPublishedElement owner, IPublishedPropertyType pt, object? source, bool preview)
    {
        if (source is null) return null;
        var s = source.ToString()!;
        s = _localLinks.EnsureInternalLinks(s, preview);
        s = _urls.EnsureUrls(s);
        return s;
    }

    public override object ConvertIntermediateToObject(IPublishedElement owner, IPublishedPropertyType pt, PropertyCacheLevel referenceCacheLevel, object? inter, bool preview)
    {
        var markdown = inter as string;
        if (string.IsNullOrEmpty(markdown)) return new HtmlEncodedString(string.Empty);

        var html = _renderer.ToHtml(markdown);   // allow HTML via the custom pipeline
        var safe = _sanitizer.Sanitize(html);    // always sanitize when HTML is allowed
        return new HtmlEncodedString(safe);
    }
}
```

See also examples:
- docs/examples/BlogContentMarkdownRenderer.cs
- docs/examples/BlogContentComposer.cs
- docs/examples/BlogContentValueConverter.cs

