namespace Umbraco.Cms.Core.Media;

public interface IEmbedProvider
{
    /// <summary>
    ///     The OEmbed API Endpoint
    /// </summary>
    string ApiEndpoint { get; }

    /// <summary>
    ///     A string array of Regex patterns to match against the pasted OEmbed URL
    /// </summary>
    string[] UrlSchemeRegex { get; }

    /// <summary>
    ///     A collection of querystring request parameters to append to the API URL
    /// </summary>
    /// <example>?key=value&amp;key2=value2</example>
    Dictionary<string, string> RequestParams { get; }

    Task<string?> GetMarkupAsync(string url, int? maxWidth, int? maxHeight, CancellationToken cancellationToken);
}
