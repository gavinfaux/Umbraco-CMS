using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Api.Common.Builders;
using Umbraco.Cms.Api.Management.ViewModels.Help;
using Umbraco.Cms.Api.Common.ViewModels.Pagination;

namespace Umbraco.Cms.Api.Management.Controllers.Help;

[ApiVersion("1.0")]
public class GetHelpController : HelpControllerBase
{
    private readonly ILogger<GetHelpController> _logger;
    private readonly IJsonSerializer _jsonSerializer;
    private HelpPageSettings _helpPageSettings;

    public GetHelpController(
        IOptionsMonitor<HelpPageSettings> helpPageSettings,
        ILogger<GetHelpController> logger,
        IJsonSerializer jsonSerializer)
    {
        _logger = logger;
        _jsonSerializer = jsonSerializer;
        _helpPageSettings = helpPageSettings.CurrentValue;
        helpPageSettings.OnChange(UpdateHelpPageSettings);
    }

    private void UpdateHelpPageSettings(HelpPageSettings settings) => _helpPageSettings = settings;

    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PagedViewModel<HelpPageResponseModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken,
        string section,
        string? tree,
        int skip = 0,
        int take = 100,
        string? baseUrl = "https://our.umbraco.com")
    {
        if (IsAllowedUrl(baseUrl) is false)
        {
            _logger.LogError($"The following URL is not listed in the allowlist for HelpPage in HelpPageSettings: {baseUrl}");

            ProblemDetails invalidModelProblem =
                new ProblemDetailsBuilder()
                    .WithTitle("Invalid database configuration")
                    .WithDetail("The provided database configuration is invalid")
                    .Build();

            return BadRequest(invalidModelProblem);
        }

        var url = string.Format(baseUrl + "/Umbraco/Documentation/Lessons/GetContextHelpDocs?sectionAlias={0}&treeAlias={1}", section, tree);

        try
        {
            var httpClient = new HttpClient();

            // fetch dashboard json and parse to JObject
            var json = await httpClient.GetStringAsync(url);
            List<HelpPageResponseModel>? result = _jsonSerializer.Deserialize<List<HelpPageResponseModel>>(json);
            if (result != null)
            {
                return Ok(new PagedViewModel<HelpPageResponseModel>
                {
                    Total = result.Count,
                    Items = result.Skip(skip).Take(take),
                });
            }
        }
        catch (HttpRequestException rex)
        {
            _logger.LogInformation($"Check your network connection, exception: {rex.Message}");
        }

        return Ok(PagedViewModel<HelpPageResponseModel>.Empty());
    }

    private bool IsAllowedUrl(string? url) => url is null || _helpPageSettings.HelpPageUrlAllowList.Contains(url);
}
