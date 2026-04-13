# MudBlazor Foundation + Simple Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Bootstrap with MudBlazor, implement the orange Material Design theme, persistent side-drawer layout, and rewrite the Phases, Focuses, and Activity Library pages as two-panel MudBlazor UIs.

**Architecture:** MudBlazor 7.x added as a NuGet package to FootballPlanner.Web. Bootstrap CSS/JS removed from index.html. All pages use MudBlazor components with inline `@code {}` blocks. Feature test journeys updated to use MudBlazor-compatible Playwright selectors.

**Tech Stack:** .NET 10, Blazor WebAssembly, MudBlazor (latest stable 7.x), xUnit, Playwright

---

## File Map

**Modified:**
- `src/FootballPlanner.Web/FootballPlanner.Web.csproj` — add MudBlazor NuGet reference
- `src/FootballPlanner.Web/wwwroot/index.html` — swap Bootstrap CSS/JS for MudBlazor CSS/JS + Google Fonts
- `src/FootballPlanner.Web/Program.cs` — register `AddMudServices()`
- `src/FootballPlanner.Web/_Imports.razor` — add `@using MudBlazor`
- `src/FootballPlanner.Web/Layout/MainLayout.razor` — full rewrite with `MudLayout`, `MudAppBar`, `MudDrawer`, orange `MudTheme`
- `src/FootballPlanner.Web/Layout/NavMenu.razor` — full rewrite with `MudNavMenu` / `MudNavLink` + icons
- `src/FootballPlanner.Web/wwwroot/css/app.css` — strip Bootstrap-dependent styles, keep loading/error UI
- `src/FootballPlanner.Web/Pages/Phases.razor` — two-panel rewrite
- `src/FootballPlanner.Web/Pages/Focuses.razor` — two-panel rewrite
- `src/FootballPlanner.Web/Pages/Activities.razor` — two-panel rewrite with search
- `tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs` — update selectors for MudBlazor
- `tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs` — update selectors for MudBlazor
- `tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs` — update selectors for MudBlazor

---

## Task 1: Add MudBlazor and wire up services

**Files:**
- Modify: `src/FootballPlanner.Web/FootballPlanner.Web.csproj`
- Modify: `src/FootballPlanner.Web/wwwroot/index.html`
- Modify: `src/FootballPlanner.Web/Program.cs`
- Modify: `src/FootballPlanner.Web/_Imports.razor`

- [ ] **Step 1: Add MudBlazor NuGet package**

```bash
dotnet add src/FootballPlanner.Web/FootballPlanner.Web.csproj package MudBlazor
```

Expected output: Package added successfully.

- [ ] **Step 2: Update index.html — swap Bootstrap for MudBlazor**

Replace the entire content of `src/FootballPlanner.Web/wwwroot/index.html`:

```html
<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Football Planner</title>
    <base href="/" />
    <link rel="preload" id="webassembly" />
    <link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
    <link href="_content/MudBlazor/MudBlazor.min.css" rel="stylesheet" />
    <link rel="stylesheet" href="css/app.css" />
    <link rel="icon" type="image/png" href="favicon.png" />
    <link href="FootballPlanner.Web.styles.css" rel="stylesheet" />
    <script type="importmap"></script>
</head>

<body>
    <div id="app">
        <svg class="loading-progress">
            <circle r="40%" cx="50%" cy="50%" />
            <circle r="40%" cx="50%" cy="50%" />
        </svg>
        <div class="loading-progress-text"></div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="." class="reload">Reload</a>
        <span class="dismiss">🗙</span>
    </div>
    <script src="_content/Microsoft.AspNetCore.Components.WebAssembly.Authentication/AuthenticationService.js"></script>
    <script src="_content/MudBlazor/MudBlazor.min.js"></script>
    <script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>
</body>

</html>
```

- [ ] **Step 3: Register MudBlazor services in Program.cs**

Replace the content of `src/FootballPlanner.Web/Program.cs`:

