# Sessions UI Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the bare HTML sessions list and session editor with polished MudBlazor pages — card grid grouped by month, two-panel session editor with inline key points, and an activity picker dialog.

**Architecture:** Sessions list becomes a `MudCard` grid grouped by month with a ⋮ menu per card and a `MudFab` that opens a `MudDialog` to create sessions. Session editor becomes a two-panel `MudGrid`: left panel is an ordered activity list with up/down reorder buttons; right panel is a detail editor that auto-saves on field blur/change. Activity picking uses a search dialog. Reorder is backed by a new `ReorderSessionActivitiesCommand`.

**Tech Stack:** MudBlazor 9.3.x, Blazor WebAssembly, MediatR, EF Core, Azure Functions v4, Playwright (feature tests).

---

## File Map

**New files:**
- `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommand.cs`
- `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandHandler.cs`
- `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandValidator.cs`
- `src/FootballPlanner.Web/Dialogs/SessionDetailsDialog.razor`
- `src/FootballPlanner.Web/Dialogs/ActivityPickerDialog.razor`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/ReorderSessionActivitiesCommandTests.cs`

**Modified files:**
- `src/FootballPlanner.Domain/Entities/SessionActivity.cs` — add `UpdateDisplayOrder` method
- `src/FootballPlanner.Api/Functions/SessionFunctions.cs` — add reorder endpoint
- `src/FootballPlanner.Web/Services/ApiClient.cs` — add `ReorderSessionActivitiesAsync` + DTOs
- `src/FootballPlanner.Web/Pages/Sessions.razor` — full rewrite with MudBlazor cards
- `src/FootballPlanner.Web/Pages/SessionEditor.razor` — full rewrite with two-panel layout
- `tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs` — update for new UI
- `tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs` — update for new UI
- `tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs` — fix broken table assertions

---

## Task 1: Reorder API

**Files:**
- Modify: `src/FootballPlanner.Domain/Entities/SessionActivity.cs`
- Create: `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommand.cs`
- Create: `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandHandler.cs`
- Create: `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandValidator.cs`
- Modify: `src/FootballPlanner.Api/Functions/SessionFunctions.cs`
- Modify: `src/FootballPlanner.Web/Services/ApiClient.cs`
- Create: `tests/FootballPlanner.Unit.Tests/SessionActivity/ReorderSessionActivitiesCommandTests.cs`

- [ ] **Step 1: Write the failing unit test**

Create `tests/FootballPlanner.Unit.Tests/SessionActivity/ReorderSessionActivitiesCommandTests.cs`:

```csharp
using FootballPlanner.Application.Activity.Commands;
using FootballPlanner.Application.Focus.Commands;
using FootballPlanner.Application.Phase.Commands;
using FootballPlanner.Application.Session.Commands;
using FootballPlanner.Application.Session.Queries;
using FootballPlanner.Application.SessionActivity.Commands;
using FootballPlanner.Unit.Tests.Infrastructure;
using MediatR;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class ReorderSessionActivitiesCommandTests
{
    private async Task<(IMediator mediator, int sessionId, int sa1Id, int sa2Id)> SetupAsync()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test Session", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        var sa1 = await mediator.Send(new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));
        var sa2 = await mediator.Send(new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 15, null));

        return (mediator, session.Id, sa1.Id, sa2.Id);
    }

    [Fact]
    public async Task Send_UpdatesDisplayOrders_WhenCommandIsValid()
    {
        var (mediator, sessionId, sa1Id, sa2Id) = await SetupAsync();

        await mediator.Send(new ReorderSessionActivitiesCommand(sessionId, [
            new ReorderItem(sa1Id, 2),
            new ReorderItem(sa2Id, 1),
        ]));

        var session = await mediator.Send(new GetSessionByIdQuery(sessionId));
        var sa1 = session!.Activities.Single(a => a.Id == sa1Id);
        var sa2 = session.Activities.Single(a => a.Id == sa2Id);

        Assert.Equal(2, sa1.DisplayOrder);
        Assert.Equal(1, sa2.DisplayOrder);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenItemsIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new ReorderSessionActivitiesCommand(1, [])));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~ReorderSessionActivitiesCommandTests" -v
```

Expected: FAIL — `ReorderSessionActivitiesCommand` does not exist yet.

- [ ] **Step 3: Add `UpdateDisplayOrder` to the domain entity**

In `src/FootballPlanner.Domain/Entities/SessionActivity.cs`, add a new method after the `Update` method:

```csharp
public void UpdateDisplayOrder(int displayOrder)
{
    DisplayOrder = displayOrder;
}
```

Full file after change:

```csharp
namespace FootballPlanner.Domain.Entities;

public class SessionActivity
{
    public int Id { get; private set; }
    public int SessionId { get; private set; }
    public int ActivityId { get; private set; }
    public int PhaseId { get; private set; }
    public int FocusId { get; private set; }
    public int Duration { get; private set; }
    public int DisplayOrder { get; private set; }
    public string? Notes { get; private set; }

    public Activity Activity { get; private set; } = null!;
    public Phase Phase { get; private set; } = null!;
    public Focus Focus { get; private set; } = null!;
    public List<SessionActivityKeyPoint> KeyPoints { get; private set; } = new();

    private SessionActivity() { }

    public static SessionActivity Create(
        int sessionId, int activityId, int phaseId, int focusId,
        int duration, int displayOrder, string? notes)
        => new SessionActivity
        {
            SessionId = sessionId,
            ActivityId = activityId,
            PhaseId = phaseId,
            FocusId = focusId,
            Duration = duration,
            DisplayOrder = displayOrder,
            Notes = notes,
        };

