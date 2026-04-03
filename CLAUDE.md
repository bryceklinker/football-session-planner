# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A responsive web application for planning, viewing, and running football (soccer) coaching sessions. Desktop for editing, mobile-optimised read-only view for use on the pitch.

Full design spec: `docs/superpowers/specs/2026-03-28-football-session-planner-design.md`
Implementation plan: `docs/superpowers/plans/2026-03-28-foundation-reference-data.md`

---

## Tech Stack

- **Frontend:** Blazor WebAssembly
- **Backend:** Azure Functions v4 (isolated worker, C# .NET 10)
- **Database:** Azure SQL Serverless + EF Core
- **Auth:** Auth0 (JWT middleware on all Functions)
- **Hosting:** Azure Static Web Apps (Blazor WASM + Azure Functions in one resource)
- **Infrastructure:** Pulumi (C#) in `infra/`
- **CQRS:** MediatR
- **Validation:** FluentValidation

Always use the latest stable versions of all libraries and NuGet packages.

---

## Solution Structure

```
src/
  FootballPlanner.Domain          # Entities only — no external dependencies
  FootballPlanner.Application     # MediatR handlers, commands, queries, FluentValidation validators
  FootballPlanner.Infrastructure  # EF Core DbContext, migrations, SQL config
  FootballPlanner.Api             # Azure Functions — thin HTTP triggers → MediatR
  FootballPlanner.Web             # Blazor WebAssembly
infra/                            # Pulumi C# infrastructure
tests/
  FootballPlanner.Unit.Tests
  FootballPlanner.Integration.Tests
  FootballPlanner.Feature.Tests
```

---

## Build and Test Commands

```bash
# Build entire solution
dotnet build FootballPlanner.slnx

# Run unit tests
dotnet test tests/FootballPlanner.Unit.Tests

# Run integration tests (requires Docker for Testcontainers)
dotnet test tests/FootballPlanner.Integration.Tests

# Run feature tests (requires a running app instance)
dotnet test tests/FootballPlanner.Feature.Tests

# Run a single test by name
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~CreatePhaseCommandTests"

# Add EF Core migration
dotnet ef migrations add <MigrationName> \
  --project src/FootballPlanner.Infrastructure \
  --startup-project src/FootballPlanner.Infrastructure

# Apply migrations to local database
dotnet ef database update \
  --project src/FootballPlanner.Infrastructure \
  --startup-project src/FootballPlanner.Infrastructure
```

---

## Architecture Conventions

### CQRS with MediatR
- Every operation is a **Command** (mutates state) or **Query** (reads state)
- Commands and queries are `record` types implementing `IRequest<T>`
- Each has a dedicated handler class and a FluentValidation validator
- Handlers use primary constructor injection: `public class Handler(AppDbContext db) : IRequestHandler<Command, Result>`
- Azure Functions are **thin triggers only** — deserialise request, `mediator.Send(command)`, return result
- No repository layer — handlers use `AppDbContext` directly

### Validation
- Every Command has a corresponding `AbstractValidator<TCommand>`
- Validators live in `FootballPlanner.Application` alongside their commands
- Validation runs automatically via `ValidationBehaviour` pipeline before the handler executes

### Domain Layer
- Entities have private setters and a private constructor
- Always constructed via a static `Create(...)` factory method
- No dependencies on other layers

### Infrastructure
- `AddInfrastructure(IConfiguration, Action<DbContextOptionsBuilder>? configureDb = null)` — the optional `configureDb` lets tests substitute InMemory EF Core without changing the extension method

### Code Organization
- Code is organized by **feature**, not by type. Each feature (Activity, Phase, Focus, Session, SessionActivity) has its own directory in `Application/`, containing its commands, queries, handlers, and validators together.
- Code shared across multiple features lives in `Application/Common/` organized by functionality (e.g., `Common/Behaviours/`).

### Logging
- All MediatR requests are logged via `LoggingBehaviour` (in `Application/Common/Behaviours/`): logs request name + parameters at start, duration at completion, and any exceptions with full context.

---

## Testing Conventions

- **No mocking anywhere.** Use real implementations throughout all test levels.
- **Unit tests** (`FootballPlanner.Unit.Tests`) — use `TestServiceProvider.CreateMediator()` which sets up real DI via `AddApplication()` + `AddInfrastructure()` with an InMemory database. Never instantiate handlers or validators directly.
- **Integration tests** (`FootballPlanner.Integration.Tests`) — use `TestApplication` (implements `IAsyncLifetime`, used as `IClassFixture<TestApplication>`). Starts a SQL Server container via Testcontainers, runs real migrations, exposes `IMediator Mediator`.
- **Feature tests** (`FootballPlanner.Feature.Tests`) — Playwright end-to-end tests against the full running Docker Compose stack (`docker compose up`). Cover key user flows end-to-end.
- All tests call `mediator.Send(command)` — never call handlers directly.
- Do **NOT** use FluentAssertions (licensing change). Use standard xUnit `Assert.*` methods only.
- All new features and bug fixes are test-driven.

---

## .NET and SDK

- Use `.NET 10` (net10.0) for all projects.
- SDK version is pinned in `global.json` at the repo root — do not hard-code versions elsewhere.
- Use `.slnx` solution format (not `.sln`).
- Blazor components: all C# inline in `@code {}` blocks — no code-behind `.razor.cs` files.

---

## Infrastructure

- Pulumi state is stored in Azure Blob Storage: `azblob://pulumi-state?storage_account=pulumistatestore`
- Stack is created per environment. Current environment: `prod`
- Resource names are always environment-scoped via the `Naming` utility: `naming.Resource("base-name")` → `"base-name-prod"`
- Azure region is controlled by the `AZURE_LOCATION` env var. The default (`centralus`) is defined **once** in `FootballPlannerStack.cs` — do not duplicate it anywhere else.
- The SWA deployment token is exposed as a Pulumi stack output (`StaticWebAppDeployToken`) and read by the deploy workflow — no manual secret needed.

## GitHub Actions

- `.NET` version is sourced from `global.json` — all `setup-dotnet` steps use `global-json-file: global.json`.
- CI (`ci.yml`): build + unit + integration + feature tests, then Pulumi preview (on main/PRs).
- Deploy (`deploy.yml`): auto-triggers when CI succeeds on main; requires manual approval via `production` environment gate.
- Both workflows set `AZURE_LOCATION: centralus` and call the shared `.github/actions/ensure-pulumi-state` composite action before any Pulumi commands.
- Required secrets: `ARM_CLIENT_ID`, `ARM_CLIENT_SECRET`, `ARM_TENANT_ID`, `ARM_SUBSCRIPTION_ID`.

---

## Custom Commands

- `/add-project` — scaffold a new .NET project in the solution
- `/add-domain-entity` — scaffold a new domain entity with CQRS commands/queries and EF Core config
- `/add-migration` — add a new EF Core database migration