```csharp
using FootballPlanner.Web;
using FootballPlanner.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? $"{builder.HostEnvironment.BaseAddress}api";

builder.Services.AddOidcAuthentication(options =>
{
    builder.Configuration.Bind("Auth0", options.ProviderOptions);
    options.ProviderOptions.ResponseType = "token id_token";
    options.ProviderOptions.AdditionalProviderParameters.Add("audience", builder.Configuration["Auth0:Audience"] ?? "");
});

builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddHttpClient<ApiClient>(client => client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler(sp =>
    {
        var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
        handler.ConfigureHandler(authorizedUrls: [apiBaseUrl]);
        return handler;
    });

builder.Services.AddMudServices();

await builder.Build().RunAsync();
```

- [ ] **Step 4: Add MudBlazor namespace to _Imports.razor**

Add `@using MudBlazor` to `src/FootballPlanner.Web/_Imports.razor`:

```razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using MudBlazor
@using FootballPlanner.Web
@using FootballPlanner.Web.Layout
@using FootballPlanner.Web.Shared
```

- [ ] **Step 5: Verify app builds**

```bash
dotnet build src/FootballPlanner.Web/FootballPlanner.Web.csproj
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/FootballPlanner.Web/FootballPlanner.Web.csproj \
        src/FootballPlanner.Web/wwwroot/index.html \
        src/FootballPlanner.Web/Program.cs \
        src/FootballPlanner.Web/_Imports.razor
git commit -m "feat: add MudBlazor and register services"
```

---

## Task 2: Replace layout with MudBlazor persistent drawer

**Files:**
- Modify: `src/FootballPlanner.Web/Layout/MainLayout.razor`
- Modify: `src/FootballPlanner.Web/Layout/NavMenu.razor`
- Modify: `src/FootballPlanner.Web/wwwroot/css/app.css`

- [ ] **Step 1: Rewrite MainLayout.razor**

Replace the entire content of `src/FootballPlanner.Web/Layout/MainLayout.razor`:

```razor
@inherits LayoutComponentBase

<MudThemeProvider Theme="_theme" />
<MudDialogProvider />
<MudSnackbarProvider />

<MudLayout>
    <MudAppBar Color="Color.Primary" Elevation="1">
        <MudIconButton Icon="@Icons.Material.Filled.Menu" Color="Color.Inherit" Edge="Edge.Start"
                       OnClick="@ToggleDrawer" />
        <MudText Typo="Typo.h6" Class="ml-3">⚽ Football Planner</MudText>
        <MudSpacer />
        <AuthorizeView>
            <Authorized>
                <MudIconButton Icon="@Icons.Material.Filled.Logout" Color="Color.Inherit"
                               Href="authentication/logout" Title="Sign out" />
            </Authorized>
        </AuthorizeView>
    </MudAppBar>
    <MudDrawer @bind-Open="_drawerOpen" Elevation="1" ClipMode="DrawerClipMode.Always">
        <MudDrawerHeader>
            <MudText Typo="Typo.subtitle1" Class="mt-1 mud-text-secondary">Football Planner</MudText>
        </MudDrawerHeader>
        <NavMenu />
    </MudDrawer>
    <MudMainContent>
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mt-6 mb-6">
            @Body
        </MudContainer>
    </MudMainContent>
</MudLayout>

@code {
    private bool _drawerOpen = true;

    private readonly MudTheme _theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#E65100",
            PrimaryDarken = "#BF360C",
            PrimaryLighten = "#FF6D00",
            AppbarBackground = "#E65100",
        }
    };

    private void ToggleDrawer() => _drawerOpen = !_drawerOpen;
}
```

- [ ] **Step 2: Rewrite NavMenu.razor**

Replace the entire content of `src/FootballPlanner.Web/Layout/NavMenu.razor`:

