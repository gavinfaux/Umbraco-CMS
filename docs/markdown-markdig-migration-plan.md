# Markdown Dependency Migration Plan

See also: [Configuration Guide](markdown-pipelines.md)

Last verified against commit `fba1d4d70046dfb921050b0041aac58152c4c629`.

**Issue**: [umbraco/Umbraco-CMS#19500](https://github.com/umbraco/Umbraco-CMS/issues/19500)
**Current package**: hey-red/MarkdownSharp (archived)
**Target package**: xoofx/markdig

## Objectives
- Replace MarkdownSharp with Markdig across the CMS.
- Centralize Markdown-to-HTML conversion behind a reusable service.
- Preserve or improve existing Markdown rendering behaviour, including health check emails and property value conversion.
- Provide a secure, configurable Markdown pipeline that is safe by default yet extensible for integrators.

## Current State
- `MarkdownEditorValueConverter` creates `MarkdownSharp.Markdown` directly during conversion.
- `MarkdownToHtmlConverter` is registered for DI, but sits in `Umbraco.Cms.Infrastructure.HealthChecks` and contains logic specific to highlighting health check statuses.
- `IMarkdownToHtmlConverter` lives under `Umbraco.Cms.Core.HealthChecks.NotificationMethods`, narrowing its perceived scope.
- Health check email notifications rely on the converter to render Markdown and to apply custom highlighting.
- No unified approach for controlling Markdown parsing features; HTML rendering is always enabled and unmanaged.

## Markdig Capabilities & Constraints
- Markdig offers >20 optional extensions (tables, footnotes, media, emojis, math, etc.) and supports disabling pieces of the core CommonMark pipeline, including HTML parsing.
- The library is fast, CommonMark compliant, and exposes an AST for advanced scenarios.
- HTML parsing can be disabled or restricted; output can be post-processed if sanitization is required.
- Markdig pipelines are immutable once built, encouraging predefined configurations that can be shared via DI.

## Proposed Changes (finalized)

### 1. Restructure the Markdown converter service
- Move `IMarkdownToHtmlConverter` into `Umbraco.Cms.Core.Strings` and update namespaces and file locations accordingly.
- Move the implementation into `Umbraco.Cms.Infrastructure.Strings` (new folder if needed) and update DI registration in `UmbracoBuilder.CoreServices` to match.
- Adjust the converter API to accept plain Markdown (`string markdown, MarkdownConversionOptions? options = null`) so it serves any feature, not just health checks.
  

### 2. Define configurable default pipeline and options model
- Start with a Markdig pipeline that mirrors HeyRed behaviour: CommonMark core with HTML parsing enabled and no additional extensions.
- Provide configuration via options (e.g., `MarkdownPipelineOptions`) registered in DI so integrators can:
  - disable HTML parsing when a stricter configuration is required;
  - opt into specific Markdig extensions;
  - register custom pipeline builders while keeping the Umbraco default isolated.
- Keep health-check emails on the same default pipeline to avoid maintaining duplicate configurations.
  

### 3. Refactor health check email workflow
- Update `EmailNotificationMethod` to construct the Markdown string (including status highlighting tokens) before calling the converter.
- Move the HTML status highlighting logic out of the converter and into the health check email path, ideally in a small helper method within `EmailNotificationMethod`.
- Ensure the converter usage relies on the single configured pipeline (HTML enabled by default) so email output matches legacy behaviour while remaining configurable via options.
- Confirm any HTML elements required for email rendering are supported by the chosen pipeline (enable targeted extensions if needed).
- Ensure the converter usage remains optional and gracefully handles null or empty Markdown input.

### 4. Update property value conversion
- Inject `IMarkdownToHtmlConverter` into `MarkdownEditorValueConverter` via constructor DI.
- Replace direct instantiation of MarkdownSharp with the shared converter, ensuring preview/local link processing still runs before Markdown conversion.
- Ensure the converter uses the default pipeline and rely on options (e.g., `DisableHtml`) when a stricter configuration is required for specific sites.
- Confirm delivery API paths (which return raw Markdown) remain unchanged.
- Decide whether backoffice preview/editor rendering needs additional extensions and isolate those to a dedicated pipeline if necessary.

### 5. Swap the Markdown engine dependency
- Replace the MarkdownSharp package reference with Markdig in `Directory.Packages.props` and `Umbraco.Infrastructure.csproj`.
- Update `NOTICES.txt` to reflect Markdig licensing.
- Configure Markdig with the chosen default pipeline and ensure no breaking changes to `TypeFinder` or other reflection-based usage.

### 6. Tests and validation
- Add or update unit tests for the converter service covering:
  - Basic Markdown to HTML round-trip using the default pipeline;
  - Handling of empty or null input;
- Pipeline configuration behaviour, ensuring optional hardening (e.g., `DisableHtml`) is respected when configured.
- Add tests for health check email output to confirm status highlighting survives the refactor and HTML output matches the configured default pipeline.
- Run existing test suites/builds that cover property value converters.
- Perform manual spot checks in backoffice for Markdown editor rendering with and without optional extensions.

### 7. Provide optional Markdown-to-plain-text conversion
- Explore adding a companion service that converts Markdown to sanitized plain text for consumers such as search indexing or snippets.
- Keep it optional and resolved via DI so solutions can opt in without affecting default behaviours; consider exposing through the same options model as HTML pipelines.
- If implemented, document how the converter integrates with existing index factories or remains an injectable utility projects can reuse.

## Implementation Plan (feature parity)
1. Update dependencies
   - Replace the MarkdownSharp package entry in `Directory.Packages.props` and `Umbraco.Infrastructure.csproj`; update `NOTICES.txt` for Markdig.
   - Search the solution for remaining `MarkdownSharp` usages and remove them before swapping libraries.
2. Move and refactor the Markdown service
   - Relocate `IMarkdownToHtmlConverter` into `Umbraco.Cms.Core.Strings` and move the implementation to `Umbraco.Cms.Infrastructure.Strings` with a Markdig-backed converter.
   - Adjust DI registration in `UmbracoBuilder.CoreServices` so the new implementation is the singleton service.
   - Update namespace declarations and using directives to reflect the new locations throughout the solution.
3. Configure the Markdig pipeline options
   - Introduce a configuration/options abstraction that exposes a single configurable pipeline and allows consumers to adjust HTML handling or add extensions.
   - Register the pipeline builder in DI and ensure the converter reuses the configured pipeline instance.
   - Add documentation describing how integrators can override or extend the pipeline via DI (see `docs/markdown-pipelines.md`).
4. Update `MarkdownEditorValueConverter`
   - Inject `IMarkdownToHtmlConverter` (and pipeline selector if separate) via constructor and replace direct `new Markdown()` calls with the default pipeline.
   - Replace instantiations like `var mark = new Markdown()` and remove `using HeyRed.MarkdownSharp` by refactoring the affected files.
   - Confirm Delivery API behaviour remains unchanged (still returning raw Markdown).
5. Refactor health check email workflow
   - Move the status-highlighting logic into `EmailNotificationMethod` and call the converter.
   - Ensure `MarkdownToHtmlConverter` no longer manipulates health-check-specific HTML; focus it solely on Markdown -> HTML rendering.
   - Add null/empty guards so emails degrade gracefully without converter output.
6. Cleanup and tests
   - Remove obsolete helpers/imports tied to MarkdownSharp and run `dotnet format`/`dotnet build` to confirm compilation.
   - Add/update unit tests for both pipelines and the email highlighting changes; consider snapshot tests for converter output.
   - Verify highlighting, property value conversion, and delivery API scenarios still mirror current behaviour before migrating further features.

## Indexing Considerations
- Markdown editor data is indexed via the default property index value factory (`MarkdownPropertyEditor` -> `DefaultPropertyIndexValueFactory`), which writes the stored markdown string without transforming it.
- Search indexes therefore contain the raw Markdown (including syntax markers); a future plain-text converter could strip formatting for improved indexing or be exposed for custom pipelines.

## Risks & Mitigations
- **Behaviour changes due to Markdig pipeline**: Start with a pipeline that mirrors current features; document and test any extensions added later.
- **Highlighting logic regression**: Keep focused tests around the email formatting to catch regressions.
- **Breaking API change**: Moving the interface may affect downstream packages; document the change and consider adding `[Obsolete]` alias wrappers if needed.
- **Security regressions from HTML parsing**: Default the pipeline to safe settings and make opt-in/explicit decisions for scenarios that need raw HTML (e.g., health checks).

## Open Questions
- Which Markdig extensions (if any) should ship enabled in the single pipeline by default (current: none)?
- Should there be an optional plain-text converter for indexing/snippets?
- Any known customizations relying on pre-migration namespaces that need guidance?

## Next Steps
1. Confirm acceptability of the proposed service API shape, namespace move, and options model.
2. Design the secure default Markdig pipeline and document optional extension points.
3. Prototype the Markdig implementation and run tests to verify rendering parity and security defaults.
4. Share findings on behavioural differences (if any) before finalizing the PR.
5. Plan the bulk code updates, including any batch refactors and verification steps, once the design is approved.

## Implementation Status
- Done
  - Introduced `Umbraco.Cms.Core.Strings.IMarkdownToHtmlConverter` with a simplified `ToHtml(string markdown)` signature and a single cached Markdig pipeline.
  - Implemented Markdig-based converter in `Umbraco.Cms.Infrastructure.Strings.MarkdownToHtmlConverter`.
  - Registered converter in DI (`UmbracoBuilder.CoreServices`).
  - Switched `MarkdownEditorValueConverter` to inject and use the converter (Default pipeline, HTML allowed by default but configurable).
  - Refactored `EmailNotificationMethod` to build markdown, convert with the default pipeline (HTML enabled), and apply status highlighting locally.
  - Replaced package dependency: removed MarkdownSharp, added Markdig in `Directory.Packages.props` and `Umbraco.Infrastructure.csproj`.
  - Updated `NOTICES.txt` entry from MarkdownSharp to Markdig.
  - Added unit tests: `MarkdownToHtmlConverterTests` for HTML disabled/enabled behavior; updated existing `MarkdownEditorValueConverterTests` to use injected converter.
- Verified
  - Tests added
    - MarkdownToHtmlConverterTests validates HTML disabled/enabled pipelines.
    - EmailNotificationMethodTests validates highlighting spans and HTML body.
    - DeliveryApi test validates raw markdown passthrough remains unchanged.
  - Verified
  - Build succeeds across Infrastructure and UnitTests.
  - Scan shows no code references to HeyRed.MarkdownSharp or `new Markdown()` remaining.

## Remaining

- **Configuration:** Finalize the `MarkdownPipelinesOptions` callback surface so integrators can adjust the single default pipeline (HTML, extensions, etc.).
- **Tests:**
  - Add tests to assert email highlighting output contains colored spans for Success/Warning/Error.
  - Add an integration-style assertion that Delivery API paths still return raw Markdown unchanged (existing unit test covers converter behaviour for Delivery API value type; consider an explicit Delivery API object test).
- **Documentation:**
  - Document the default pipeline, its HTML-on-by-default behaviour, and how to harden or extend it via DI.
  - Update developer docs with guidance for opting into a custom pipeline in feature code.
- **Manual checks:**
  - Backoffice Markdown editor rendering spot-check.
  - Health check email format rendering (HTML and highlighting) in a test environment.

## Suggested Steps (next)
- Add test coverage for Email highlighting and Delivery API pass-through.
- Sketch and propose a DI options model for adjusting (or, if necessary, extending) the Markdig pipeline:
  - Keep using `MarkdownPipelinesOptions` with a single builder callback for the default pipeline.
  - Document how to disable HTML or add extensions through that callback.
- Write short developer docs illustrating how to inject and select pipelines in custom features.

See also: `docs/markdown-pipelines.md` for pipeline configuration and examples.













## Before vs After (Quick View)

- Parser
  - Before: MarkdownSharp; HTML always allowed; ad hoc usage in callers.
  - After: Markdig; single configurable pipeline (HTML allowed by default).
- API
  - Before: Converter tied to health checks with custom behaviour.
  - After: `IMarkdownToHtmlConverter.ToHtml(string)` in Core; no scenario-specific logic.
- Configuration
  - Before: No central configuration of Markdown behaviour.
  - After: `builder.ConfigureMarkdownPipelines` to tweak Markdig (e.g., disable HTML, add extensions).
- Callers
  - Before: Health checks and editor path each built Markdown independently.
  - After: Both use the shared converter; Delivery API still returns raw Markdown.
- Tests/Docs
  - Before: Limited parity tests; docs tied to legacy usage.
  - After: CommonMark regression test added; docs describe single-pipeline configuration and private pipeline patterns.
