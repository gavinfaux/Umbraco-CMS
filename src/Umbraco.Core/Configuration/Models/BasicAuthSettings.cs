// Copyright (c) Umbraco.
// See LICENSE for more details.

using System.ComponentModel;

namespace Umbraco.Cms.Core.Configuration.Models;

/// <summary>
/// Typed configuration options for basic authentication settings.
/// </summary>
[UmbracoOptions(Constants.Configuration.ConfigBasicAuth)]
public class BasicAuthSettings
{
    private const bool StaticEnabled = false;

    /// <summary>
    /// Gets or sets a value indicating whether Basic Auth Middleware is enabled.
    /// </summary>
    [DefaultValue(StaticEnabled)]
    public bool Enabled { get; set; } = StaticEnabled;

    public ISet<string> AllowedIPs { get; set; } = new HashSet<string>();

    public SharedSecret SharedSecret { get; set; } = new();

    public bool RedirectToLoginPage { get; set; } = false;
}

public class SharedSecret
{
    private const string StaticHeaderName = "X-Authentication-Shared-Secret";

    [DefaultValue(StaticHeaderName)]
    public string? HeaderName { get; set; } = StaticHeaderName;

    public string? Value { get; set; }
}
