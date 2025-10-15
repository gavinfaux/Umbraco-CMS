using Markdig;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Strings;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.Strings;

[TestFixture]
public class MarkdownToHtmlConverterTests
{
    [Test]
    public void Default_pipeline_allows_html_parsing()
    {
        IMarkdownToHtmlConverter converter = new MarkdownToHtmlConverter(Options.Create(new MarkdownPipelinesOptions()));
        var markdown = "Hello <b>world</b>";

        var html = converter.ToHtml(markdown);

        StringAssert.Contains("<b>world</b>", html);
    }

    [Test]
    public void Default_pipeline_can_be_hardened_via_configuration()
    {
        var options = Options.Create(new MarkdownPipelinesOptions
        {
            Configure = b => b.DisableHtml()
        });

        IMarkdownToHtmlConverter converter = new MarkdownToHtmlConverter(options);
        var markdown = "Hello <b>world</b>";

        var html = converter.ToHtml(markdown);

        StringAssert.Contains("&lt;b&gt;world&lt;/b&gt;", html);
        StringAssert.DoesNotContain("<b>world</b>", html);
    }

    [Test]
    public void Pipeline_configuration_can_enable_extensions()
    {
        var options = Options.Create(new MarkdownPipelinesOptions
        {
            Configure = b => b.UsePipeTables()
        });

        IMarkdownToHtmlConverter converter = new MarkdownToHtmlConverter(options);
        var markdown = "| a | b |\n|---|---|\n| 1 | 2 |";

        var html = converter.ToHtml(markdown);

        StringAssert.Contains("<table>", html);
        StringAssert.Contains("<td>", html);
    }

    [Test]
    public void Default_pipeline_matches_basic_commonmark_output()
    {
        IMarkdownToHtmlConverter converter = new MarkdownToHtmlConverter();
        const string markdown = "Hello **world**\n\n- One\n- Two";

        var html = converter.ToHtml(markdown);

        const string expected = "<p>Hello <strong>world</strong></p>\n<ul>\n<li>One</li>\n<li>Two</li>\n</ul>\n";
        Assert.AreEqual(expected, html);
    }
}