```razor
<MudNavMenu>
    <MudNavLink Href="sessions" Icon="@Icons.Material.Filled.CalendarMonth" Match="NavLinkMatch.Prefix">
        Sessions
    </MudNavLink>
    <MudNavLink Href="activities" Icon="@Icons.Material.Filled.SportsSoccer" Match="NavLinkMatch.Prefix">
        Activities
    </MudNavLink>
    <MudNavLink Href="phases" Icon="@Icons.Material.Filled.Layers" Match="NavLinkMatch.Prefix">
        Phases
    </MudNavLink>
    <MudNavLink Href="focuses" Icon="@Icons.Material.Filled.TrackChanges" Match="NavLinkMatch.Prefix">
        Focuses
    </MudNavLink>
</MudNavMenu>
```

- [ ] **Step 3: Strip Bootstrap-dependent styles from app.css**

Replace the entire content of `src/FootballPlanner.Web/wwwroot/css/app.css`:

```css
#blazor-error-ui {
    color-scheme: light only;
    background: lightyellow;
    bottom: 0;
    box-shadow: 0 -1px 2px rgba(0, 0, 0, 0.2);
    box-sizing: border-box;
    display: none;
    left: 0;
    padding: 0.6rem 1.25rem 0.7rem 1.25rem;
    position: fixed;
    width: 100%;
    z-index: 1000;
}

#blazor-error-ui .dismiss {
    cursor: pointer;
    position: absolute;
    right: 0.75rem;
    top: 0.5rem;
}

.blazor-error-boundary {
    background: #b32121;
    padding: 1rem 1rem 1rem 3.7rem;
    color: white;
}

.blazor-error-boundary::after {
    content: "An error has occurred.";
}

.loading-progress {
    position: absolute;
    display: block;
    width: 8rem;
    height: 8rem;
    inset: 20vh 0 auto 0;
    margin: 0 auto;
}

.loading-progress circle {
    fill: none;
    stroke: #e0e0e0;
    stroke-width: 0.6rem;
    transform-origin: 50% 50%;
    transform: rotate(-90deg);
}

.loading-progress circle:last-child {
    stroke: #E65100;
    stroke-dasharray: calc(3.141 * var(--blazor-load-percentage, 0%) * 0.8), 500%;
    transition: stroke-dasharray 0.05s ease-in-out;
}

.loading-progress-text {
    position: absolute;
    text-align: center;
    font-weight: bold;
    inset: calc(20vh + 3.25rem) 0 auto 0.2rem;
}

.loading-progress-text:after {
    content: var(--blazor-load-percentage-text, "Loading");
}
```

- [ ] **Step 4: Verify app builds**

```bash
dotnet build src/FootballPlanner.Web/FootballPlanner.Web.csproj
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Layout/MainLayout.razor \
        src/FootballPlanner.Web/Layout/NavMenu.razor \
        src/FootballPlanner.Web/wwwroot/css/app.css
git commit -m "feat: replace layout with MudBlazor persistent drawer and orange theme"
```

---

## Task 3: Phases page — two-panel MudBlazor UI

**Files:**
- Modify: `src/FootballPlanner.Web/Pages/Phases.razor`
- Modify: `tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs`

- [ ] **Step 1: Update PhaseJourney to use MudBlazor selectors**

