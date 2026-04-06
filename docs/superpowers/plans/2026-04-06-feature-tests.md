# Feature Tests Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Playwright end-to-end feature tests covering key user planning journeys against the full Docker Compose stack.

**Architecture:** A `FeatureTestFixture` (IClassFixture) launches Playwright, authenticates once via Auth0, and saves browser storage state to a temp file. Each test calls `fixture.NewPageAsync()` which creates a fresh `IBrowserContext` loaded with that auth state and returns a new `IPage` — giving each test isolated navigation state without re-authenticating. Journey classes (`PhaseJourney`, `FocusJourney`, `ActivityJourney`, `SessionJourney`, `SessionEditorJourney`) are stateless helpers that receive an `IPage` and drive the browser. Each journey method accepts an input record (e.g. `CreateActivityInput`) defined in the same file — making it easy to add fields later without changing method signatures. Three independent `[Fact]` tests in `PlanningJourneyTests` each create their own prerequisites and compose journeys into user stories. CI modifies the existing `build-and-test` job to start the Docker Compose stack before running feature tests and tear it down after.

**Tech Stack:** .NET 10, Microsoft.Playwright 1.58.0, xUnit 2.9.3, Docker Compose, Auth0

---

## File Map

**New files:**
- `tests/FootballPlanner.Feature.Tests/Infrastructure/FeatureTestFixture.cs` — IAsyncLifetime, Playwright lifecycle, auth storage state; `NewPageAsync()` creates a fresh page and binds all journey properties; exposes `Page`, `PhaseJourney`, `FocusJourney`, `ActivityJourney`, `SessionJourney`, `SessionEditorJourney`
- `tests/FootballPlanner.Feature.Tests/Infrastructure/AuthJourney.cs` — logs in via Auth0 UI, reads credentials from env vars
- `tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs` — `CreatePhaseInput` record + `CreatePhaseAsync(CreatePhaseInput)`
- `tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs` — `CreateFocusInput` record + `CreateFocusAsync(CreateFocusInput)`
- `tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs` — `CreateActivityInput` record + `CreateActivityAsync(CreateActivityInput)`
- `tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs` — `CreateSessionInput` record + `CreateSessionAsync(CreateSessionInput)`, `NavigateToEditorAsync(string title)`
- `tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs` — `AddActivityInput` record + `AddActivityAsync(AddActivityInput)`
- `tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs` — three independent [Fact] planning journey tests
- `.env.test.example` — documents Auth0 test credentials env vars (separate from .env which is the Docker Compose file)

**Modified files:**
- `.github/workflows/ci.yml` — install Playwright browsers, create .env, start/stop Docker Compose, pass Auth0 secrets to feature test step

---

## Chunk 1: Infrastructure

### Task 1: FeatureTestFixture, AuthJourney, and env example

**Files:**
- Create: `tests/FootballPlanner.Feature.Tests/Infrastructure/FeatureTestFixture.cs`
- Create: `tests/FootballPlanner.Feature.Tests/Infrastructure/AuthJourney.cs`
- Create: `.env.test.example`

- [ ] **Step 1: Create .env.test.example**

Create `.env.test.example` at the repo root:

```
# Auth0 test user credentials for Playwright feature tests
# These are separate from .env (which configures the Docker Compose stack)
# Copy this file to .env.test (gitignored) and fill in your values
# Export as environment variables before running dotnet test locally:
#   export AUTH0_TEST_USER_EMAIL=...
#   export AUTH0_TEST_USER_PASSWORD=...

AUTH0_TEST_USER_EMAIL=your-test-user@example.com
AUTH0_TEST_USER_PASSWORD=your-test-user-password
```

Add `.env.test` to `.gitignore` if not already present:
```bash
echo ".env.test" >> .gitignore
```

- [ ] **Step 2: Create FeatureTestFixture**

Create `tests/FootballPlanner.Feature.Tests/Infrastructure/FeatureTestFixture.cs`:

