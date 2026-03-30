# Activity Library Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full CRUD for the Activity entity (name, description, optional inspiration URL, estimated duration in minutes, and a DiagramJson placeholder) with unit tests, integration tests, HTTP API, and Blazor management page with inline editing.

**Architecture:** Follows the established Phase/Focus CQRS pattern — domain entity with private setters and factory methods, MediatR commands/queries with FluentValidation validators running via `ValidationBehaviour`, thin Azure Functions HTTP triggers, and a Blazor WASM management page that calls the API through the existing `ApiClient`.

**Tech Stack:** .NET 10, MediatR, FluentValidation, EF Core (Azure SQL), xUnit (no FluentAssertions), Testcontainers, Blazor WebAssembly

---

## File Map

**New files:**
- `src/FootballPlanner.Domain/Entities/Activity.cs`
- `src/FootballPlanner.Application/Commands/Activity/CreateActivityCommand.cs`
- `src/FootballPlanner.Application/Commands/Activity/CreateActivityCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/Activity/CreateActivityCommandHandler.cs`
- `src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommand.cs`
- `src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommandHandler.cs`
- `src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommand.cs`
- `src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommandHandler.cs`
- `src/FootballPlanner.Application/Queries/Activity/GetAllActivitiesQuery.cs`
- `src/FootballPlanner.Application/Queries/Activity/GetAllActivitiesQueryHandler.cs`
- `src/FootballPlanner.Infrastructure/Configurations/ActivityConfiguration.cs`
- `tests/FootballPlanner.Unit.Tests/Activity/CreateActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Activity/UpdateActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Activity/DeleteActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Activity/GetAllActivitiesQueryTests.cs`
- `tests/FootballPlanner.Integration.Tests/Activity/ActivityIntegrationTests.cs`
- `src/FootballPlanner.Api/Functions/ActivityFunctions.cs`
- `src/FootballPlanner.Web/Pages/Activities.razor`

**Modified files:**
- `src/FootballPlanner.Infrastructure/AppDbContext.cs` — add `DbSet<Activity>` and `ActivityConfiguration`
- `src/FootballPlanner.Web/Services/ApiClient.cs` — add activity DTOs and methods

---

## Chunk 1: Activity Domain Entity, CQRS, Unit Tests

### Task 1: Activity domain entity

**Files:**
- Create: `src/FootballPlanner.Domain/Entities/Activity.cs`

- [ ] **Step 1: Write the failing test**

`tests/FootballPlanner.Unit.Tests/Activity/CreateActivityCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class CreateActivityCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsActivity_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(
            new CreateActivityCommand("Warm Up Rondo", "Players pass in a circle", null, 10));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Warm Up Rondo", result.Name);
        Assert.Equal("Players pass in a circle", result.Description);
        Assert.Equal(10, result.EstimatedDuration);
        Assert.Null(result.InspirationUrl);
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateActivityCommand("", "Description", null, 10)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenEstimatedDurationIsZero()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateActivityCommand("Name", "Description", null, 0)));
    }
}
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~CreateActivityCommandTests" 2>&1 | tail -10
```
Expected: FAIL (types not found yet).

- [ ] **Step 3: Create the Activity entity**