    public void Update(int phaseId, int focusId, int duration, string? notes)
    {
        PhaseId = phaseId;
        FocusId = focusId;
        Duration = duration;
        Notes = notes;
    }

    public void UpdateDisplayOrder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }
}
```

- [ ] **Step 4: Create the command record**

Create `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommand.cs`:

```csharp
using MediatR;

namespace FootballPlanner.Application.SessionActivity.Commands;

public record ReorderItem(int SessionActivityId, int DisplayOrder);

public record ReorderSessionActivitiesCommand(
    int SessionId,
    List<ReorderItem> Items) : IRequest;
```

- [ ] **Step 5: Create the command handler**

Create `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandHandler.cs`:

```csharp
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class ReorderSessionActivitiesCommandHandler(AppDbContext db)
    : IRequestHandler<ReorderSessionActivitiesCommand>
{
    public async Task Handle(
        ReorderSessionActivitiesCommand request, CancellationToken cancellationToken)
    {
        var sessionActivities = await db.SessionActivities
            .Where(sa => sa.SessionId == request.SessionId)
            .ToListAsync(cancellationToken);

        foreach (var item in request.Items)
        {
            var sa = sessionActivities.FirstOrDefault(a => a.Id == item.SessionActivityId);
            sa?.UpdateDisplayOrder(item.DisplayOrder);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Create the command validator**

Create `src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandValidator.cs`:

```csharp
using FluentValidation;

namespace FootballPlanner.Application.SessionActivity.Commands;

public class ReorderSessionActivitiesCommandValidator
    : AbstractValidator<ReorderSessionActivitiesCommand>
{
    public ReorderSessionActivitiesCommandValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

- [ ] **Step 7: Run unit tests to verify they pass**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~ReorderSessionActivitiesCommandTests" -v
```

Expected: 2 tests PASS.

- [ ] **Step 8: Add the reorder endpoint to SessionFunctions**

In `src/FootballPlanner.Api/Functions/SessionFunctions.cs`, add a new function after `UpdateKeyPoints` and a new private record. Full file after change:

```csharp
using FootballPlanner.Application.Session.Commands;
using FootballPlanner.Application.Session.Queries;
using FootballPlanner.Application.SessionActivity.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace FootballPlanner.Api.Functions;

public class SessionFunctions(IMediator mediator)
{
    [Function("GetSessions")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions")] HttpRequestData req)
    {
        var sessions = await mediator.Send(new GetAllSessionsQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(sessions);
        return response;
    }

    [Function("GetSessionById")]
    public async Task<HttpResponseData> GetById(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "sessions/{id:int}")] HttpRequestData req,
        int id)
    {
        var session = await mediator.Send(new GetSessionByIdQuery(id));
        if (session is null)
            return req.CreateResponse(HttpStatusCode.NotFound);
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(session);
        return response;
    }

    [Function("CreateSession")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreateSessionCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var session = await mediator.Send(command);
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(session);
        return response;
    }

    [Function("UpdateSession")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sessions/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdateSessionRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateSessionCommand(id, body.Date, body.Title, body.Notes));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("DeleteSession")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sessions/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeleteSessionCommand(id));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("AddSessionActivity")]
    public async Task<HttpResponseData> AddActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sessions/{id:int}/activities")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<AddSessionActivityRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var sa = await mediator.Send(new AddSessionActivityCommand(
            id, body.ActivityId, body.PhaseId, body.FocusId, body.Duration, body.Notes));
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(sa);
        return response;
    }

    [Function("UpdateSessionActivity")]
    public async Task<HttpResponseData> UpdateActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sessions/{id:int}/activities/{sessionActivityId:int}")] HttpRequestData req,
        int id, int sessionActivityId)
    {
        var body = await req.ReadFromJsonAsync<UpdateSessionActivityRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateSessionActivityCommand(
            sessionActivityId, body.PhaseId, body.FocusId, body.Duration, body.Notes));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("RemoveSessionActivity")]
    public async Task<HttpResponseData> RemoveActivity(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "sessions/{id:int}/activities/{sessionActivityId:int}")] HttpRequestData req,
        int id, int sessionActivityId)
    {
        await mediator.Send(new RemoveSessionActivityCommand(sessionActivityId));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("UpdateSessionActivityKeyPoints")]
    public async Task<HttpResponseData> UpdateKeyPoints(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sessions/{id:int}/activities/{sessionActivityId:int}/keypoints")] HttpRequestData req,
        int id, int sessionActivityId)
    {
        var body = await req.ReadFromJsonAsync<UpdateKeyPointsRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(sessionActivityId, body.KeyPoints));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    [Function("ReorderSessionActivities")]
    public async Task<HttpResponseData> ReorderActivities(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "sessions/{id:int}/activities/reorder")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<ReorderActivitiesRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new ReorderSessionActivitiesCommand(id,
            body.Items.Select(i => new ReorderItem(i.SessionActivityId, i.DisplayOrder)).ToList()));
        return req.CreateResponse(HttpStatusCode.NoContent);
    }

    private record UpdateSessionRequest(DateTime Date, string Title, string? Notes);
    private record AddSessionActivityRequest(int ActivityId, int PhaseId, int FocusId, int Duration, string? Notes);
    private record UpdateSessionActivityRequest(int PhaseId, int FocusId, int Duration, string? Notes);
    private record UpdateKeyPointsRequest(List<string> KeyPoints);
    private record ReorderActivitiesRequest(List<ReorderActivityItem> Items);
    private record ReorderActivityItem(int SessionActivityId, int DisplayOrder);
}
```

- [ ] **Step 9: Add `ReorderSessionActivitiesAsync` to ApiClient**

In `src/FootballPlanner.Web/Services/ApiClient.cs`, add the new method and records. Add the method after `UpdateSessionActivityKeyPointsAsync` and add the new records after `UpdateSessionActivityKeyPointsRequest`:

Full file after change:

```csharp
using System.Net.Http.Json;

namespace FootballPlanner.Web.Services;

public class ApiClient(HttpClient http)
{
    public Task<List<PhaseDto>?> GetPhasesAsync() =>
        http.GetFromJsonAsync<List<PhaseDto>>("phases");

    public Task<HttpResponseMessage> CreatePhaseAsync(CreatePhaseRequest request) =>
        http.PostAsJsonAsync("phases", request);

    public Task<HttpResponseMessage> UpdatePhaseAsync(int id, UpdatePhaseRequest request) =>
        http.PutAsJsonAsync($"phases/{id}", request);

    public Task<HttpResponseMessage> DeletePhaseAsync(int id) =>
        http.DeleteAsync($"phases/{id}");

    public Task<List<FocusDto>?> GetFocusesAsync() =>
        http.GetFromJsonAsync<List<FocusDto>>("focuses");

    public Task<HttpResponseMessage> CreateFocusAsync(CreateFocusRequest request) =>
        http.PostAsJsonAsync("focuses", request);

    public Task<HttpResponseMessage> UpdateFocusAsync(int id, UpdateFocusRequest request) =>
        http.PutAsJsonAsync($"focuses/{id}", request);

    public Task<HttpResponseMessage> DeleteFocusAsync(int id) =>
        http.DeleteAsync($"focuses/{id}");

    public Task<List<ActivityDto>?> GetActivitiesAsync() =>
        http.GetFromJsonAsync<List<ActivityDto>>("activities");

    public Task<ActivityDto?> CreateActivityAsync(CreateActivityRequest request) =>
        http.PostAsJsonAsync("activities", request).ContinueWith(t => t.Result.Content.ReadFromJsonAsync<ActivityDto>()).Unwrap();

    public Task<HttpResponseMessage> UpdateActivityAsync(int id, UpdateActivityRequest request) =>
        http.PutAsJsonAsync($"activities/{id}", request);

    public Task<HttpResponseMessage> DeleteActivityAsync(int id) =>
        http.DeleteAsync($"activities/{id}");

    public Task<List<SessionDto>?> GetSessionsAsync() =>
        http.GetFromJsonAsync<List<SessionDto>>("sessions");

    public Task<SessionDto?> GetSessionAsync(int id) =>
        http.GetFromJsonAsync<SessionDto>($"sessions/{id}");

    public Task<HttpResponseMessage> CreateSessionAsync(CreateSessionRequest request) =>
        http.PostAsJsonAsync("sessions", request);

    public Task<HttpResponseMessage> UpdateSessionAsync(int id, UpdateSessionRequest request) =>
        http.PutAsJsonAsync($"sessions/{id}", request);

    public Task<HttpResponseMessage> DeleteSessionAsync(int id) =>
        http.DeleteAsync($"sessions/{id}");

    public Task<HttpResponseMessage> AddSessionActivityAsync(int sessionId, AddSessionActivityRequest request) =>
        http.PostAsJsonAsync($"sessions/{sessionId}/activities", request);

    public Task<HttpResponseMessage> UpdateSessionActivityAsync(int sessionId, int id, UpdateSessionActivityRequest request) =>
        http.PutAsJsonAsync($"sessions/{sessionId}/activities/{id}", request);

    public Task<HttpResponseMessage> RemoveSessionActivityAsync(int sessionId, int id) =>
        http.DeleteAsync($"sessions/{sessionId}/activities/{id}");

    public Task<HttpResponseMessage> UpdateSessionActivityKeyPointsAsync(int sessionId, int id, List<string> keyPoints) =>
        http.PutAsJsonAsync($"sessions/{sessionId}/activities/{id}/keypoints", new UpdateSessionActivityKeyPointsRequest(keyPoints));

    public Task<HttpResponseMessage> ReorderSessionActivitiesAsync(int sessionId, List<ReorderSessionActivityItem> items) =>
        http.PutAsJsonAsync($"sessions/{sessionId}/activities/reorder", new ReorderSessionActivitiesRequest(items));

    public record PhaseDto(int Id, string Name, int Order);
    public record FocusDto(int Id, string Name);
    public record ActivityDto(
        int Id, string Name, string Description, string? InspirationUrl,
        int EstimatedDuration, string? DiagramJson, DateTime CreatedAt, DateTime UpdatedAt);
    public record SessionDto(
        int Id, string Title, DateTime Date, string? Notes,
        DateTime CreatedAt, DateTime UpdatedAt,
        List<SessionActivityDto> Activities);
    public record SessionActivityDto(
        int Id, int SessionId, int ActivityId, ActivityDto? Activity,
        int PhaseId, PhaseDto? Phase, int FocusId, FocusDto? Focus,
        int Duration, int DisplayOrder, string? Notes,
        List<SessionActivityKeyPointDto> KeyPoints);
    public record SessionActivityKeyPointDto(int Id, int Order, string Text);
    public record CreatePhaseRequest(string Name, int Order);
    public record UpdatePhaseRequest(string Name, int Order);
    public record CreateFocusRequest(string Name);
    public record UpdateFocusRequest(string Name);
    public record CreateActivityRequest(string Name, string Description, string? InspirationUrl, int EstimatedDuration);
    public record UpdateActivityRequest(string Name, string Description, string? InspirationUrl, int EstimatedDuration);
    public record CreateSessionRequest(DateTime Date, string Title, string? Notes);
    public record UpdateSessionRequest(DateTime Date, string Title, string? Notes);
    public record AddSessionActivityRequest(int ActivityId, int PhaseId, int FocusId, int Duration, string? Notes);
    public record UpdateSessionActivityRequest(int PhaseId, int FocusId, int Duration, string? Notes);
    public record UpdateSessionActivityKeyPointsRequest(List<string> KeyPoints);
    public record ReorderSessionActivityItem(int SessionActivityId, int DisplayOrder);
    private record ReorderSessionActivitiesRequest(List<ReorderSessionActivityItem> Items);
}
```

- [ ] **Step 10: Build and run all unit tests**

```bash
dotnet build FootballPlanner.slnx
dotnet test tests/FootballPlanner.Unit.Tests
```

Expected: Build succeeds. All unit tests pass.

- [ ] **Step 11: Commit**

```bash
git add src/FootballPlanner.Domain/Entities/SessionActivity.cs \
        src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommand.cs \
        src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandHandler.cs \
        src/FootballPlanner.Application/SessionActivity/Commands/ReorderSessionActivitiesCommandValidator.cs \
        src/FootballPlanner.Api/Functions/SessionFunctions.cs \
        src/FootballPlanner.Web/Services/ApiClient.cs \
        tests/FootballPlanner.Unit.Tests/SessionActivity/ReorderSessionActivitiesCommandTests.cs
git commit -m "feat: add ReorderSessionActivities command and API endpoint"
```

---

## Task 2: Sessions List (MudBlazor)

**Files:**
- Create: `src/FootballPlanner.Web/Dialogs/SessionDetailsDialog.razor`
- Modify: `src/FootballPlanner.Web/Pages/Sessions.razor`
- Modify: `tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs`
- Modify: `tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs`

- [ ] **Step 1: Create the session details dialog**

Create `src/FootballPlanner.Web/Dialogs/SessionDetailsDialog.razor`:

```razor
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@(IsEditing ? "Edit Session" : "New Session")</MudText>
    </TitleContent>
    <DialogContent>
        <MudDatePicker @bind-Date="_date" Label="Date" Variant="Variant.Outlined" Class="mb-3" />
        <MudTextField @bind-Value="_title" Label="Title" Variant="Variant.Outlined" Class="mb-3" />
        <MudTextField @bind-Value="_notes" Label="Notes (optional)" Variant="Variant.Outlined" Lines="3" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="() => MudDialog.Cancel()">Cancel</MudButton>
        <MudButton Color="Color.Primary" Variant="Variant.Filled" OnClick="Submit">
            @(IsEditing ? "Save" : "Create")
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public bool IsEditing { get; set; }
    [Parameter] public DateTime? InitialDate { get; set; }
    [Parameter] public string InitialTitle { get; set; } = string.Empty;
    [Parameter] public string InitialNotes { get; set; } = string.Empty;

    public record SessionDetailsResult(DateTime Date, string Title, string Notes);

    private DateTime? _date;
    private string _title = string.Empty;
    private string _notes = string.Empty;

    protected override void OnInitialized()
    {
        _date = InitialDate ?? DateTime.Today;
        _title = InitialTitle;
        _notes = InitialNotes;
    }

    private void Submit()
    {
        if (string.IsNullOrWhiteSpace(_title) || _date == null) return;
        MudDialog.Close(new SessionDetailsResult(
            DateTime.SpecifyKind(_date.Value, DateTimeKind.Utc), _title, _notes));
    }
}
```

- [ ] **Step 2: Rewrite Sessions.razor**

Replace the entire contents of `src/FootballPlanner.Web/Pages/Sessions.razor`:

```razor
@page "/sessions"
@inject Services.ApiClient Api
@inject NavigationManager Nav
@inject IDialogService DialogService

<div class="d-flex justify-space-between align-center mb-4">
    <MudText Typo="Typo.h4">Sessions</MudText>
    <MudFab Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add" Size="Size.Small"
            OnClick="OpenCreateDialog" aria-label="New Session" />
</div>

@if (_sessions == null)
{
    <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
}
else if (_sessions.Count == 0)
{
    <MudPaper Class="pa-8 d-flex align-center justify-center" Elevation="0">
        <MudText Color="Color.Secondary">No sessions yet. Click + to create one.</MudText>
    </MudPaper>
}
else
{
    @foreach (var group in GroupedSessions)
    {
        <MudText Typo="Typo.subtitle1" Class="mb-2 mt-4 mud-text-secondary">@group.MonthKey</MudText>
        <MudGrid>
            @foreach (var session in group.Sessions)
            {
                <MudItem xs="12" sm="6" md="4">
                    <MudCard Elevation="2">
                        <MudCardContent>
                            <div class="d-flex justify-space-between align-center mb-1">
                                <MudText Typo="Typo.caption" Color="Color.Secondary">
                                    @session.Date.ToString("MMM d") &bull; @TotalDuration(session) min
                                </MudText>
                                <MudMenu Icon="@Icons.Material.Filled.MoreVert" Dense="true" Size="Size.Small">
                                    <MudMenuItem OnClick="() => OpenEditDialog(session)">Edit Details</MudMenuItem>
                                    <MudMenuItem OnClick="() => ConfirmDelete(session)">Delete</MudMenuItem>
                                    <MudMenuItem OnClick="() => Nav.NavigateTo($"/sessions/{session.Id}/run")">Run Session</MudMenuItem>
                                </MudMenu>
                            </div>
                            <MudText Typo="Typo.h6" Class="mb-2">@session.Title</MudText>
                            <div style="display:flex;gap:6px;overflow-x:auto;padding-bottom:4px;min-height:34px;">
                                @foreach (var sa in session.Activities.OrderBy(a => a.DisplayOrder))
                                {
                                    @if (sa.Activity?.DiagramJson != null)
                                    {
                                        <div style="flex-shrink:0;width:60px;height:38px;background:#388e3c;border-radius:4px;overflow:hidden;">
                                            @((MarkupString)sa.Activity.DiagramJson)
                                        </div>
                                    }
                                    else
                                    {
                                        <MudChip T="string" Size="Size.Small"
                                                 Style="flex-shrink:0;font-size:10px;"
                                                 Color="Color.Default">
                                            @(sa.Activity?.Name ?? "?")
                                        </MudChip>
                                    }
                                }
                            </div>
                        </MudCardContent>
                        <MudCardActions>
                            <MudButton Variant="Variant.Text" Color="Color.Primary"
                                       OnClick="() => Nav.NavigateTo($"/sessions/{session.Id}")">Open</MudButton>
                        </MudCardActions>
                    </MudCard>
                </MudItem>
            }
        </MudGrid>
    }
}

@code {
    private List<Services.ApiClient.SessionDto>? _sessions;

    protected override async Task OnInitializedAsync()
    {
        _sessions = await Api.GetSessionsAsync();
    }

    private IEnumerable<(string MonthKey, List<Services.ApiClient.SessionDto> Sessions)> GroupedSessions =>
        (_sessions ?? [])
            .OrderByDescending(s => s.Date)
            .GroupBy(s => s.Date.ToString("MMMM yyyy"))
            .Select(g => (g.Key, g.OrderByDescending(s => s.Date).ToList()));

    private static int TotalDuration(Services.ApiClient.SessionDto session) =>
        session.Activities.Sum(a => a.Duration);

    private async Task OpenCreateDialog()
    {
        var parameters = new DialogParameters<Dialogs.SessionDetailsDialog>
        {
            { x => x.IsEditing, false }
        };
        var dialog = await DialogService.ShowAsync<Dialogs.SessionDetailsDialog>("New Session", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            var data = (Dialogs.SessionDetailsDialog.SessionDetailsResult)result.Data!;
            await Api.CreateSessionAsync(new Services.ApiClient.CreateSessionRequest(
                data.Date, data.Title, string.IsNullOrWhiteSpace(data.Notes) ? null : data.Notes));
            _sessions = await Api.GetSessionsAsync();
        }
    }

    private async Task OpenEditDialog(Services.ApiClient.SessionDto session)
    {
        var parameters = new DialogParameters<Dialogs.SessionDetailsDialog>
        {
            { x => x.IsEditing, true },
            { x => x.InitialDate, session.Date },
            { x => x.InitialTitle, session.Title },
            { x => x.InitialNotes, session.Notes ?? string.Empty },
        };
        var dialog = await DialogService.ShowAsync<Dialogs.SessionDetailsDialog>("Edit Session", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            var data = (Dialogs.SessionDetailsDialog.SessionDetailsResult)result.Data!;
            await Api.UpdateSessionAsync(session.Id, new Services.ApiClient.UpdateSessionRequest(
                data.Date, data.Title, string.IsNullOrWhiteSpace(data.Notes) ? null : data.Notes));
            _sessions = await Api.GetSessionsAsync();
        }
    }

    private async Task ConfirmDelete(Services.ApiClient.SessionDto session)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Delete Session",
            $"Delete \"{session.Title}\"? This cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");
        if (confirmed == true)
        {
            await Api.DeleteSessionAsync(session.Id);
            _sessions = await Api.GetSessionsAsync();
        }
    }
}
```

- [ ] **Step 3: Update SessionJourney to use the new MudBlazor UI**

Replace the entire contents of `tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs`:

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

        await page.GetByRole(AriaRole.Button, new() { Name = "New Session" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.GetByLabel("Title").FillAsync(input.Title);
        await page.GetByLabel("Date").FillAsync(input.Date.ToString("MM/dd/yyyy"));
        await page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    public async Task NavigateToEditorAsync(string title)
    {
        await page.GotoAsync($"{FeatureTestFixture.BaseUrl}/sessions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var card = page.Locator(".mud-card").Filter(new() { HasText = title });
        await card.GetByRole(AriaRole.Button, new() { Name = "Open" }).ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 4: Fix broken table assertions in PlanningJourneyTests**

The Phases, Focuses, and Activities pages were converted from HTML tables to MudBlazor lists in Plan 1. The assertions in `PlanningJourneyTests` still reference `table`. Replace the entire contents of `tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs`:

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
        await Assertions.Expect(fixture.Page.GetByText("Warm Up")).ToBeVisibleAsync();

        await fixture.Page.GotoAsync($"{FeatureTestFixture.BaseUrl}/focuses");
        await fixture.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Assertions.Expect(fixture.Page.GetByText("Technique")).ToBeVisibleAsync();
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
        await Assertions.Expect(fixture.Page.GetByText("Rondo")).ToBeVisibleAsync();
        await Assertions.Expect(fixture.Page.GetByText("10 min")).ToBeVisibleAsync();
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
        await Assertions.Expect(fixture.Page.GetByText("Warm Up")).ToBeVisibleAsync();
    }
}
```

- [ ] **Step 5: Build to verify no compile errors**

```bash
dotnet build FootballPlanner.slnx
```

Expected: Build succeeds with no errors.

- [ ] **Step 6: Run unit tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests
```

Expected: All unit tests pass.

- [ ] **Step 7: Commit**

```bash
git add src/FootballPlanner.Web/Dialogs/SessionDetailsDialog.razor \
        src/FootballPlanner.Web/Pages/Sessions.razor \
        tests/FootballPlanner.Feature.Tests/Journeys/SessionJourney.cs \
        tests/FootballPlanner.Feature.Tests/Tests/PlanningJourneyTests.cs
git commit -m "feat: sessions list MudBlazor cards with create/edit/delete dialogs"
```

---

## Task 3: Session Editor (MudBlazor)

**Files:**
- Create: `src/FootballPlanner.Web/Dialogs/ActivityPickerDialog.razor`
- Modify: `src/FootballPlanner.Web/Pages/SessionEditor.razor`
- Modify: `tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs`

- [ ] **Step 1: Create the activity picker dialog**

Create `src/FootballPlanner.Web/Dialogs/ActivityPickerDialog.razor`:

```razor
<MudDialog Style="min-width:420px;">
    <TitleContent>
        <MudText Typo="Typo.h6">Add Activity</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_search" Label="Search activities" Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search" Variant="Variant.Outlined"
                      Class="mb-3" Immediate="true" />
        @if (!FilteredActivities.Any())
        {
            <MudText Color="Color.Secondary">No activities match your search.</MudText>
        }
        else
        {
            <MudList T="object" Dense="true" Style="max-height:300px;overflow-y:auto;">
                @foreach (var activity in FilteredActivities)
                {
                    <MudListItem T="object" OnClick="() => Select(activity)">
                        <div class="d-flex align-center gap-3">
                            <div style="width:40px;height:26px;background:#388e3c;border-radius:3px;flex-shrink:0;" />
                            <div>
                                <MudText Typo="Typo.body2">@activity.Name</MudText>
                                <MudText Typo="Typo.caption" Color="Color.Secondary">@activity.EstimatedDuration min</MudText>
                            </div>
                        </div>
                    </MudListItem>
                }
            </MudList>
        }
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="() => MudDialog.Cancel()">Cancel</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter] public List<Services.ApiClient.ActivityDto> Activities { get; set; } = [];

    private string _search = string.Empty;

    private IEnumerable<Services.ApiClient.ActivityDto> FilteredActivities =>
        Activities.Where(a => string.IsNullOrWhiteSpace(_search) ||
            a.Name.Contains(_search, StringComparison.OrdinalIgnoreCase));

    private void Select(Services.ApiClient.ActivityDto activity) =>
        MudDialog.Close(activity);
}
```

- [ ] **Step 2: Rewrite SessionEditor.razor**

Replace the entire contents of `src/FootballPlanner.Web/Pages/SessionEditor.razor`:

```razor
@page "/sessions/{Id:int}"
@inject Services.ApiClient Api
@inject NavigationManager Nav
@inject IDialogService DialogService

@if (_session == null)
{
    <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
}
else
{
    <div class="d-flex align-center gap-2 mb-4 flex-wrap">
        <MudIconButton Icon="@Icons.Material.Filled.ArrowBack"
                       OnClick='() => Nav.NavigateTo("/sessions")' />
        <MudText Typo="Typo.h5" Style="flex:1;min-width:120px;">@_session.Title</MudText>
        <MudText Typo="Typo.caption" Color="Color.Secondary">
            @_session.Date.ToString("MMM d, yyyy")
        </MudText>
        <MudIconButton Icon="@Icons.Material.Filled.Edit" title="Edit details"
                       OnClick="OpenEditDialog" />
        <MudButton Color="Color.Primary" Variant="Variant.Filled"
                   StartIcon="@Icons.Material.Filled.PlayArrow"
                   OnClick='() => Nav.NavigateTo($"/sessions/{Id}/run")'>Run Session</MudButton>
    </div>

    <MudGrid>
        <MudItem xs="12" sm="4">
            <MudPaper Class="pa-3" Elevation="1">
                <MudText Typo="Typo.h6" Class="mb-3">Activities</MudText>
                @if (!_session.Activities.Any())
                {
                    <MudText Color="Color.Secondary" Class="mb-3">
                        No activities yet. Click + to add one.
                    </MudText>
                }
                @foreach (var sa in _session.Activities.OrderBy(a => a.DisplayOrder))
                {
                    <MudPaper Class="@($"pa-2 mb-2 d-flex align-center gap-2{(_selectedActivityId == sa.Id ? " mud-theme-primary" : "")}")"
                              Elevation="1">
                        <div class="d-flex flex-column">
                            <MudIconButton Size="Size.Small" Icon="@Icons.Material.Filled.ArrowUpward"
                                           Disabled="@IsFirst(sa)" OnClick="() => MoveUp(sa)" />
                            <MudIconButton Size="Size.Small" Icon="@Icons.Material.Filled.ArrowDownward"
                                           Disabled="@IsLast(sa)" OnClick="() => MoveDown(sa)" />
                        </div>
                        <div class="flex-1" style="cursor:pointer;min-width:0;"
                             @onclick="() => SelectActivity(sa)">
                            <MudText Typo="Typo.body2" Style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;">
                                @(sa.Activity?.Name ?? "Unknown")
                            </MudText>
                            <MudText Typo="Typo.caption" Color="Color.Secondary">
                                @(sa.Phase?.Name ?? "") &bull; @sa.Duration min
                            </MudText>
                        </div>
                        <MudIconButton Size="Size.Small" Icon="@Icons.Material.Filled.Delete"
                                       Color="Color.Error" OnClick="() => RemoveActivity(sa.Id)" />
                    </MudPaper>
                }
                <div class="d-flex justify-center mt-2">
                    <MudFab Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add"
                            Size="Size.Small" OnClick="OpenActivityPicker"
                            aria-label="Add Activity" />
                </div>
            </MudPaper>
        </MudItem>

        <MudItem xs="12" sm="8">
            @if (_selectedActivityId == null)
            {
                <MudPaper Class="pa-4 d-flex align-center justify-center" Elevation="0">
                    <MudText Color="Color.Secondary">
                        Select an activity to edit its details
                    </MudText>
                </MudPaper>
            }
            else
            {
                <MudPaper Class="pa-4" Elevation="1">
                    <MudText Typo="Typo.h6" Class="mb-4">@_selectedActivityName</MudText>
                    <MudSelect T="int" Value="_editPhaseId"
                               ValueChanged="async v => { _editPhaseId = v; await AutoSaveActivity(); }"
                               Label="Phase" Variant="Variant.Outlined" Class="mb-3">
                        @foreach (var phase in _allPhases ?? [])
                        {
                            <MudSelectItem T="int" Value="@phase.Id">@phase.Name</MudSelectItem>
                        }
                    </MudSelect>
                    <MudSelect T="int" Value="_editFocusId"
                               ValueChanged="async v => { _editFocusId = v; await AutoSaveActivity(); }"
                               Label="Focus" Variant="Variant.Outlined" Class="mb-3">
                        @foreach (var focus in _allFocuses ?? [])
                        {
                            <MudSelectItem T="int" Value="@focus.Id">@focus.Name</MudSelectItem>
                        }
                    </MudSelect>
                    <MudNumericField T="int" Value="_editDuration"
                                     ValueChanged="async v => { _editDuration = v; await AutoSaveActivity(); }"
                                     Label="Duration (min)" Variant="Variant.Outlined"
                                     Min="1" Class="mb-3" />

                    <MudText Typo="Typo.subtitle2" Class="mb-2">Key Points</MudText>
                    @for (int i = 0; i < _editKeyPoints.Count; i++)
                    {
                        var idx = i;
                        <div class="d-flex align-center gap-2 mb-2">
                            <MudTextField @bind-Value="_editKeyPoints[idx]"
                                          Label="@($"Key Point {idx + 1}")"
                                          Variant="Variant.Outlined" Style="flex:1"
                                          OnBlur="AutoSaveKeyPoints" />
                            <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small"
                                           OnClick="() => RemoveKeyPoint(idx)" />
                        </div>
                    }
                    <MudButton Variant="Variant.Text" StartIcon="@Icons.Material.Filled.Add"
                               OnClick="AddKeyPoint" Class="mb-3">Add Key Point</MudButton>

                    <MudTextField @bind-Value="_editNotes" Label="Notes"
                                  Variant="Variant.Outlined" Lines="3"
                                  OnBlur="AutoSaveActivity" />
                </MudPaper>
            }
        </MudItem>
    </MudGrid>
}

@code {
    [Parameter] public int Id { get; set; }

    private Services.ApiClient.SessionDto? _session;
    private List<Services.ApiClient.ActivityDto>? _allActivities;
    private List<Services.ApiClient.PhaseDto>? _allPhases;
    private List<Services.ApiClient.FocusDto>? _allFocuses;

    private int? _selectedActivityId;
    private string? _selectedActivityName;
    private int _editPhaseId;
    private int _editFocusId;
    private int _editDuration;
    private string _editNotes = string.Empty;
    private List<string> _editKeyPoints = [];

    protected override async Task OnInitializedAsync()
    {
        await LoadAll();
    }

    private async Task LoadAll()
    {
        _session = await Api.GetSessionAsync(Id);
        _allActivities = await Api.GetActivitiesAsync();
        _allPhases = await Api.GetPhasesAsync();
        _allFocuses = await Api.GetFocusesAsync();
    }

    private void SelectActivity(Services.ApiClient.SessionActivityDto sa)
    {
        _selectedActivityId = sa.Id;
        _selectedActivityName = sa.Activity?.Name;
        _editPhaseId = sa.PhaseId;
        _editFocusId = sa.FocusId;
        _editDuration = sa.Duration;
        _editNotes = sa.Notes ?? string.Empty;
        _editKeyPoints = sa.KeyPoints.OrderBy(kp => kp.Order).Select(kp => kp.Text).ToList();
    }

    private bool IsFirst(Services.ApiClient.SessionActivityDto sa)
    {
        var sorted = _session!.Activities.OrderBy(a => a.DisplayOrder).ToList();
        return sorted.First().Id == sa.Id;
    }

    private bool IsLast(Services.ApiClient.SessionActivityDto sa)
    {
        var sorted = _session!.Activities.OrderBy(a => a.DisplayOrder).ToList();
        return sorted.Last().Id == sa.Id;
    }

    private async Task MoveUp(Services.ApiClient.SessionActivityDto sa)
    {
        var sorted = _session!.Activities.OrderBy(a => a.DisplayOrder).ToList();
        var idx = sorted.FindIndex(a => a.Id == sa.Id);
        if (idx == 0) return;

        var above = sorted[idx - 1];
        var items = sorted.Select(a =>
        {
            if (a.Id == sa.Id) return new Services.ApiClient.ReorderSessionActivityItem(a.Id, above.DisplayOrder);
            if (a.Id == above.Id) return new Services.ApiClient.ReorderSessionActivityItem(a.Id, sa.DisplayOrder);
            return new Services.ApiClient.ReorderSessionActivityItem(a.Id, a.DisplayOrder);
        }).ToList();

        await Api.ReorderSessionActivitiesAsync(Id, items);
        _session = await Api.GetSessionAsync(Id);
    }

    private async Task MoveDown(Services.ApiClient.SessionActivityDto sa)
    {
        var sorted = _session!.Activities.OrderBy(a => a.DisplayOrder).ToList();
        var idx = sorted.FindIndex(a => a.Id == sa.Id);
        if (idx == sorted.Count - 1) return;

        var below = sorted[idx + 1];
        var items = sorted.Select(a =>
        {
            if (a.Id == sa.Id) return new Services.ApiClient.ReorderSessionActivityItem(a.Id, below.DisplayOrder);
            if (a.Id == below.Id) return new Services.ApiClient.ReorderSessionActivityItem(a.Id, sa.DisplayOrder);
            return new Services.ApiClient.ReorderSessionActivityItem(a.Id, a.DisplayOrder);
        }).ToList();

        await Api.ReorderSessionActivitiesAsync(Id, items);
        _session = await Api.GetSessionAsync(Id);
    }

    private async Task RemoveActivity(int sessionActivityId)
    {
        await Api.RemoveSessionActivityAsync(Id, sessionActivityId);
        if (_selectedActivityId == sessionActivityId)
        {
            _selectedActivityId = null;
            _selectedActivityName = null;
        }
        _session = await Api.GetSessionAsync(Id);
    }

    private async Task OpenActivityPicker()
    {
        var parameters = new DialogParameters<Dialogs.ActivityPickerDialog>
        {
            { x => x.Activities, _allActivities ?? [] }
        };
        var dialog = await DialogService.ShowAsync<Dialogs.ActivityPickerDialog>("Add Activity", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            var activity = (Services.ApiClient.ActivityDto)result.Data!;
            var firstPhase = (_allPhases ?? []).MinBy(p => p.Order);
            var firstFocus = (_allFocuses ?? []).FirstOrDefault();
            if (firstPhase == null || firstFocus == null) return;
            await Api.AddSessionActivityAsync(Id, new Services.ApiClient.AddSessionActivityRequest(
                activity.Id, firstPhase.Id, firstFocus.Id, activity.EstimatedDuration, null));
            _session = await Api.GetSessionAsync(Id);
        }
    }

    private async Task OpenEditDialog()
    {
        if (_session == null) return;
        var parameters = new DialogParameters<Dialogs.SessionDetailsDialog>
        {
            { x => x.IsEditing, true },
            { x => x.InitialDate, _session.Date },
            { x => x.InitialTitle, _session.Title },
            { x => x.InitialNotes, _session.Notes ?? string.Empty },
        };
        var dialog = await DialogService.ShowAsync<Dialogs.SessionDetailsDialog>("Edit Session", parameters);
        var result = await dialog.Result;
        if (result is { Canceled: false })
        {
            var data = (Dialogs.SessionDetailsDialog.SessionDetailsResult)result.Data!;
            await Api.UpdateSessionAsync(Id, new Services.ApiClient.UpdateSessionRequest(
                data.Date, data.Title, string.IsNullOrWhiteSpace(data.Notes) ? null : data.Notes));
            _session = await Api.GetSessionAsync(Id);
        }
    }

    private async Task AutoSaveActivity()
    {
        if (_selectedActivityId == null) return;
        await Api.UpdateSessionActivityAsync(Id, _selectedActivityId.Value,
            new Services.ApiClient.UpdateSessionActivityRequest(
                _editPhaseId, _editFocusId, _editDuration,
                string.IsNullOrWhiteSpace(_editNotes) ? null : _editNotes));
        _session = await Api.GetSessionAsync(Id);
    }

    private async Task AutoSaveKeyPoints()
    {
        if (_selectedActivityId == null) return;
        var keyPoints = _editKeyPoints
            .Where(kp => !string.IsNullOrWhiteSpace(kp))
            .ToList();
        await Api.UpdateSessionActivityKeyPointsAsync(Id, _selectedActivityId.Value, keyPoints);
        _session = await Api.GetSessionAsync(Id);
    }

    private void AddKeyPoint()
    {
        _editKeyPoints.Add(string.Empty);
    }

    private async Task RemoveKeyPoint(int index)
    {
        _editKeyPoints.RemoveAt(index);
        await AutoSaveKeyPoints();
    }
}
```

- [ ] **Step 3: Update SessionEditorJourney to use the new UI**

Replace the entire contents of `tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs`:

```csharp
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Journeys;

/// <param name="ActivityName">Name of the activity to select from the picker dialog.</param>
/// <param name="ActivityEstimatedDuration">Not used in the new picker-based flow, kept for backwards compatibility.</param>
/// <param name="PhaseName">Not used in the new picker-based flow; defaults are assigned automatically.</param>
/// <param name="FocusName">Not used in the new picker-based flow; defaults are assigned automatically.</param>
/// <param name="SessionDuration">Not used in the new picker-based flow; activity's estimated duration is used.</param>
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

        await page.GetByRole(AriaRole.Button, new() { Name = "Add Activity" }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Click the activity row in the dialog's list
        await page.GetByRole(AriaRole.Dialog)
            .Locator(".mud-list-item")
            .Filter(new() { HasText = input.ActivityName })
            .ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
```

- [ ] **Step 4: Build and run unit tests**

```bash
dotnet build FootballPlanner.slnx
dotnet test tests/FootballPlanner.Unit.Tests
```

Expected: Build succeeds. All unit tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Dialogs/ActivityPickerDialog.razor \
        src/FootballPlanner.Web/Pages/SessionEditor.razor \
        tests/FootballPlanner.Feature.Tests/Journeys/SessionEditorJourney.cs
git commit -m "feat: session editor two-panel layout with activity picker dialog and auto-save"
```
