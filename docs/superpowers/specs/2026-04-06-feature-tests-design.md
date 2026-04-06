# Feature Tests Design

## Goal

Add Playwright end-to-end feature tests that verify key user planning journeys against the full running Docker Compose stack.

## Architecture

Tests run Playwright (headless Chromium) against the Docker Compose stack (`docker compose up`). Tests assume the stack is already running — they do not start or stop containers. CI starts the stack before the test run and tears it down after.

Base URL: `http://localhost:4280` (SWA container).

Auth is handled via real Auth0 login. A `FeatureTestFixture` authenticates once and saves browser storage state. Individual tests reuse that saved state — no repeated logins.

## Journey Pattern

Journey classes are stateless helpers that receive an `IPage` and know how to drive the browser through a set of related actions. They contain no tests. All `[Fact]` methods live in test classes that compose journeys into user stories.

## File Structure

```
tests/FootballPlanner.Feature.Tests/
  Infrastructure/
    FeatureTestFixture.cs       — IAsyncLifetime, starts Playwright, authenticates once, exposes IBrowserContext
    AuthJourney.cs              — logs in via Auth0 UI, saves storage state path
  Journeys/
    PhaseJourney.cs             — CreatePhase(name, order)
    FocusJourney.cs             — CreateFocus(name)
    ActivityJourney.cs          — CreateActivity(name, description, duration)
    SessionJourney.cs           — CreateSession(title, date), NavigateToEditor(title)
    SessionEditorJourney.cs     — AddActivity(activityName, phase, focus, duration)
  Tests/
    PlanningJourneyTests.cs     — three [Fact] methods composing the journeys
```

## FeatureTestFixture

`FeatureTestFixture` implements `IAsyncLifetime` and is shared via `IClassFixture<FeatureTestFixture>`. On startup it:
1. Launches Playwright and a Chromium browser instance
2. Creates a browser context, logs in via Auth0 (using `AuthJourney`), and saves the storage state to a temp file
3. Disposes the setup context

Each `[Fact]` calls `fixture.NewPage()` which creates a fresh `IBrowserContext` pre-loaded with the saved auth storage state, then returns a new `IPage` from that context. This gives each test isolated navigation state while sharing the authenticated session.

## Test Scenarios

The three facts are independent — each creates all the data it needs. There is no assumed execution order.

**Fact 1 — CanSetUpReferenceData**
Creates a phase ("Warm Up", order 1) and a focus ("Technique"). Asserts both appear in their respective lists.

**Fact 2 — CanBuildActivityLibrary**
Creates a phase and focus (prerequisites), then creates an activity ("Rondo", "A possession drill", 10 min). Asserts it appears in the activity list.

**Fact 3 — CanPlanASession**
Creates a phase, focus, and activity (prerequisites). Creates a session ("Tuesday Training", today's date). Navigates to the session editor. Adds "Rondo" with "Warm Up" phase, "Technique" focus, 10 min duration. Asserts the activity appears in the session.

## Auth0 Setup

### Auth0 tenant configuration
1. Create a test user in your Auth0 tenant (email + password)
2. Note the Auth0 domain and the Client ID of the SWA application (already present in your docker-compose `.env`)

### Local environment
Create a `.env.test` file (gitignored, separate from `.env` which is the Docker Compose env file):
```
AUTH0_TEST_USER_EMAIL=...
AUTH0_TEST_USER_PASSWORD=...
```

`FeatureTestFixture` reads these via `Environment.GetEnvironmentVariable`. For local runs, export the variables in your shell before running `dotnet test`, or use `dotenv` tooling to load `.env.test` automatically.

A `.env.test.example` file documents the required variables.

### GitHub secrets
Add to repository secrets:
- `AUTH0_TEST_USER_EMAIL`
- `AUTH0_TEST_USER_PASSWORD`

## CI Integration

```yaml
- name: Build feature tests
  run: dotnet build tests/FootballPlanner.Feature.Tests -c Release

- name: Install Playwright browsers
  run: pwsh tests/FootballPlanner.Feature.Tests/bin/Release/net10.0/playwright.ps1 install chromium

- name: Start Docker Compose stack
  run: |
    docker compose up -d
    curl --retry 15 --retry-delay 3 --retry-connrefused -s http://localhost:4280/ > /dev/null

- name: Run feature tests
  env:
    AUTH0_TEST_USER_EMAIL: ${{ secrets.AUTH0_TEST_USER_EMAIL }}
    AUTH0_TEST_USER_PASSWORD: ${{ secrets.AUTH0_TEST_USER_PASSWORD }}
  run: dotnet test tests/FootballPlanner.Feature.Tests

- name: Stop Docker Compose stack
  if: always()
  run: docker compose down
```

The `curl --retry` poll replaces `--wait` since the SWA container has no healthcheck. It retries up to 15 times (45 seconds) until port 4280 responds.

## Out of Scope

Session running/timer flows — to be added in a future iteration once that feature is built.
