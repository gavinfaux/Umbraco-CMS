# Repository Guidelines

## Project Structure & Module Organization
- `src/` holds the .NET projects (`Umbraco.Cms`, `Umbraco.Web.UI`, `Umbraco.Web.UI.Client`) that ship the CMS, APIs, and backoffice.
- `tests/` groups NUnit suites: Unit, Integration, Acceptance, plus shared fixtures in `Umbraco.Tests.Common` and benchmarks.
- `templates/` contains installer blueprints and sample packages; only commit reusable templates here.
- `build/` hosts Azure Pipelines YAML and shared steps driving CI/CD.
- `docs/` stores contributor references and migration notes aligned with release planning.

## Build, Test, and Development Commands
- Use the SDK pinned by `global.json`; `dotnet --version` should match `10.0.100-rc.1.25451.107`.
- Backend workflow: `dotnet restore umbraco.sln`, `dotnet build umbraco.sln -c Release`, then `dotnet test umbraco.sln -c Release`.
- Focus runs with `dotnet test tests/Umbraco.Tests.UnitTests/Umbraco.Tests.UnitTests.csproj --filter "TestCategory!=Slow"`; integration coverage uses `tests/Umbraco.Tests.Integration/Umbraco.Tests.Integration.csproj`.
- Backoffice client: inside `src/Umbraco.Web.UI.Client`, run `npm ci`, `npm run dev` for local work, `npm run build:for:cms` to sync static assets, and `npm run backoffice:test:e2e` for Playwright runs.

## Coding Style & Naming Conventions
- `.editorconfig` enforces UTF-8, trailing newlines, and four-space indentation for C#; JSON/YAML/web assets stay at two spaces.
- Apply .NET naming defaults (PascalCase for types, camelCase for locals, `Async` suffix on asynchronous methods) and keep nullable annotations accurate because `Nullable` is enabled.
- Run `npm run lint` and `npm run format` before committing TypeScript; keep component folder names aligned with their custom elements under `src/Umbraco.Web.UI.Client/src`.

## Testing Guidelines
- Place fast checks in `Umbraco.Tests.UnitTests`; share builders via `tests/Umbraco.Tests.Common`.
- Integration tests default to SQLite but can target LocalDB; clean stalled `tests/Umbraco.Tests.Integration/TEMP` artifacts after failures.
- Respect NUnit categories so `--filter "TestCategory!=Slow"` remains useful.
- Front-end unit tests rely on Web Test Runner; run `npm test` or `npm run test:watch` and regenerate schema fixtures with `npm run generate:check-const-test` when models change.

## Commit & Pull Request Guidelines
- Mirror existing history: `Area: concise summary (#issue-or-PR)` in commit subjects, with short bodies covering intent and key changes.
- PRs should link issues, list verification steps (`dotnet test`, `npm test`, e2e when relevant), and include UI screenshots for visual updates.
- Ensure Azure Pipelines succeed (YAML lives under `build/`) and request the owners listed in `CODEOWNERS` for shared components.

## Security & Configuration Tips
- Keep secrets out of source control; base local settings on the `.env*` files in `src/Umbraco.Web.UI.Client` and `appsettings.Development.json`.
- Coordinate dependency upgrades through `Directory.Packages.props` and the client lock file, verifying with `dotnet restore` and `npm ci` before pushing.