Replace the entire content of `tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs`:

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

        await page.GetByRole(AriaRole.Button, new() { Name = "New Phase" }).ClickAsync();
        await page.GetByLabel("Name").FillAsync(input.Name);
        await page.GetByLabel("Order").FillAsync(input.Order.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 2: Run feature tests to confirm journey now fails**

```bash
dotnet test tests/FootballPlanner.Feature.Tests --filter "FullyQualifiedName~PlanningJourneyTests" 2>&1 | tail -20
```

Expected: FAIL — the old page doesn't have "New Phase" button or "Save" button.

- [ ] **Step 3: Rewrite Phases.razor**

Replace the entire content of `src/FootballPlanner.Web/Pages/Phases.razor`:

```razor
@page "/phases"
@inject Services.ApiClient Api

<MudText Typo="Typo.h4" Class="mb-4">Phases</MudText>

<MudGrid>
    <MudItem xs="12" sm="4">
        <MudPaper Class="pa-4" Elevation="1">
            <div class="d-flex justify-space-between align-center mb-3">
                <MudText Typo="Typo.h6">All Phases</MudText>
                <MudFab Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                        OnClick="CreateNew" aria-label="New Phase" />
            </div>
            @if (_phases == null)
            {
                <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
            }
            else
            {
                <MudList Dense="true">
                    @foreach (var phase in _phases.OrderBy(p => p.Order))
                    {
                        <MudListItem OnClick="() => SelectPhase(phase)"
                                     Class="@(_selectedId == phase.Id ? "mud-selected-item" : "")">
                            @phase.Order. @phase.Name
                        </MudListItem>
                    }
                </MudList>
            }
        </MudPaper>
    </MudItem>
    <MudItem xs="12" sm="8">
        @if (_selectedId != null || _isCreating)
        {
            <MudPaper Class="pa-4" Elevation="1">
                <MudText Typo="Typo.h6" Class="mb-4">@(_isCreating ? "New Phase" : "Edit Phase")</MudText>
                <MudTextField @bind-Value="_editName" Label="Name" Variant="Variant.Outlined" Class="mb-3" />
                <MudNumericField @bind-Value="_editOrder" Label="Order" Variant="Variant.Outlined" Min="1" Class="mb-3" />
                <div class="d-flex gap-2">
                    <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Save">Save</MudButton>
                    @if (!_isCreating)
                    {
                        <MudButton Color="Color.Error" Variant="Variant.Outlined" OnClick="Delete">Delete</MudButton>
                    }
                    <MudButton Variant="Variant.Text" OnClick="Cancel">Cancel</MudButton>
                </div>
            </MudPaper>
        }
        else
        {
            <MudPaper Class="pa-4 d-flex align-center justify-center mud-height-full" Elevation="0">
                <MudText Color="Color.Secondary">Select a phase to edit, or click + to create one</MudText>
            </MudPaper>
        }
    </MudItem>
</MudGrid>

@code {
    private List<Services.ApiClient.PhaseDto>? _phases;
    private int? _selectedId;
    private bool _isCreating;
    private string _editName = string.Empty;
    private int _editOrder = 1;

    protected override async Task OnInitializedAsync()
    {
        _phases = await Api.GetPhasesAsync();
    }

    private void CreateNew()
    {
        _isCreating = true;
        _selectedId = null;
        _editName = string.Empty;
        _editOrder = (_phases?.Count ?? 0) + 1;
    }

    private void SelectPhase(Services.ApiClient.PhaseDto phase)
    {
        _isCreating = false;
        _selectedId = phase.Id;
        _editName = phase.Name;
        _editOrder = phase.Order;
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_editName)) return;
        if (_isCreating)
        {
            await Api.CreatePhaseAsync(new Services.ApiClient.CreatePhaseRequest(_editName, _editOrder));
        }
        else if (_selectedId != null)
        {
            await Api.UpdatePhaseAsync(_selectedId.Value,
                new Services.ApiClient.UpdatePhaseRequest(_editName, _editOrder));
        }
        _phases = await Api.GetPhasesAsync();
        Cancel();
    }

    private async Task Delete()
    {
        if (_selectedId == null) return;
        await Api.DeletePhaseAsync(_selectedId.Value);
        _phases = await Api.GetPhasesAsync();
        Cancel();
    }

    private void Cancel()
    {
        _selectedId = null;
        _isCreating = false;
        _editName = string.Empty;
        _editOrder = 1;
    }
}
```

- [ ] **Step 4: Run feature tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Feature.Tests --filter "FullyQualifiedName~PlanningJourneyTests" 2>&1 | tail -20
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Pages/Phases.razor \
        tests/FootballPlanner.Feature.Tests/Journeys/PhaseJourney.cs
git commit -m "feat: rewrite Phases page as MudBlazor two-panel UI"
```

---

## Task 4: Focuses page — two-panel MudBlazor UI

**Files:**
- Modify: `src/FootballPlanner.Web/Pages/Focuses.razor`
- Modify: `tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs`

- [ ] **Step 1: Update FocusJourney to use MudBlazor selectors**

Replace the entire content of `tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs`:

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

        await page.GetByRole(AriaRole.Button, new() { Name = "New Focus" }).ClickAsync();
        await page.GetByLabel("Name").FillAsync(input.Name);
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 2: Run feature tests to confirm journey now fails**

```bash
dotnet test tests/FootballPlanner.Feature.Tests --filter "FullyQualifiedName~PlanningJourneyTests" 2>&1 | tail -20
```

Expected: FAIL — old Focuses page doesn't have "New Focus" or "Save" buttons.

- [ ] **Step 3: Rewrite Focuses.razor**

Replace the entire content of `src/FootballPlanner.Web/Pages/Focuses.razor`:

```razor
@page "/focuses"
@inject Services.ApiClient Api

<MudText Typo="Typo.h4" Class="mb-4">Focuses</MudText>

<MudGrid>
    <MudItem xs="12" sm="4">
        <MudPaper Class="pa-4" Elevation="1">
            <div class="d-flex justify-space-between align-center mb-3">
                <MudText Typo="Typo.h6">All Focuses</MudText>
                <MudFab Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                        OnClick="CreateNew" aria-label="New Focus" />
            </div>
            @if (_focuses == null)
            {
                <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
            }
            else
            {
                <MudList Dense="true">
                    @foreach (var focus in _focuses)
                    {
                        <MudListItem OnClick="() => SelectFocus(focus)"
                                     Class="@(_selectedId == focus.Id ? "mud-selected-item" : "")">
                            @focus.Name
                        </MudListItem>
                    }
                </MudList>
            }
        </MudPaper>
    </MudItem>
    <MudItem xs="12" sm="8">
        @if (_selectedId != null || _isCreating)
        {
            <MudPaper Class="pa-4" Elevation="1">
                <MudText Typo="Typo.h6" Class="mb-4">@(_isCreating ? "New Focus" : "Edit Focus")</MudText>
                <MudTextField @bind-Value="_editName" Label="Name" Variant="Variant.Outlined" Class="mb-3" />
                <div class="d-flex gap-2">
                    <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Save">Save</MudButton>
                    @if (!_isCreating)
                    {
                        <MudButton Color="Color.Error" Variant="Variant.Outlined" OnClick="Delete">Delete</MudButton>
                    }
                    <MudButton Variant="Variant.Text" OnClick="Cancel">Cancel</MudButton>
                </div>
            </MudPaper>
        }
        else
        {
            <MudPaper Class="pa-4 d-flex align-center justify-center mud-height-full" Elevation="0">
                <MudText Color="Color.Secondary">Select a focus to edit, or click + to create one</MudText>
            </MudPaper>
        }
    </MudItem>
</MudGrid>

@code {
    private List<Services.ApiClient.FocusDto>? _focuses;
    private int? _selectedId;
    private bool _isCreating;
    private string _editName = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _focuses = await Api.GetFocusesAsync();
    }

    private void CreateNew()
    {
        _isCreating = true;
        _selectedId = null;
        _editName = string.Empty;
    }

    private void SelectFocus(Services.ApiClient.FocusDto focus)
    {
        _isCreating = false;
        _selectedId = focus.Id;
        _editName = focus.Name;
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_editName)) return;
        if (_isCreating)
        {
            await Api.CreateFocusAsync(new Services.ApiClient.CreateFocusRequest(_editName));
        }
        else if (_selectedId != null)
        {
            await Api.UpdateFocusAsync(_selectedId.Value,
                new Services.ApiClient.UpdateFocusRequest(_editName));
        }
        _focuses = await Api.GetFocusesAsync();
        Cancel();
    }

    private async Task Delete()
    {
        if (_selectedId == null) return;
        await Api.DeleteFocusAsync(_selectedId.Value);
        _focuses = await Api.GetFocusesAsync();
        Cancel();
    }

    private void Cancel()
    {
        _selectedId = null;
        _isCreating = false;
        _editName = string.Empty;
    }
}
```

- [ ] **Step 4: Run feature tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Feature.Tests --filter "FullyQualifiedName~PlanningJourneyTests" 2>&1 | tail -20
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Pages/Focuses.razor \
        tests/FootballPlanner.Feature.Tests/Journeys/FocusJourney.cs
git commit -m "feat: rewrite Focuses page as MudBlazor two-panel UI"
```

