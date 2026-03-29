# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Is

A responsive web application for planning, viewing, and running football (soccer) coaching sessions. Desktop for editing, mobile-optimised read-only view for use on the pitch.

Full design spec: `docs/superpowers/specs/2026-03-28-football-session-planner-design.md`

---

## Tech Stack

- **Frontend:** Blazor WebAssembly
- **Backend:** Azure Functions (consumption plan, C# .NET)
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

## Architecture Conventions

### CQRS with MediatR
- Every operation is a **Command** (mutates state) or **Query** (reads state)
- Each has a dedicated handler class and a FluentValidation validator
- Azure Functions are **thin triggers only** — one line: `mediator.Send(command)` and return the result
- No repository layer — handlers use `DbContext` directly

### Validation
- Every Command has a corresponding FluentValidation validator
- Validators live in `FootballPlanner.Application` alongside their commands
- Validation runs via a MediatR pipeline behaviour before the handler executes

### Domain Layer
- Entities live in `FootballPlanner.Domain` with no dependencies on other projects
- No business logic in handlers — push behaviour into domain entities where appropriate

---

## Testing Conventions

- **No mocking anywhere.** Use real implementations throughout all test levels.
- **Unit tests** (`FootballPlanner.Unit.Tests`) — use EF Core InMemory provider. Test handlers and validators in isolation.
- **Integration tests** (`FootballPlanner.Integration.Tests`) — use Testcontainers to spin up a real SQL Server. Test full API request/response including migrations and constraints.
- **Feature tests** (`FootballPlanner.Feature.Tests`) — Playwright end-to-end tests against a running instance. Cover key user flows.
- All new features and bug fixes are test-driven.

---

## Custom Commands

- `/add-project` — scaffold a new .NET project in the solution
- `/add-domain-entity` — scaffold a new domain entity with CQRS commands/queries and EF Core config
- `/add-migration` — add a new EF Core database migration