`src/FootballPlanner.Domain/Entities/Activity.cs`:
```csharp
namespace FootballPlanner.Domain.Entities;

public class Activity
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? InspirationUrl { get; private set; }
    public int EstimatedDuration { get; private set; }
    public string? DiagramJson { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Activity() { }

    public static Activity Create(
        string name, string description, string? inspirationUrl, int estimatedDuration)
        => new Activity
        {
            Name = name,
            Description = description,
            InspirationUrl = inspirationUrl,
            EstimatedDuration = estimatedDuration,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public void Update(
        string name, string description, string? inspirationUrl, int estimatedDuration)
    {
        Name = name;
        Description = description;
        InspirationUrl = inspirationUrl;
        EstimatedDuration = estimatedDuration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDiagram(string? diagramJson)
    {
        DiagramJson = diagramJson;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Task 2: Activity CQRS commands, queries, and unit tests

**Files (all new):**
- Create: `src/FootballPlanner.Application/Commands/Activity/CreateActivityCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Activity/CreateActivityCommandValidator.cs`
- Create: `src/FootballPlanner.Application/Commands/Activity/CreateActivityCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommandValidator.cs`
- Create: `src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Queries/Activity/GetAllActivitiesQuery.cs`
- Create: `src/FootballPlanner.Application/Queries/Activity/GetAllActivitiesQueryHandler.cs`
- Create: `src/FootballPlanner.Infrastructure/Configurations/ActivityConfiguration.cs`
- Modify: `src/FootballPlanner.Infrastructure/AppDbContext.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Activity/CreateActivityCommandTests.cs` (already done in Task 1)
- Create: `tests/FootballPlanner.Unit.Tests/Activity/UpdateActivityCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Activity/DeleteActivityCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Activity/GetAllActivitiesQueryTests.cs`

- [ ] **Step 1: Create the command and query types**

`src/FootballPlanner.Application/Commands/Activity/CreateActivityCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public record CreateActivityCommand(
    string Name,
    string Description,
    string? InspirationUrl,
    int EstimatedDuration) : IRequest<Domain.Entities.Activity>;
```

`src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public record UpdateActivityCommand(
    int Id,
    string Name,
    string Description,
    string? InspirationUrl,
    int EstimatedDuration) : IRequest<Domain.Entities.Activity>;
```

`src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public record DeleteActivityCommand(int Id) : IRequest;
```

`src/FootballPlanner.Application/Queries/Activity/GetAllActivitiesQuery.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Queries.Activity;

public record GetAllActivitiesQuery : IRequest<List<Domain.Entities.Activity>>;
```

- [ ] **Step 2: Create the validators**

`src/FootballPlanner.Application/Commands/Activity/CreateActivityCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Activity;

public class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.InspirationUrl).MaximumLength(500).When(x => x.InspirationUrl != null);
        RuleFor(x => x.EstimatedDuration).GreaterThan(0);
    }
}
```

`src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Activity;

public class UpdateActivityCommandValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.InspirationUrl).MaximumLength(500).When(x => x.InspirationUrl != null);
        RuleFor(x => x.EstimatedDuration).GreaterThan(0);
    }
}
```

`src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Activity;

public class DeleteActivityCommandValidator : AbstractValidator<DeleteActivityCommand>
{
    public DeleteActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
```

- [ ] **Step 3: Create the EF configuration and update AppDbContext**

`src/FootballPlanner.Infrastructure/Configurations/ActivityConfiguration.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Description).IsRequired().HasMaxLength(2000);
        builder.Property(a => a.InspirationUrl).HasMaxLength(500);
        builder.Property(a => a.EstimatedDuration).IsRequired();
        builder.Property(a => a.DiagramJson);
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
```

Modify `src/FootballPlanner.Infrastructure/AppDbContext.cs` — add the Activity DbSet and configuration:
```csharp
using FootballPlanner.Domain.Entities;
using FootballPlanner.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<Focus> Focuses => Set<Focus>();
    public DbSet<Activity> Activities => Set<Activity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PhaseConfiguration());
        modelBuilder.ApplyConfiguration(new FocusConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityConfiguration());
    }
}
```

- [ ] **Step 4: Create the handlers**

`src/FootballPlanner.Application/Commands/Activity/CreateActivityCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public class CreateActivityCommandHandler(AppDbContext db)
    : IRequestHandler<CreateActivityCommand, Domain.Entities.Activity>
{
    public async Task<Domain.Entities.Activity> Handle(
        CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = Domain.Entities.Activity.Create(
            request.Name, request.Description, request.InspirationUrl, request.EstimatedDuration);
        db.Activities.Add(activity);
        await db.SaveChangesAsync(cancellationToken);
        return activity;
    }
}
```

`src/FootballPlanner.Application/Commands/Activity/UpdateActivityCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public class UpdateActivityCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateActivityCommand, Domain.Entities.Activity>
{
    public async Task<Domain.Entities.Activity> Handle(
        UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Activity {request.Id} not found.");
        activity.Update(request.Name, request.Description, request.InspirationUrl, request.EstimatedDuration);
        await db.SaveChangesAsync(cancellationToken);
        return activity;
    }
}
```

`src/FootballPlanner.Application/Commands/Activity/DeleteActivityCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Activity;

