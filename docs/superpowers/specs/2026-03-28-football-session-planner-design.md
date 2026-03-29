# Football Session Planner — Design Spec

**Date:** 2026-03-28

## Overview

A responsive web application for planning, viewing, and running football (soccer) coaching sessions. Full editing on desktop, read-only session running view on mobile. Cloud-backed with a small user base (3–4 users).

---

## Domain Model

### Managed Reference Tables

**Phase** — the structural stage of a session activity
- Id, Name, DisplayOrder
- Managed from the desktop app (add/rename/remove)
- Examples: Warm Up, Small Sided, Increased Numbers, Scrimmage

**Focus** — the coaching focus of a session activity
- Id, Name, DisplayOrder
- Managed from the desktop app (add/rename/remove)
- Examples: Technique, Pressing, Possession, Defending as a Group, Defending as an Individual, Transitioning, Attacking as a Group, Attacking as an Individual

### Core Entities

**Activity** (library item, reusable across sessions)
- Name, Description
- InspirationUrl (optional link to external source)
- EstimatedDuration (minutes)
- DiagramJson (pitch diagram stored as serialized JSON)
- CreatedAt, UpdatedAt

**Session**
- Date, Title, Notes
- Ordered collection of SessionActivities

**SessionActivity** (an activity placed into a specific session)
- ActivityId → Activity (library reference)
- PhaseId → Phase
- FocusId → Focus
- Duration (minutes — defaults from Activity.EstimatedDuration, overridable per session)
- DisplayOrder
- Notes (free text — pre-planning thoughts and post-session observations)
- SessionActivityKeyPoints — ordered list of coaching instruction strings (child table)

---

## Tech Stack

| Concern | Choice | Reason |
|---------|--------|--------|
| Frontend | Blazor WebAssembly | Single C# codebase, shared models with API, consistent structure |
| Backend | Azure Functions (consumption) | No always-on server cost; infrequent use pattern |
| Database | Azure SQL Serverless + EF Core | Relational model fits the data, auto-pauses on idle, EF Core native |
| Auth | Auth0 | Handles 3–4 users on free tier, well-supported with Blazor |
| Hosting | Azure Static Web Apps | Hosts Blazor WASM + Azure Functions in one resource |
| Infrastructure | Pulumi (C#) | Stays in the .NET ecosystem |
| CQRS | MediatR | Preferred pattern for structure and consistency |
| Validation | FluentValidation | Consistent validation across all commands |

Always use the latest stable versions of all libraries and NuGet packages.

---

## Solution Structure

```
src/
  FootballPlanner.Domain          # Entities, value objects — no external dependencies
  FootballPlanner.Application     # MediatR handlers, commands, queries, FluentValidation validators
  FootballPlanner.Infrastructure  # EF Core DbContext, migrations, SQL configuration
  FootballPlanner.Api             # Azure Functions — thin HTTP triggers delegating to MediatR
  FootballPlanner.Web             # Blazor WebAssembly frontend
infra/                            # Pulumi C# infrastructure
tests/
  FootballPlanner.Unit.Tests      # Handlers and validators using EF Core InMemory, no mocks
  FootballPlanner.Integration.Tests # Full API tests using Testcontainers (real SQL Server)
  FootballPlanner.Feature.Tests   # Playwright end-to-end tests for key user flows
```

---

## Architecture

### API Layer
Azure Functions are thin HTTP triggers. Each function does exactly one thing: call `mediator.Send(command/query)` and return the result. Auth0 JWT validation runs as middleware before any handler executes.

### Application Layer
Every operation is a Command or Query with a dedicated MediatR handler and a FluentValidation validator. Handlers use the EF Core `DbContext` directly — no repository layer (CQRS makes repositories redundant indirection).

### Infrastructure
- Azure Static Web Apps bundles Blazor WASM and Azure Functions together, simplifying CORS and Auth0 token routing
- Pulumi provisions: Resource Group, Static Web App, Azure SQL Server + Serverless Database, Key Vault for secrets
- Azure SQL Serverless auto-pauses after configurable idle time; first request after pause has ~30s cold start (acceptable for a planning tool)

### Testing
- **No mocking anywhere.** Use real implementations throughout.
- **Unit tests** — EF Core InMemory provider. Tests handlers and validators in isolation without hitting SQL Server.
- **Integration tests** — Testcontainers spins up a real SQL Server container. Tests full API request/response including migrations and constraints.
- **Feature tests** — Playwright tests run against a live instance, covering key flows: create activity, build session, run session on mobile viewport.

---

## Pitch Diagram Builder

Built as a Blazor component using SVG. Diagrams are serialized to JSON stored in `Activity.DiagramJson` using percentage-based coordinates so they scale correctly on any screen size.

### Pitch Sizes

| Format | Full Dimensions | Half Dimensions |
|--------|----------------|-----------------|
| 11v11 | 100m × 64m | 50m × 64m |
| 9v9 | 80m × 50m | 40m × 50m |
| 7v7 | 60m × 40m | 30m × 40m |
| Small-sided | Custom w × h | — |

The canvas preserves the correct aspect ratio at all screen sizes.

### Placeable Elements
- **Player tokens** — attacking team, defending team, neutral/GK. Draggable.
- **Cones / markers** — draggable.
- **Goals** — draggable and resizable. Default size matches the standard for the selected format; freely resizable for small-sided and non-standard setups.
- **Movement arrows** — click start point → click end point. Arrow styles: run, pass, dribble.

### Mobile
Diagram renders as a static read-only SVG scaled to the mobile viewport. No editing on mobile.

---

## Session Running Mode (Mobile)

A mobile-optimised read-only view for use on the pitch during a session.

**Per activity display:**
- Activity name, phase, and focus
- Key points (bulleted list)
- Pitch diagram (static SVG)
- Count-up timer showing elapsed time alongside estimated duration (e.g., `2:34 / 15:00`)

**Navigation:**
- Start/pause the timer
- Next / Previous buttons to move between activities
- Progress indicator (e.g., Activity 2 of 5)
- Visual alert when elapsed time reaches estimated duration

**Not available on mobile:**
- No editing of any kind
- Notes are read-only (post-session notes are added on desktop after the session)

---

## Desktop Views

- **Activity Library** — create, edit, delete activities and their pitch diagrams
- **Session Planner** — create/edit sessions, add activities from library, set phase/focus/duration/key points per session activity
- **Post-session** — add/edit notes on each session activity after running the session
- **Reference Data Management** — add/rename/remove Phase and Focus entries
