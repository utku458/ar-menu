# ADR-0001: Target .NET 10 LTS

- **Status:** Accepted
- **Date:** 2026-09-13

## Context

The project starts in September 2026. The initial plan mentioned .NET 8 or 9, but both reach end of support on
2026-11-10, two months after the first commit. .NET 10 is the current long-term support release, supported until
2028-11-14.

## Decision

- Target `net10.0` for every project, set once in `Directory.Build.props`.
- Pin the SDK in `global.json` (`10.0.401`, `rollForward: latestFeature`).
- Manage NuGet versions centrally (`Directory.Packages.props`) and pin tools with a local manifest (`dotnet-tools.json`).
- Treat warnings as errors with the `latest-recommended` analyzer set. Every suppression in `.editorconfig` carries its reason.

## Consequences

- EF Core 10 is available. Its **named query filters** let the tenant filter and the soft-delete filter be disabled
  independently. With a single anonymous filter, an "include deleted" query would also disable tenant isolation.
- C# 14, built-in OpenAPI document generation and HybridCache come with the platform.
- Contributors and CI need the .NET 10 SDK. Upgrading to .NET 12 (the next LTS) means changing one property.
