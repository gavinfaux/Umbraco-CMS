using Moq;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors.ValueConverters;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Core.Templates;
using Umbraco.Cms.Core.PropertyEditors;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.DeliveryApi;

[TestFixture]
public class MarkdownEditorDeliveryApiTests
{
    [Test]
    public void DeliveryApi_returns_raw_markdown_unchanged()
    {
        var linkParser = new HtmlLocalLinkParser(Mock.Of<IPublishedUrlProvider>());
        var urlParser = new HtmlUrlParser(Mock.Of<IOptionsMonitor<ContentSettings>>(),
            Mock.Of<ILogger<HtmlUrlParser>>(),
            Mock.Of<IProfilingLogger>(),
            Mock.Of<IIOHelper>());
        var markdownConverter = Mock.Of<IMarkdownToHtmlConverter>();
        var converter = new MarkdownEditorValueConverter(linkParser, urlParser, markdownConverter);

        var inter = "Hello <b>world</b> and **markdown**";
        var result = converter.ConvertIntermediateToDeliveryApiObject(
            Mock.Of<IPublishedElement>(),
            Mock.Of<IPublishedPropertyType>(),
            global::Umbraco.Cms.Core.PropertyEditors.PropertyCacheLevel.Element,
            inter,
            false,
            false);
        Assert.AreEqual(inter, result);
    }
}
