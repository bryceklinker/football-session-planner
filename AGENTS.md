# AGENTS.md

This file provides guidance to AI coding agents when working with code in this repository.

## What This Is

A responsive web application for planning, viewing, and running football (soccer) coaching sessions.

## Tech Stack

- **Frontend:** Blazor WebAssembly (.NET 10)
- **Backend:** Azure Functions v4 isolated worker (C# .NET 10)
- **Database:** Azure SQL Serverless + EF Core
- **Auth:** Auth0 (JWT middleware on all Functions)
- **Hosting:** Azure Static Web Apps
- **CQRS:** MediatR with FluentValidation

## Solution Structure

```
src/
  FootballPlanner.Domain          # Entities — no external dependencies
  FootballPlanner.Application     # MediatR handlers, commands, queries, validators
  FootballPlanner.Infrastructure  # EF Core DbContext, migrations
  FootballPlanner.Api             # Azure Functions HTTP triggers
  FootballPlanner.Web             # Blazor WebAssembly
tests/
  FootballPlanner.Unit.Tests      # InMemory EF Core, real DI
  FootballPlanner.Integration.Tests  # Testcontainers SQL Server
  FootballPlanner.Feature.Tests   # Playwright e2e against Docker stack
```

## Build and Test Commands

```bash
dotnet build FootballPlanner.slnx
dotnet test tests/FootballPlanner.Unit.Tests
dotnet test tests/FootballPlanner.Integration.Tests
dotnet test tests/FootballPlanner.Feature.Tests
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~MyTest"
```

## Architecture Conventions

### CQRS with MediatR
- Every operation is a Command (mutates) or Query (reads)
- Commands and queries are `record` types implementing `IRequest<T>`
- Each has a dedicated handler and FluentValidation validator
- Azure Functions are thin HTTP triggers only — deserialise, `mediator.Send()`, return result
- No repository layer — handlers use `AppDbContext` directly

### Code Organization
- Code is organized by **feature**, not by type
- Each feature (Activity, Phase, Focus, Session, SessionActivity) has its own directory in `Application/`
- Shared code lives in `Application/Common/` (e.g., `Common/Behaviours/`)

### Testing
- **No mocking** — use real implementations throughout
- Unit tests: `TestServiceProvider.CreateMediator()` with InMemory EF Core
- Integration tests: `TestApplication` fixture with real SQL Server via Testcontainers
- Feature tests: Playwright against running Docker Compose stack
- All tests call `mediator.Send()` — never call handlers directly
- Use xUnit `Assert.*` — do NOT use FluentAssertions

### Domain Layer
- Entities have private setters and private constructors
- Constructed via static `Create(...)` factory methods

## Local Development

```bash
cp .env.example .env
# Edit .env with your Auth0 credentials and DB password
docker compose up --build
```

App runs at http://localhost:4280