```csharp
using FootballPlanner.Feature.Tests.Journeys;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Infrastructure;

public class FeatureTestFixture : IAsyncLifetime
{
    public const string BaseUrl = "http://localhost:4280";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _storageStatePath;
    private readonly List<IBrowserContext> _contexts = [];

    // Set by NewPageAsync() — available for direct assertions in tests
    public IPage Page { get; private set; } = null!;

    // Journey properties — rebound to the fresh page on each NewPageAsync() call
    public PhaseJourney PhaseJourney { get; private set; } = null!;
    public FocusJourney FocusJourney { get; private set; } = null!;
    public ActivityJourney ActivityJourney { get; private set; } = null!;
    public SessionJourney SessionJourney { get; private set; } = null!;
    public SessionEditorJourney SessionEditorJourney { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });

        _storageStatePath = Path.Combine(Path.GetTempPath(), $"auth-state-{Guid.NewGuid()}.json");

        var setupContext = await _browser.NewContextAsync();
        try
        {
            var page = await setupContext.NewPageAsync();
            await new AuthJourney(page, BaseUrl).LoginAsync();
            await setupContext.StorageStateAsync(new() { Path = _storageStatePath });
        }
        finally
        {
            await setupContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a fresh authenticated browser context and page for the current test,
    /// then rebinds all journey properties to that page.
    /// Call once at the start of each [Fact].
    /// </summary>
    public async Task NewPageAsync()
    {
        var context = await _browser!.NewContextAsync(new() { StorageStatePath = _storageStatePath });
        _contexts.Add(context);
        Page = await context.NewPageAsync();
        PhaseJourney = new PhaseJourney(Page);
        FocusJourney = new FocusJourney(Page);
        ActivityJourney = new ActivityJourney(Page);
        SessionJourney = new SessionJourney(Page);
        SessionEditorJourney = new SessionEditorJourney(Page);
    }

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();

        if (_storageStatePath != null && File.Exists(_storageStatePath))
            File.Delete(_storageStatePath);

        if (_browser != null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();
    }
}
```

- [ ] **Step 3: Create AuthJourney**

Create `tests/FootballPlanner.Feature.Tests/Infrastructure/AuthJourney.cs`:

```csharp
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Infrastructure;

public class AuthJourney(IPage page, string baseUrl)
{
    public async Task LoginAsync()
    {
        var email = Environment.GetEnvironmentVariable("AUTH0_TEST_USER_EMAIL")
            ?? throw new InvalidOperationException(
                "AUTH0_TEST_USER_EMAIL is not set. See .env.test.example for setup instructions.");

        var password = Environment.GetEnvironmentVariable("AUTH0_TEST_USER_PASSWORD")
            ?? throw new InvalidOperationException(
                "AUTH0_TEST_USER_PASSWORD is not set. See .env.test.example for setup instructions.");

        await page.GotoAsync(baseUrl);

        // Wait for Auth0 redirect or the app (if storage state was somehow already valid)
        await page.WaitForURLAsync(
            url => url.Contains("auth0.com") || url.StartsWith(baseUrl),
            new() { Timeout = 30_000 });

        if (!page.Url.Contains("auth0.com"))
            return;

        // Auth0 Universal Login: enter email, then password (two-step flow)
        // If your tenant uses Classic Login (single form), replace with:
        //   await page.Locator("input[type='email']").FillAsync(email);
        //   await page.Locator("input[type='password']").FillAsync(password);
        //   await page.Locator("button[type='submit']").ClickAsync();
        await page.Locator("input[name='username']").FillAsync(email);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
        await page.Locator("input[name='password']").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();

        await page.WaitForURLAsync(url => url.StartsWith(baseUrl), new() { Timeout = 30_000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build tests/FootballPlanner.Feature.Tests -c Release 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add tests/FootballPlanner.Feature.Tests/Infrastructure/ .env.test.example .gitignore
git commit -m "feat: add FeatureTestFixture and AuthJourney"
```