public class DeleteActivityCommandHandler(AppDbContext db) : IRequestHandler<DeleteActivityCommand>
{
    public async Task Handle(DeleteActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await db.Activities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Activity {request.Id} not found.");
        db.Activities.Remove(activity);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

`src/FootballPlanner.Application/Queries/Activity/GetAllActivitiesQueryHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Queries.Activity;

public class GetAllActivitiesQueryHandler(AppDbContext db)
    : IRequestHandler<GetAllActivitiesQuery, List<Domain.Entities.Activity>>
{
    public async Task<List<Domain.Entities.Activity>> Handle(
        GetAllActivitiesQuery request, CancellationToken cancellationToken)
    {
        return await db.Activities
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 5: Write remaining unit tests**

`tests/FootballPlanner.Unit.Tests/Activity/UpdateActivityCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class UpdateActivityCommandTests
{
    [Fact]
    public async Task Send_UpdatesActivity_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateActivityCommand("Old Name", "Old desc", null, 10));

        var updated = await mediator.Send(
            new UpdateActivityCommand(created.Id, "New Name", "New desc", "https://example.com", 20));

        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New desc", updated.Description);
        Assert.Equal("https://example.com", updated.InspirationUrl);
        Assert.Equal(20, updated.EstimatedDuration);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateActivityCommand(99999, "Name", "Desc", null, 10)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateActivityCommand("Name", "Desc", null, 10));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new UpdateActivityCommand(created.Id, "", "Desc", null, 10)));
    }
}
```

`tests/FootballPlanner.Unit.Tests/Activity/DeleteActivityCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Queries.Activity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class DeleteActivityCommandTests
{
    [Fact]
    public async Task Send_DeletesActivity_WhenActivityExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateActivityCommand("To Delete", "Desc", null, 10));

        await mediator.Send(new DeleteActivityCommand(created.Id));

        var activities = await mediator.Send(new GetAllActivitiesQuery());
        Assert.DoesNotContain(activities, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new DeleteActivityCommand(99999)));
    }
}
```

`tests/FootballPlanner.Unit.Tests/Activity/GetAllActivitiesQueryTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Queries.Activity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Activity;

public class GetAllActivitiesQueryTests
{
    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoActivitiesExist()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var activities = await mediator.Send(new GetAllActivitiesQuery());

        Assert.NotNull(activities);
        Assert.Empty(activities);
    }

    [Fact]
    public async Task Send_ReturnsActivitiesOrderedByName()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreateActivityCommand("Zonal Marking", "Desc", null, 15));
        await mediator.Send(new CreateActivityCommand("Attacking Patterns", "Desc", null, 20));

        var activities = await mediator.Send(new GetAllActivitiesQuery());

        var names = activities.Select(a => a.Name).ToList();
        Assert.Equal(names.OrderBy(n => n).ToList(), names);
    }
}
```

- [ ] **Step 6: Run all unit tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -10
```
Expected: All tests pass (existing Phase/Focus tests plus new Activity tests).

- [ ] **Step 7: Commit**

```bash
git add src/FootballPlanner.Domain/Entities/Activity.cs \
        src/FootballPlanner.Application/Commands/Activity/ \
        src/FootballPlanner.Application/Queries/Activity/ \
        src/FootballPlanner.Infrastructure/Configurations/ActivityConfiguration.cs \
        src/FootballPlanner.Infrastructure/AppDbContext.cs \
        tests/FootballPlanner.Unit.Tests/Activity/
git commit -m "feat: add Activity entity with CQRS commands, queries, and unit tests"
```

