using System.Reflection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.HealthChecks;
using Umbraco.Cms.Core.HealthChecks.NotificationMethods;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Mail;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.HealthChecks;

[TestFixture]
public class EmailNotificationMethodTests
{
    private sealed class CapturingEmailSender : IEmailSender
    {
        public EmailMessage? LastMessage { get; private set; }

        public Task SendAsync(EmailMessage message, string emailType) { LastMessage = message; return Task.CompletedTask; }
        public Task SendAsync(EmailMessage message, string emailType, bool enableNotification) { LastMessage = message; return Task.CompletedTask; }
        public bool CanSendRequiredEmail() => true;
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public TestOptionsMonitor(T value) { CurrentValue = value; }
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<T, string> listener) => new Noop();
        private sealed class Noop : IDisposable { public void Dispose() { } }
    }

    private sealed class FakeLocalizedTextService : ILocalizedTextService
    {
        // Returns a simple concatenation of token values, ensuring the 3rd token (html) is included in the message body
        public string Localize(string? area, string? alias, System.Globalization.CultureInfo? culture, IDictionary<string, string?>? tokens = null)
            => tokens is null ? string.Empty : string.Join(" ", tokens.Values.Where(v => v is not null));

        public IDictionary<string, IDictionary<string, string>> GetAllStoredValuesByAreaAndAlias(System.Globalization.CultureInfo culture) => new Dictionary<string, IDictionary<string, string>>();
        public IDictionary<string, string> GetAllStoredValues(System.Globalization.CultureInfo culture) => new Dictionary<string, string>();
        public IEnumerable<System.Globalization.CultureInfo> GetSupportedCultures() => Array.Empty<System.Globalization.CultureInfo>();
        public System.Globalization.CultureInfo ConvertToSupportedCultureWithRegionCode(System.Globalization.CultureInfo currentCulture) => currentCulture;
    }

    [Test]
    public async Task Email_contains_highlighted_status_spans_and_is_html()
    {
        // Arrange settings to enable email notifications and set recipient
        var hcSettings = new HealthChecksSettings
        {
            Notification = new HealthChecksNotificationSettings
            {
                Enabled = true,
                NotificationMethods = new Dictionary<string, HealthChecksNotificationMethodSettings>
                {
                    ["email"] = new HealthChecksNotificationMethodSettings
                    {
                        Enabled = true,
                        FailureOnly = false,
                        Verbosity = HealthCheckNotificationVerbosity.Summary,
                        Settings = new Dictionary<string, string> { ["RecipientEmail"] = "admin@example.com" }
                    }
                }
            }
        };
        var hcOptions = new TestOptionsMonitor<HealthChecksSettings>(hcSettings);

        var contentSettings = new ContentSettings { Notifications = new ContentNotificationSettings { Email = "noreply@example.com" } };
        var contentOptions = new TestOptionsMonitor<ContentSettings>(contentSettings);

        var lts = new FakeLocalizedTextService();
        var hosting = new Mock<IHostingEnvironment>();
        hosting.SetupGet(x => x.ApplicationMainUrl).Returns((Uri?)null);

        var sender = new CapturingEmailSender();

        // Converter returns the same string so we can assert highlighting replacement on the markdown itself
        var converter = new Mock<IMarkdownToHtmlConverter>();
        converter.Setup(x => x.ToHtml(It.IsAny<string>()))
            .Returns<string>(s => s);

        var email = new EmailNotificationMethod(lts, hosting.Object, sender, hcOptions, contentOptions, converter.Object);

        // Build HealthCheckResults instance via private constructor
        var statuses = new List<HealthCheckStatus>
        {
            new("ok") { ResultType = StatusResultType.Success },
            new("warn") { ResultType = StatusResultType.Warning },
            new("err") { ResultType = StatusResultType.Error }
        };
        var dict = new Dictionary<string, IEnumerable<HealthCheckStatus>> { ["Demo Check"] = statuses };
        var ctor = typeof(HealthCheckResults).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Dictionary<string, IEnumerable<HealthCheckStatus>>), typeof(bool) }, null);
        Assert.NotNull(ctor);
        var results = (HealthCheckResults)ctor!.Invoke(new object[] { dict, false });

        // Act
        await email.SendAsync(results);

        // Assert
        Assert.NotNull(sender.LastMessage);
        StringAssert.Contains("<span style=\"color: #5cb85c\">Success</span>", sender.LastMessage!.Body);
        StringAssert.Contains("<span style=\"color: #f0ad4e\">Warning</span>", sender.LastMessage!.Body);
        StringAssert.Contains("<span style=\"color: #d9534f\">Error</span>", sender.LastMessage!.Body);
        Assert.IsTrue(sender.LastMessage!.IsBodyHtml);
    }
}