---

## Chunk 2: Journey Classes

### Task 2: Phase, Focus, and Activity journeys

**Files:**
- Create: `tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs`
- Create: `tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs`
- Create: `tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs`

Each file defines an input record alongside the journey class. The record holds all arguments the method needs — adding a new field later only requires updating the record and the call sites, not the method signature.

Selectors are derived from the placeholder text in the Blazor razor components.

- [ ] **Step 1: Create PhaseJourney**

Create `tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs`:

```csharp
using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreatePhaseInput(string Name, int Order);

public class PhaseJourney(IPage page)
{
    public async Task CreatePhaseAsync(CreatePhaseInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/phases");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByPlaceholder("Phase name").FillAsync(input.Name);
        await page.GetByPlaceholder("Order").FillAsync(input.Order.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 2: Create FocusJourney**

Create `tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs`:

```csharp
using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreateFocusInput(string Name);

public class FocusJourney(IPage page)
{
    public async Task CreateFocusAsync(CreateFocusInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/focuses");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByPlaceholder("Focus name").FillAsync(input.Name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 3: Create ActivityJourney**

Create `tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs`:

```csharp
using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreateActivityInput(string Name, string Description, int EstimatedDurationMinutes);

public class ActivityJourney(IPage page)
{
    public async Task CreateActivityAsync(CreateActivityInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/activities");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByPlaceholder("Name").FillAsync(input.Name);
        await page.GetByPlaceholder("Description").FillAsync(input.Description);
        await page.GetByPlaceholder("Duration (mins)").FillAsync(input.EstimatedDurationMinutes.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build tests/FootballPlanner.Feature.Tests -c Release 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs \
        tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs \
        tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs
git commit -m "feat: add Phase, Focus, and Activity journey classes"
```

### Task 3: Session and SessionEditor journeys

**Files:**
- Create: `tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs`
- Create: `tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs`

`SessionJourney.CreateSessionAsync` takes a `CreateSessionInput`. `NavigateToEditorAsync` takes a plain `string title` — wrapping a single string in a record adds no extensibility benefit.

`AddActivityInput` separates `ActivityEstimatedDuration` (used to match the dropdown label `"Rondo (10 min)"`) from `SessionDuration` (the duration field on the session activity). These may differ — e.g. estimated 30 min but scheduled for 15 min in this session.

- [ ] **Step 1: Create SessionJourney**

Create `tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs`:

```csharp
using FootballPlanner.Feature.Tests.Infrastructure;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

public record CreateSessionInput(string Title, DateTime Date);

public class SessionJourney(IPage page)
{
    public async Task CreateSessionAsync(CreateSessionInput input)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/sessions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.Locator("input[type='date']").FillAsync(input.Date.ToString("yyyy-MM-dd"));
        await page.GetByPlaceholder("Title").FillAsync(input.Title);
        await page.GetByRole(AriaRole.Button, new() { Name = "Add Session" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task NavigateToEditorAsync(string title)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/sessions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var row = page.Locator("tr").Filter(new() { HasText = title });
        await row.GetByRole(AriaRole.Button, new() { Name = "Edit" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 2: Create SessionEditorJourney**

Create `tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs`:

```csharp
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

/// <param name="ActivityName">Name of the activity to select from the dropdown.</param>
/// <param name="ActivityEstimatedDuration">Estimated duration used when the activity was created — needed to match the dropdown label "{name} ({duration} min)".</param>
/// <param name="PhaseName">Phase to assign to this session activity.</param>
/// <param name="FocusName">Focus to assign to this session activity.</param>
/// <param name="SessionDuration">How long this activity will run in the session (may differ from estimated duration).</param>
public record AddActivityInput(
    string ActivityName,
    int ActivityEstimatedDuration,
    string PhaseName,
    string FocusName,
    int SessionDuration);

public class SessionEditorJourney(IPage page)
{
    public async Task AddActivityAsync(AddActivityInput input)
    {
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Activity dropdown option text is "{name} ({estimatedDuration} min)"
        await page.Locator("select")
            .Filter(new() { HasText = "-- Select Activity --" })
            .SelectOptionAsync(new SelectOptionValue { Label = $"{input.ActivityName} ({input.ActivityEstimatedDuration} min)" });

        await page.Locator("select")
            .Filter(new() { HasText = "-- Select Phase --" })
            .SelectOptionAsync(new SelectOptionValue { Label = input.PhaseName });

        await page.Locator("select")
            .Filter(new() { HasText = "-- Select Focus --" })
            .SelectOptionAsync(new SelectOptionValue { Label = input.FocusName });

        await page.GetByPlaceholder("Duration (mins)").FillAsync(input.SessionDuration.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Add Activity" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build tests/FootballPlanner.Feature.Tests -c Release 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit**

```bash
git add tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs \
        tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs
git commit -m "feat: add Session and SessionEditor journey classes"
```

---

## Chunk 3: Tests and CI

### Task 4: PlanningJourneyTests

**Files:**
- Create: `tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs`

**Prerequisite:** Docker Compose stack must be running and Auth0 credentials exported before running these tests locally:

```bash
docker compose up -d
curl --retry 15 --retry-delay 3 --retry-connrefused -s http://localhost:4280/ > /dev/null
export AUTH0_TEST_USER_EMAIL=your-test-user@example.com
export AUTH0_TEST_USER_PASSWORD=your-test-password
```

Each `[Fact]` is fully independent — it creates all the data it needs. xUnit does not guarantee execution order so tests must not share state.

- [ ] **Step 1: Create PlanningJourneyTests**

Create `tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs`:

```csharp
using FootballPlanner.Feature.Tests.Infrastructure;
using FootballPlanner.Feature.Tests.Journeys;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Tests;

public class PlanningJourneyTests(FeatureTestFixture fixture) : IClassFixture<FeatureTestFixture>
{
    [Fact]
    public async Task CanSetUpReferenceData()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/phases");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("Warm Up");

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/focuses");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("Technique");
    }

    [Fact]
    public async Task CanBuildActivityLibrary()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(new CreateActivityInput("Rondo", "A possession drill", 10));

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/activities");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("Rondo");
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("A possession drill");
        await Assertions.Expect(fixture.Page.Locator("table")).ToContainTextAsync("10 min");
    }

    [Fact]
    public async Task CanPlanASession()
    {
        await fixture.NewPageAsync();

        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(new CreateActivityInput("Rondo", "A possession drill", 10));

        await fixture.SessionJourney.CreateSessionAsync(new CreateSessionInput("Tuesday Training", DateTime.Today));
        await fixture.SessionJourney.NavigateToEditorAsync("Tuesday Training");

        await fixture.SessionEditorJourney.AddActivityAsync(new AddActivityInput(
            ActivityName: "Rondo",
            ActivityEstimatedDuration: 10,
            PhaseName: "Warm Up",
            FocusName: "Technique",
            SessionDuration: 10));

        await Assertions.Expect(fixture.Page.GetByText("Rondo")).ToBeVisibleAsync();
        // Exact = false because the activity div renders "— Warm Up / Technique — 10 min"
        // and GetByText with exact matching would not find a partial substring
        await Assertions.Expect(fixture.Page.GetByText("Warm Up / Technique", new() { Exact = false })).ToBeVisibleAsync();
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build tests/FootballPlanner.Feature.Tests -c Release 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Run against live stack**

Ensure Docker Compose is running and credentials are exported, then:

```bash
dotnet test tests/FootballPlanner.Feature.Tests -c Release 2>&1 | tail -20
```

Expected: `Passed! — 3 tests`

**If tests fail:**
- 401 errors or redirect loops → check Auth0 credentials and that your tenant uses Universal Login (two-step). If Classic Login, update `AuthJourney` — see the comment in that file.
- Element not found → Blazor may still be loading; increase `WaitForLoadStateAsync` timeout or add explicit `WaitForAsync` on a key element.
- Activity dropdown option not found → verify the activity was created with the exact estimated duration passed to `AddActivityInput.ActivityEstimatedDuration`.

- [ ] **Step 4: Commit**

```bash
git add tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs
git commit -m "feat: add PlanningJourneyTests"
```

### Task 5: CI integration

**Files:**
- Modify: `.github/workflows/ci.yml`

The existing `build-and-test` job already has a `Run feature tests` step but it's missing: Playwright browser installation, Docker Compose setup/teardown, and Auth0 credentials. This task adds those.

- [ ] **Step 1: Read current ci.yml**

Read `.github/workflows/ci.yml` to confirm the current structure before editing.

- [ ] **Step 2: Update ci.yml**

Replace the `build-and-test` job steps with the following (keeping the `pulumi-preview` job unchanged). The changes are: add `Install Playwright browsers` step, add `Create .env file` step, add `Start Docker Compose stack` step, update `Run feature tests` to pass secrets and use `--no-build`, add `Stop Docker Compose stack` step:

```yaml
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Restore dependencies
        run: dotnet restore FootballPlanner.slnx

      - name: Build
        run: dotnet build FootballPlanner.slnx --no-restore --configuration Release

      - name: Install Playwright browsers
        run: pwsh tests/FootballPlanner.Feature.Tests/bin/Release/net10.0/playwright.ps1 install chromium

      - name: Run unit tests
        run: dotnet test tests/FootballPlanner.Unit.Tests --no-build --configuration Release --logger trx

      - name: Run integration tests
        run: dotnet test tests/FootballPlanner.Integration.Tests --no-build --configuration Release --logger trx

      - name: Create .env for Docker Compose
        # Variable names match what docker-compose.yml expects:
        #   DB_PASSWORD      → SA_PASSWORD on the SQL Server container
        #   AUTH0_DOMAIN     → Auth0__Domain on the API container
        #   AUTH0_AUDIENCE   → Auth0__Audience on the API container
        run: |
          echo "AUTH0_DOMAIN=${{ secrets.AUTH0_DOMAIN }}" >> .env
          echo "AUTH0_AUDIENCE=${{ secrets.AUTH0_AUDIENCE }}" >> .env
          echo "DB_PASSWORD=${{ secrets.DB_PASSWORD }}" >> .env

      - name: Start Docker Compose stack
        run: |
          docker compose up -d
          curl --retry 15 --retry-delay 3 --retry-connrefused -s http://localhost:4280/ > /dev/null

      - name: Run feature tests
        env:
          AUTH0_TEST_USER_EMAIL: ${{ secrets.AUTH0_TEST_USER_EMAIL }}
          AUTH0_TEST_USER_PASSWORD: ${{ secrets.AUTH0_TEST_USER_PASSWORD }}
        run: dotnet test tests/FootballPlanner.Feature.Tests --no-build --configuration Release --logger trx

      - name: Stop Docker Compose stack
        if: always()
        run: docker compose down
```

**GitHub secrets required** (add these in your repository Settings → Secrets → Actions):
- `AUTH0_DOMAIN` — your Auth0 tenant domain (e.g. `your-tenant.auth0.com`)
- `AUTH0_AUDIENCE` — your API identifier (e.g. `https://your-api-identifier`)
- `DB_PASSWORD` — SQL Server SA password (e.g. `YourStrongPassword123!`)
- `AUTH0_TEST_USER_EMAIL` — test user email
- `AUTH0_TEST_USER_PASSWORD` — test user password

- [ ] **Step 3: Verify build still succeeds**

```bash
dotnet build FootballPlanner.slnx --configuration Release 2>&1 | tail -5
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4: Commit and push**

```bash
git add .github/workflows/ci.yml
git commit -m "feat: add Docker Compose and Playwright setup to CI for feature tests"
git push origin main
```
