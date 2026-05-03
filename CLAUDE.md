# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Purpose

CLI tooling for administering a cloud Elasticsearch instance. The console app is the entrypoint; reusable Elasticsearch logic (clients, index/template/ILM/role management, etc.) lives in the Core class library so it can be exercised from the CLI today and reused from other hosts later.

## Solution layout

- `ElasticHelpers.SysAdmin.slnx` — solution file in the **new XML `.slnx` format**. Requires Visual Studio 2022 17.10+ or `dotnet` SDK with slnx support. Edit it as plain XML; the legacy `.sln` format is not used here.
- `ElasticHelpers.SysAdmin.Core/` — class library (DLL). All Elasticsearch interaction logic belongs here.
- `ElasticHelpers.SysAdmin.Cmd/` — console app (`OutputType=Exe`). References Core. Should stay thin: argument parsing, wiring, output formatting.

Both projects target **`net10.0`** with `ImplicitUsings` and `Nullable` enabled.

## Common commands

Run from the solution root (`E:\PROJEKTI\ElasticHelpers\SysAdmin`):

```powershell
# Build entire solution
dotnet build ElasticHelpers.SysAdmin.slnx

# Run the CLI (args after --)
dotnet run --project ElasticHelpers.SysAdmin.Cmd -- <args>

# Add a NuGet package to a specific project
dotnet add ElasticHelpers.SysAdmin.Core package <PackageName>

# Restore only (after editing csproj/slnx by hand)
dotnet restore ElasticHelpers.SysAdmin.slnx
```

## Dependency injection

All DI uses **`Microsoft.Extensions.DependencyInjection`**. The container is built once in `Program.cs` (the composition root) and handed to Spectre via `TypeRegistrar`.

- **Core** references only `Microsoft.Extensions.DependencyInjection.Abstractions` (the lightweight interfaces package). Each feature area in Core exposes a `static ServiceCollectionExtensions` class with an `AddXxx(this IServiceCollection)` extension method that registers its own services. Core never instantiates `ServiceCollection` itself.
- **Cmd** references the full `Microsoft.Extensions.DependencyInjection` package, creates the `ServiceCollection`, calls the Core extension methods, then passes the registrar to `CommandApp`.

## CLI framework

The Cmd project uses **[Spectre.Console.Cli](https://spectreconsole.net/cli/getting-started)** for all command parsing. Every CLI verb is a `Command<TSettings>` (or `AsyncCommand<TSettings>`), with parameters and options declared as properties on a `CommandSettings` subclass using `[CommandArgument]` and `[CommandOption]` attributes. Register commands in `Program.cs` via `CommandApp`. Do not use `System.CommandLine` or manual `args[]` parsing.

## Secrets management

`appsettings.json` contains placeholder values for `Elasticsearch:Password` and `Elasticsearch:ApiKey`. **Never populate these fields in the file and never commit credentials to git.**

Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) for local development:

```powershell
# One-time setup (adds <UserSecretsId> to the Cmd csproj)
dotnet user-secrets init --project ElasticHelpers.SysAdmin.Cmd

# Store a secret
dotnet user-secrets set "Elasticsearch:ApiKey" "<your-key>" --project ElasticHelpers.SysAdmin.Cmd
dotnet user-secrets set "Elasticsearch:Password" "<your-password>" --project ElasticHelpers.SysAdmin.Cmd

# List stored secrets
dotnet user-secrets list --project ElasticHelpers.SysAdmin.Cmd
```

User secrets are stored outside the repository at `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` and are never written to disk inside the project folder. Environment variables (e.g. `Elasticsearch__ApiKey`) can be used in CI/CD instead.

No test project exists yet. When one is added, register it in `ElasticHelpers.SysAdmin.slnx` by adding another `<Project Path="..." />` line — `dotnet sln add` does not yet write to slnx the same way it does for sln.