---

## Task 5: Activity Library — two-panel MudBlazor UI with search

**Files:**
- Modify: `src/FootballPlanner.Web/Pages/Activities.razor`
- Modify: `tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs`

- [ ] **Step 1: Update ActivityJourney to use MudBlazor selectors**

Replace the entire content of `tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs`:

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

        await page.GetByRole(AriaRole.Button, new() { Name = "New Activity" }).ClickAsync();
        await page.GetByLabel("Name").FillAsync(input.Name);
        await page.GetByLabel("Description").FillAsync(input.Description);
        await page.GetByLabel("Duration (min)").FillAsync(input.EstimatedDurationMinutes.ToString());
        await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 2: Run feature tests to confirm journey now fails**

```bash
dotnet test tests/FootballPlanner.Feature.Tests --filter "FullyQualifiedName~PlanningJourneyTests" 2>&1 | tail -20
```

Expected: FAIL — old Activities page doesn't have "New Activity" or "Save" buttons.

- [ ] **Step 3: Rewrite Activities.razor**

Replace the entire content of `src/FootballPlanner.Web/Pages/Activities.razor`:

```razor
@page "/activities"
@inject Services.ApiClient Api
@inject IJSRuntime JS

<MudText Typo="Typo.h4" Class="mb-4">Activity Library</MudText>

<MudGrid>
    <MudItem xs="12" sm="4">
        <MudPaper Class="pa-4" Elevation="1">
            <div class="d-flex justify-space-between align-center mb-3">
                <MudText Typo="Typo.h6">All Activities</MudText>
                <MudFab Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
                        OnClick="CreateNew" aria-label="New Activity" />
            </div>
            <MudTextField @bind-Value="_search" Label="Search" Adornment="Adornment.Start"
                          AdornmentIcon="@Icons.Material.Filled.Search" Variant="Variant.Outlined"
                          Class="mb-3" Immediate="true" />
            @if (_activities == null)
            {
                <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
            }
            else
            {
                <MudList Dense="true">
                    @foreach (var activity in FilteredActivities)
                    {
                        <MudListItem OnClick="() => SelectActivity(activity)"
                                     Class="@(_selectedId == activity.Id ? "mud-selected-item" : "")">
                            <div>
                                <MudText Typo="Typo.body2">@activity.Name</MudText>
                                <MudText Typo="Typo.caption" Color="Color.Secondary">@activity.EstimatedDuration min</MudText>
                            </div>
                        </MudListItem>
                    }
                </MudList>
            }
        </MudPaper>
    </MudItem>
    <MudItem xs="12" sm="8">
        @if (_selectedId != null || _isCreating)
        {
            <MudPaper Class="pa-4" Elevation="1">
                <MudText Typo="Typo.h6" Class="mb-4">@(_isCreating ? "New Activity" : "Edit Activity")</MudText>
                <MudTextField @bind-Value="_editName" Label="Name" Variant="Variant.Outlined" Class="mb-3" />
                <MudTextField @bind-Value="_editDescription" Label="Description" Variant="Variant.Outlined"
                              Lines="3" Class="mb-3" />
                <MudTextField @bind-Value="_editInspirationUrl" Label="Inspiration URL" Variant="Variant.Outlined"
                              Adornment="Adornment.End"
                              AdornmentIcon="@Icons.Material.Filled.OpenInNew"
                              OnAdornmentClick="OpenInspirationUrl"
                              Class="mb-3" />
                <MudNumericField @bind-Value="_editDuration" Label="Duration (min)" Variant="Variant.Outlined"
                                 Min="1" Class="mb-3" />
                <MudPaper Outlined="true" Class="pa-4 mb-3 d-flex align-center justify-center"
                          Style="min-height:80px; cursor:default;">
                    <MudText Color="Color.Secondary">🏟️ Pitch diagram builder (coming soon)</MudText>
                </MudPaper>
                <div class="d-flex gap-2">
                    <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Save">Save</MudButton>
                    @if (!_isCreating)
                    {
                        <MudButton Color="Color.Error" Variant="Variant.Outlined" OnClick="Delete">Delete</MudButton>
                    }
                    <MudButton Variant="Variant.Text" OnClick="Cancel">Cancel</MudButton>
                </div>
            </MudPaper>
        }
        else
        {
            <MudPaper Class="pa-4 d-flex align-center justify-center mud-height-full" Elevation="0">
                <MudText Color="Color.Secondary">Select an activity to edit, or click + to create one</MudText>
            </MudPaper>
        }
    </MudItem>
</MudGrid>

@code {
    private List<Services.ApiClient.ActivityDto>? _activities;
    private int? _selectedId;
    private bool _isCreating;
    private string _search = string.Empty;

    private string _editName = string.Empty;
    private string _editDescription = string.Empty;
    private string _editInspirationUrl = string.Empty;
    private int _editDuration = 30;

    private IEnumerable<Services.ApiClient.ActivityDto> FilteredActivities =>
        _activities?.Where(a => string.IsNullOrWhiteSpace(_search) ||
            a.Name.Contains(_search, StringComparison.OrdinalIgnoreCase)) ?? [];

    protected override async Task OnInitializedAsync()
    {
        _activities = await Api.GetActivitiesAsync();
    }

    private void CreateNew()
    {
        _isCreating = true;
        _selectedId = null;
        _editName = string.Empty;
        _editDescription = string.Empty;
        _editInspirationUrl = string.Empty;
        _editDuration = 30;
    }

    private void SelectActivity(Services.ApiClient.ActivityDto activity)
    {
        _isCreating = false;
        _selectedId = activity.Id;
        _editName = activity.Name;
        _editDescription = activity.Description;
        _editInspirationUrl = activity.InspirationUrl ?? string.Empty;
        _editDuration = activity.EstimatedDuration;
    }

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_editName)) return;
        var url = string.IsNullOrWhiteSpace(_editInspirationUrl) ? null : _editInspirationUrl;
        if (_isCreating)
        {
            await Api.CreateActivityAsync(new Services.ApiClient.CreateActivityRequest(
                _editName, _editDescription, url, _editDuration));
        }
        else if (_selectedId != null)
        {
            await Api.UpdateActivityAsync(_selectedId.Value,
                new Services.ApiClient.UpdateActivityRequest(_editName, _editDescription, url, _editDuration));
        }
        _activities = await Api.GetActivitiesAsync();
        Cancel();
    }

    private async Task Delete()
    {
        if (_selectedId == null) return;
        await Api.DeleteActivityAsync(_selectedId.Value);
        _activities = await Api.GetActivitiesAsync();
        Cancel();
    }

    private void Cancel()
    {
        _selectedId = null;
        _isCreating = false;
        _editName = string.Empty;
        _editDescription = string.Empty;
        _editInspirationUrl = string.Empty;
        _editDuration = 30;
    }

    private async Task OpenInspirationUrl()
    {
        if (!string.IsNullOrWhiteSpace(_editInspirationUrl))
            await JS.InvokeVoidAsync("open", _editInspirationUrl, "_blank");
    }
}
```

- [ ] **Step 4: Run feature tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Feature.Tests --filter "FullyQualifiedName~PlanningJourneyTests" 2>&1 | tail -20
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Pages/Activities.razor \
        tests/FootballPlanner.Feature.Tests/Journeys/ActivityJourney.cs
git commit -m "feat: rewrite Activity Library as MudBlazor two-panel UI with search"
```
