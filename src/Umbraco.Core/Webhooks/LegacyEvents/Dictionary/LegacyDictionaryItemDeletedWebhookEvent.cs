using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Umbraco.Cms.Core.Webhooks.Events;

[WebhookEvent("Dictionary Item Deleted")]
public class LegacyDictionaryItemDeletedWebhookEvent : WebhookEventBase<DictionaryItemDeletedNotification>
{
    public LegacyDictionaryItemDeletedWebhookEvent(
        IWebhookFiringService webhookFiringService,
        IWebhookService webHookService,
        IOptionsMonitor<WebhookSettings> webhookSettings,
        IServerRoleAccessor serverRoleAccessor)
        : base(webhookFiringService, webHookService, webhookSettings, serverRoleAccessor)
    {
    }

    public override string Alias => Constants.WebhookEvents.Aliases.DictionaryItemDeleted;

    public override object? ConvertNotificationToRequestPayload(DictionaryItemDeletedNotification notification)
        => notification.DeletedEntities;
}