---

## Chunk 2: EF Migration, Integration Tests, API Functions

### Task 3: EF Core migration

**Files:**
- New migration file generated under `src/FootballPlanner.Infrastructure/Migrations/`

- [ ] **Step 1: Add the migration**

```bash
dotnet ef migrations add AddActivity \
  --project src/FootballPlanner.Infrastructure \
  --startup-project src/FootballPlanner.Infrastructure
```
Expected: A new migration file created in `src/FootballPlanner.Infrastructure/Migrations/`.

- [ ] **Step 2: Verify the migration creates the Activities table**

Open the generated migration file and confirm it contains `CreateTable` for `Activities` with columns: `Id`, `Name` (nvarchar(200)), `Description` (nvarchar(2000)), `InspirationUrl` (nvarchar(500) nullable), `EstimatedDuration` (int), `DiagramJson` (nullable nvarchar(max)), `CreatedAt` (datetime2), `UpdatedAt` (datetime2).

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Infrastructure/Migrations/
git commit -m "feat: add EF Core migration for Activity table"
```

---

### Task 4: Integration tests

**Files:**
- Create: `tests/FootballPlanner.Integration.Tests/Activity/ActivityIntegrationTests.cs`

- [ ] **Step 1: Write the integration tests**

`tests/FootballPlanner.Integration.Tests/Activity/ActivityIntegrationTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Queries.Activity;
using FootballPlanner.Integration.Tests.Infrastructure;

namespace FootballPlanner.Integration.Tests.Activity;

public class ActivityIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrieveActivity_RoundTrip()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("Integration Pressing Drill", "A drill for tests", null, 20));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());

        Assert.Contains(activities, a => a.Id == created.Id && a.Name == "Integration Pressing Drill");
    }

    [Fact]
    public async Task UpdateActivity_PersistsChanges()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("Old Name", "Old description", null, 15));

        await app.Mediator.Send(
            new UpdateActivityCommand(created.Id, "New Name", "New description", "https://example.com", 25));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());
        var updated = activities.First(a => a.Id == created.Id);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal(25, updated.EstimatedDuration);
    }

    [Fact]
    public async Task DeleteActivity_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(
            new CreateActivityCommand("To Delete Activity", "Description", null, 10));

        await app.Mediator.Send(new DeleteActivityCommand(created.Id));

        var activities = await app.Mediator.Send(new GetAllActivitiesQuery());
        Assert.DoesNotContain(activities, a => a.Id == created.Id);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add tests/FootballPlanner.Integration.Tests/Activity/
git commit -m "feat: add Activity integration tests using TestApplication"
```

---

### Task 5: ActivityFunctions HTTP API

**Files:**
- Create: `src/FootballPlanner.Api/Functions/ActivityFunctions.cs`

- [ ] **Step 1: Create ActivityFunctions**

`src/FootballPlanner.Api/Functions/ActivityFunctions.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Queries.Activity;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FootballPlanner.Api.Functions;

public class ActivityFunctions(IMediator mediator)
{
    [Function("GetActivities")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "activities")] HttpRequestData req)
    {
        var activities = await mediator.Send(new GetAllActivitiesQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(activities);
        return response;
    }

    [Function("CreateActivity")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "activities")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreateActivityCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var activity = await mediator.Send(command);
        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteAsJsonAsync(activity);
        return response;
    }

    [Function("UpdateActivity")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "activities/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdateActivityRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateActivityCommand(
            id, body.Name, body.Description, body.InspirationUrl, body.EstimatedDuration));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    [Function("DeleteActivity")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "activities/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeleteActivityCommand(id));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    private record UpdateActivityRequest(
        string Name, string Description, string? InspirationUrl, int EstimatedDuration);
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/FootballPlanner.Api 2>&1 | tail -5
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Api/Functions/ActivityFunctions.cs
git commit -m "feat: add Activity HTTP Azure Functions"
```

---

## Chunk 3: Blazor Activity Management Page

### Task 6: Blazor Activities page and ApiClient update

**Files:**
- Modify: `src/FootballPlanner.Web/Services/ApiClient.cs`
- Create: `src/FootballPlanner.Web/Pages/Activities.razor`

- [ ] **Step 1: Add activity types and methods to ApiClient**

In `src/FootballPlanner.Web/Services/ApiClient.cs`, add the following alongside the existing Phase and Focus members:

```csharp
// Add these methods to the ApiClient class body:

