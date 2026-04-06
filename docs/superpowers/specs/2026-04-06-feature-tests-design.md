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
    FeatureTestFixture.cs       — IAsyncLifetime, starts Playwright, authenticates once, exposes IPage
    AuthJourney.cs              — logs in via Auth0 UI, saves storage state
  Journeys/
    PhaseJourney.cs             — CreatePhase(name, order)
    FocusJourney.cs             — CreateFocus(name)
    ActivityJourney.cs          — CreateActivity(name, description, duration)
    SessionJourney.cs           — CreateSession(title, date), NavigateToEditor(title)
    SessionEditorJourney.cs     — AddActivity(activityName, phase, focus, duration)
  Tests/
    PlanningJourneyTests.cs     — three [Fact] methods composing the journeys
```

## Test Scenarios

All three facts share a `IClassFixture<FeatureTestFixture>` so database state from earlier tests is visible to later ones.

**Fact 1 — CanSetUpReferenceData**
Creates a phase ("Warm Up", order 1) and a focus ("Technique"). Asserts both appear in their respective lists.

**Fact 2 — CanBuildActivityLibrary**
Creates an activity ("Rondo", "A possession drill", 10 min). Asserts it appears in the activity list.

**Fact 3 — CanPlanASession**
Creates a session ("Tuesday Training", today's date). Navigates to the session editor. Adds "Rondo" with "Warm Up" phase, "Technique" focus, 10 min duration. Asserts the activity appears in the session.

## Auth0 Setup

### Auth0 tenant configuration
1. Create a test user in your Auth0 tenant (email + password)
2. Note the Auth0 domain and the Client ID of the SWA application

### Local environment
Add to a `.env.test` file (gitignored):
```
AUTH0_TEST_USER_EMAIL=...
AUTH0_TEST_USER_PASSWORD=...
```
The docker-compose Auth0 domain and audience are already in `.env`.

### GitHub secrets
Add to repository secrets:
- `AUTH0_TEST_USER_EMAIL`
- `AUTH0_TEST_USER_PASSWORD`

The CI workflow reads these into environment variables before running the feature tests.

## CI Integration

```yaml
- name: Start Docker Compose stack
  run: docker compose up -d --wait

- name: Run feature tests
  env:
    AUTH0_TEST_USER_EMAIL: ${{ secrets.AUTH0_TEST_USER_EMAIL }}
    AUTH0_TEST_USER_PASSWORD: ${{ secrets.AUTH0_TEST_USER_PASSWORD }}
  run: dotnet test tests/FootballPlanner.Feature.Tests

- name: Stop Docker Compose stack
  if: always()
  run: docker compose down
```

## Out of Scope

Session running/timer flows — to be added in a future iteration once that feature is built.