public Task<List<ActivityDto>?> GetActivitiesAsync() =>
    http.GetFromJsonAsync<List<ActivityDto>>("activities");

public Task<HttpResponseMessage> CreateActivityAsync(CreateActivityRequest request) =>
    http.PostAsJsonAsync("activities", request);

public Task<HttpResponseMessage> UpdateActivityAsync(int id, UpdateActivityRequest request) =>
    http.PutAsJsonAsync($"activities/{id}", request);

public Task<HttpResponseMessage> DeleteActivityAsync(int id) =>
    http.DeleteAsync($"activities/{id}");

// Add these records alongside the existing PhaseDto, FocusDto etc.:

public record ActivityDto(
    int Id,
    string Name,
    string Description,
    string? InspirationUrl,
    int EstimatedDuration,
    string? DiagramJson,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateActivityRequest(
    string Name, string Description, string? InspirationUrl, int EstimatedDuration);

public record UpdateActivityRequest(
    string Name, string Description, string? InspirationUrl, int EstimatedDuration);
```

The complete updated `ApiClient.cs` should look like:

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

    public Task<HttpResponseMessage> CreateActivityAsync(CreateActivityRequest request) =>
        http.PostAsJsonAsync("activities", request);

    public Task<HttpResponseMessage> UpdateActivityAsync(int id, UpdateActivityRequest request) =>
        http.PutAsJsonAsync($"activities/{id}", request);

    public Task<HttpResponseMessage> DeleteActivityAsync(int id) =>
        http.DeleteAsync($"activities/{id}");

    public record PhaseDto(int Id, string Name, int Order);
    public record FocusDto(int Id, string Name);
    public record ActivityDto(
        int Id,
        string Name,
        string Description,
        string? InspirationUrl,
        int EstimatedDuration,
        string? DiagramJson,
        DateTime CreatedAt,
        DateTime UpdatedAt);
    public record CreatePhaseRequest(string Name, int Order);
    public record UpdatePhaseRequest(string Name, int Order);
    public record CreateFocusRequest(string Name);
    public record UpdateFocusRequest(string Name);
    public record CreateActivityRequest(
        string Name, string Description, string? InspirationUrl, int EstimatedDuration);
    public record UpdateActivityRequest(
        string Name, string Description, string? InspirationUrl, int EstimatedDuration);
}
```

- [ ] **Step 2: Create the Activities page**

`src/FootballPlanner.Web/Pages/Activities.razor`:
```razor
@page "/activities"
@inject Services.ApiClient Api

<h1>Activity Library</h1>

@if (editingId == null)
{
    <div>
        <input @bind="newName" placeholder="Name" />
        <textarea @bind="newDescription" placeholder="Description"></textarea>
        <input @bind="newInspirationUrl" placeholder="Inspiration URL (optional)" />
        <input @bind="newEstimatedDuration" type="number" placeholder="Duration (mins)" />
        <button @onclick="AddActivity">Add</button>
    </div>

    @if (activities == null)
    {
        <p>Loading...</p>
    }
    else
    {
        <table>
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Description</th>
                    <th>Duration</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                @foreach (var activity in activities)
                {
                    <tr>
                        <td>@activity.Name</td>
                        <td>@activity.Description</td>
                        <td>@activity.EstimatedDuration min</td>
                        <td>
                            <button @onclick="() => StartEdit(activity)">Edit</button>
                            <button @onclick="() => DeleteActivity(activity.Id)">Delete</button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
}
else
{
    <h2>Edit Activity</h2>
    <input @bind="editName" placeholder="Name" />
    <textarea @bind="editDescription" placeholder="Description"></textarea>
    <input @bind="editInspirationUrl" placeholder="Inspiration URL (optional)" />
    <input @bind="editEstimatedDuration" type="number" placeholder="Duration (mins)" />
    <button @onclick="SaveEdit">Save</button>
    <button @onclick="CancelEdit">Cancel</button>
}

@code {
    private List<Services.ApiClient.ActivityDto>? activities;

    // Create form state
    private string newName = string.Empty;
    private string newDescription = string.Empty;
    private string newInspirationUrl = string.Empty;
    private int newEstimatedDuration = 30;

    // Edit form state
    private int? editingId;
    private string editName = string.Empty;
    private string editDescription = string.Empty;
    private string editInspirationUrl = string.Empty;
    private int editEstimatedDuration = 30;

    protected override async Task OnInitializedAsync()
    {
        activities = await Api.GetActivitiesAsync();
    }

    private async Task AddActivity()
    {
        if (string.IsNullOrWhiteSpace(newName)) return;
        await Api.CreateActivityAsync(new Services.ApiClient.CreateActivityRequest(
            newName,
            newDescription,
            string.IsNullOrWhiteSpace(newInspirationUrl) ? null : newInspirationUrl,
            newEstimatedDuration));
        activities = await Api.GetActivitiesAsync();
        newName = string.Empty;
        newDescription = string.Empty;
        newInspirationUrl = string.Empty;
        newEstimatedDuration = 30;
    }

    private void StartEdit(Services.ApiClient.ActivityDto activity)
    {
        editingId = activity.Id;
        editName = activity.Name;
        editDescription = activity.Description;
        editInspirationUrl = activity.InspirationUrl ?? string.Empty;
        editEstimatedDuration = activity.EstimatedDuration;
    }

    private async Task SaveEdit()
    {
        if (editingId == null || string.IsNullOrWhiteSpace(editName)) return;
        await Api.UpdateActivityAsync(editingId.Value, new Services.ApiClient.UpdateActivityRequest(
            editName,
            editDescription,
            string.IsNullOrWhiteSpace(editInspirationUrl) ? null : editInspirationUrl,
            editEstimatedDuration));
        activities = await Api.GetActivitiesAsync();
        CancelEdit();
    }

    private void CancelEdit()
    {
        editingId = null;
        editName = string.Empty;
        editDescription = string.Empty;
        editInspirationUrl = string.Empty;
        editEstimatedDuration = 30;
    }

    private async Task DeleteActivity(int id)
    {
        await Api.DeleteActivityAsync(id);
        activities = await Api.GetActivitiesAsync();
    }
}
```

- [ ] **Step 3: Build the Web project**

```bash
dotnet build src/FootballPlanner.Web 2>&1 | tail -5
```
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/FootballPlanner.Web/Services/ApiClient.cs \
        src/FootballPlanner.Web/Pages/Activities.razor
git commit -m "feat: add Blazor Activity Library management page with inline editing"
```

---

## Summary

After completing all tasks, you will have:

1. **`Activity` domain entity** with private setters, `Create()` factory, `Update()` and `UpdateDiagram()` mutators
2. **CQRS layer** — `CreateActivityCommand`, `UpdateActivityCommand`, `DeleteActivityCommand`, `GetAllActivitiesQuery` with handlers and validators
3. **EF migration** adding the `Activities` table
4. **14+ unit tests** covering valid operations and validation failures
5. **3 integration tests** against real SQL Server via Testcontainers
6. **4 Azure Functions** endpoints: `GET /activities`, `POST /activities`, `PUT /activities/{id}`, `DELETE /activities/{id}`
7. **Blazor Activities page** with create form, edit form, and delete — inline, no code-behind
