# Session Management Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement full Session management — create/edit/delete sessions, add/edit/remove session activities with phase, focus, duration, notes, and key points — with unit tests, integration tests, HTTP API, and Blazor pages.

**Architecture:** Follows the established Phase/Focus/Activity CQRS pattern. Three new domain entities (Session, SessionActivity, SessionActivityKeyPoint) with private setters and factory methods. Session CQRS handles session headers; separate SessionActivity commands handle adding/updating/removing activities within a session. Blazor has two pages: `/sessions` (list) and `/sessions/{id}` (full editor).

**Tech Stack:** .NET 10, MediatR, FluentValidation, EF Core (Azure SQL), xUnit (no FluentAssertions), Testcontainers, Blazor WebAssembly

---

## File Map

**New files:**
- `src/FootballPlanner.Domain/Entities/Session.cs`
- `src/FootballPlanner.Domain/Entities/SessionActivity.cs`
- `src/FootballPlanner.Domain/Entities/SessionActivityKeyPoint.cs`
- `src/FootballPlanner.Application/Commands/Session/CreateSessionCommand.cs`
- `src/FootballPlanner.Application/Commands/Session/CreateSessionCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/Session/CreateSessionCommandHandler.cs`
- `src/FootballPlanner.Application/Commands/Session/UpdateSessionCommand.cs`
- `src/FootballPlanner.Application/Commands/Session/UpdateSessionCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/Session/UpdateSessionCommandHandler.cs`
- `src/FootballPlanner.Application/Commands/Session/DeleteSessionCommand.cs`
- `src/FootballPlanner.Application/Commands/Session/DeleteSessionCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/Session/DeleteSessionCommandHandler.cs`
- `src/FootballPlanner.Application/Queries/Session/GetAllSessionsQuery.cs`
- `src/FootballPlanner.Application/Queries/Session/GetAllSessionsQueryHandler.cs`
- `src/FootballPlanner.Application/Queries/Session/GetSessionByIdQuery.cs`
- `src/FootballPlanner.Application/Queries/Session/GetSessionByIdQueryHandler.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/AddSessionActivityCommand.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/AddSessionActivityCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/AddSessionActivityCommandHandler.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityCommand.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityCommandHandler.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/RemoveSessionActivityCommand.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/RemoveSessionActivityCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/RemoveSessionActivityCommandHandler.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityKeyPointsCommand.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityKeyPointsCommandValidator.cs`
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityKeyPointsCommandHandler.cs`
- `src/FootballPlanner.Infrastructure/Configurations/SessionConfiguration.cs`
- `src/FootballPlanner.Infrastructure/Configurations/SessionActivityConfiguration.cs`
- `src/FootballPlanner.Infrastructure/Configurations/SessionActivityKeyPointConfiguration.cs`
- `tests/FootballPlanner.Unit.Tests/Session/CreateSessionCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Session/UpdateSessionCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Session/DeleteSessionCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Session/GetAllSessionsQueryTests.cs`
- `tests/FootballPlanner.Unit.Tests/Session/GetSessionByIdQueryTests.cs`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/AddSessionActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/UpdateSessionActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/RemoveSessionActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/UpdateSessionActivityKeyPointsCommandTests.cs`
- `tests/FootballPlanner.Integration.Tests/Session/SessionIntegrationTests.cs`
- `src/FootballPlanner.Api/Functions/SessionFunctions.cs`
- `src/FootballPlanner.Web/Pages/Sessions.razor`
- `src/FootballPlanner.Web/Pages/SessionEditor.razor`

**Modified files:**
- `src/FootballPlanner.Infrastructure/AppDbContext.cs` — add DbSets for Session, SessionActivity, SessionActivityKeyPoint
- `src/FootballPlanner.Web/Services/ApiClient.cs` — add session DTOs and methods

---

## Chunk 1: Domain Entities and Session CQRS

### Task 1: Domain entities

**Files:**
- Create: `src/FootballPlanner.Domain/Entities/Session.cs`
- Create: `src/FootballPlanner.Domain/Entities/SessionActivity.cs`
- Create: `src/FootballPlanner.Domain/Entities/SessionActivityKeyPoint.cs`

- [ ] **Step 1: Write the failing test**

Create `tests/FootballPlanner.Unit.Tests/Session/CreateSessionCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class CreateSessionCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsSession_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var date = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = await mediator.Send(new CreateSessionCommand(date, "Tuesday U10s", null));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(date, result.Date);
        Assert.Equal("Tuesday U10s", result.Title);
        Assert.Null(result.Notes);
        Assert.True(result.CreatedAt > DateTime.MinValue);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenTitleIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "", null)));
    }
}
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~CreateSessionCommandTests" 2>&1 | tail -5
```
Expected: FAIL (types not found yet).

- [ ] **Step 3: Create the domain entities**

`src/FootballPlanner.Domain/Entities/Session.cs`:
```csharp
namespace FootballPlanner.Domain.Entities;

public class Session
{
    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public List<SessionActivity> Activities { get; private set; } = new();

    private Session() { }

    public static Session Create(DateTime date, string title, string? notes)
        => new Session
        {
            Date = date,
            Title = title,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public void Update(DateTime date, string title, string? notes)
    {
        Date = date;
        Title = title;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

`src/FootballPlanner.Domain/Entities/SessionActivity.cs`:
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
}
```

`src/FootballPlanner.Domain/Entities/SessionActivityKeyPoint.cs`:
```csharp
namespace FootballPlanner.Domain.Entities;

public class SessionActivityKeyPoint
{
    public int Id { get; private set; }
    public int SessionActivityId { get; private set; }
    public int Order { get; private set; }
    public string Text { get; private set; } = string.Empty;

    private SessionActivityKeyPoint() { }

    public static SessionActivityKeyPoint Create(int sessionActivityId, int order, string text)
        => new SessionActivityKeyPoint
        {
            SessionActivityId = sessionActivityId,
            Order = order,
            Text = text,
        };
}
```

- [ ] **Step 4: Commit**

```bash
git add src/FootballPlanner.Domain/Entities/Session.cs \
        src/FootballPlanner.Domain/Entities/SessionActivity.cs \
        src/FootballPlanner.Domain/Entities/SessionActivityKeyPoint.cs \
        tests/FootballPlanner.Unit.Tests/Session/CreateSessionCommandTests.cs
git commit -m "feat: add Session, SessionActivity, SessionActivityKeyPoint domain entities"
```

---

### Task 2: Session CQRS commands, queries, validators, handlers, and unit tests

**Files (all new):**
- `src/FootballPlanner.Application/Commands/Session/CreateSessionCommand.cs` + Validator + Handler
- `src/FootballPlanner.Application/Commands/Session/UpdateSessionCommand.cs` + Validator + Handler
- `src/FootballPlanner.Application/Commands/Session/DeleteSessionCommand.cs` + Validator + Handler
- `src/FootballPlanner.Application/Queries/Session/GetAllSessionsQuery.cs` + Handler
- `src/FootballPlanner.Application/Queries/Session/GetSessionByIdQuery.cs` + Handler
- `src/FootballPlanner.Infrastructure/Configurations/SessionConfiguration.cs`
- Modify: `src/FootballPlanner.Infrastructure/AppDbContext.cs`
- `tests/FootballPlanner.Unit.Tests/Session/CreateSessionCommandTests.cs` (already started)
- `tests/FootballPlanner.Unit.Tests/Session/UpdateSessionCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Session/DeleteSessionCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/Session/GetAllSessionsQueryTests.cs`
- `tests/FootballPlanner.Unit.Tests/Session/GetSessionByIdQueryTests.cs`

- [ ] **Step 1: Create the command and query types**

`src/FootballPlanner.Application/Commands/Session/CreateSessionCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public record CreateSessionCommand(
    DateTime Date,
    string Title,
    string? Notes) : IRequest<Domain.Entities.Session>;
```

`src/FootballPlanner.Application/Commands/Session/UpdateSessionCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public record UpdateSessionCommand(
    int Id,
    DateTime Date,
    string Title,
    string? Notes) : IRequest<Domain.Entities.Session>;
```

`src/FootballPlanner.Application/Commands/Session/DeleteSessionCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public record DeleteSessionCommand(int Id) : IRequest;
```

`src/FootballPlanner.Application/Queries/Session/GetAllSessionsQuery.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Queries.Session;

public record GetAllSessionsQuery : IRequest<List<Domain.Entities.Session>>;
```

`src/FootballPlanner.Application/Queries/Session/GetSessionByIdQuery.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Queries.Session;

public record GetSessionByIdQuery(int Id) : IRequest<Domain.Entities.Session?>;
```

- [ ] **Step 2: Create the validators**

`src/FootballPlanner.Application/Commands/Session/CreateSessionCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Session;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
```

`src/FootballPlanner.Application/Commands/Session/UpdateSessionCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Session;

public class UpdateSessionCommandValidator : AbstractValidator<UpdateSessionCommand>
{
    public UpdateSessionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
```

`src/FootballPlanner.Application/Commands/Session/DeleteSessionCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Session;

public class DeleteSessionCommandValidator : AbstractValidator<DeleteSessionCommand>
{
    public DeleteSessionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
```

- [ ] **Step 3: Create EF configuration and update AppDbContext**

`src/FootballPlanner.Infrastructure/Configurations/SessionConfiguration.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("Sessions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Date).IsRequired();
        builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.UpdatedAt).IsRequired();
        builder.HasMany(s => s.Activities)
            .WithOne()
            .HasForeignKey(sa => sa.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Read `src/FootballPlanner.Infrastructure/AppDbContext.cs` first, then add `DbSet<Session>` and `ApplyConfiguration(new SessionConfiguration())` alongside the existing Phase, Focus, Activity entries:

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
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionActivity> SessionActivities => Set<SessionActivity>();
    public DbSet<SessionActivityKeyPoint> SessionActivityKeyPoints => Set<SessionActivityKeyPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PhaseConfiguration());
        modelBuilder.ApplyConfiguration(new FocusConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new SessionActivityConfiguration());
        modelBuilder.ApplyConfiguration(new SessionActivityKeyPointConfiguration());
    }
}
```

Note: `SessionActivityConfiguration` and `SessionActivityKeyPointConfiguration` are created in Task 4. For now, add the DbSets and the Apply calls — the build will fail until Task 4 adds those configuration classes. That's fine; we'll fix it in Task 4.

Actually, to keep the build green: only add `DbSet<Session>` and `ApplyConfiguration(new SessionConfiguration())` now. Task 4 will add the rest. Rewrite the AppDbContext step accordingly:

Add only to AppDbContext now:
```csharp
public DbSet<Session> Sessions => Set<Session>();
```
And in `OnModelCreating`:
```csharp
modelBuilder.ApplyConfiguration(new SessionConfiguration());
```

- [ ] **Step 4: Create the handlers**

`src/FootballPlanner.Application/Commands/Session/CreateSessionCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public class CreateSessionCommandHandler(AppDbContext db)
    : IRequestHandler<CreateSessionCommand, Domain.Entities.Session>
{
    public async Task<Domain.Entities.Session> Handle(
        CreateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = Domain.Entities.Session.Create(request.Date, request.Title, request.Notes);
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
}
```

`src/FootballPlanner.Application/Commands/Session/UpdateSessionCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public class UpdateSessionCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateSessionCommand, Domain.Entities.Session>
{
    public async Task<Domain.Entities.Session> Handle(
        UpdateSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Session {request.Id} not found.");
        session.Update(request.Date, request.Title, request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }
}
```

`src/FootballPlanner.Application/Commands/Session/DeleteSessionCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Session;

public class DeleteSessionCommandHandler(AppDbContext db) : IRequestHandler<DeleteSessionCommand>
{
    public async Task Handle(DeleteSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Session {request.Id} not found.");
        db.Sessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

`src/FootballPlanner.Application/Queries/Session/GetAllSessionsQueryHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Queries.Session;

public class GetAllSessionsQueryHandler(AppDbContext db)
    : IRequestHandler<GetAllSessionsQuery, List<Domain.Entities.Session>>
{
    public async Task<List<Domain.Entities.Session>> Handle(
        GetAllSessionsQuery request, CancellationToken cancellationToken)
    {
        return await db.Sessions
            .OrderByDescending(s => s.Date)
            .ToListAsync(cancellationToken);
    }
}
```

`src/FootballPlanner.Application/Queries/Session/GetSessionByIdQueryHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Queries.Session;

public class GetSessionByIdQueryHandler(AppDbContext db)
    : IRequestHandler<GetSessionByIdQuery, Domain.Entities.Session?>
{
    public async Task<Domain.Entities.Session?> Handle(
        GetSessionByIdQuery request, CancellationToken cancellationToken)
    {
        return await db.Sessions
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.Activity)
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.Phase)
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.Focus)
            .Include(s => s.Activities.OrderBy(sa => sa.DisplayOrder))
                .ThenInclude(sa => sa.KeyPoints.OrderBy(kp => kp.Order))
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
    }
}
```

- [ ] **Step 5: Write remaining unit tests**

`tests/FootballPlanner.Unit.Tests/Session/UpdateSessionCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class UpdateSessionCommandTests
{
    [Fact]
    public async Task Send_UpdatesSession_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), "Old Title", null));
        var newDate = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);

        var updated = await mediator.Send(
            new UpdateSessionCommand(created.Id, newDate, "New Title", "Some notes"));

        Assert.Equal("New Title", updated.Title);
        Assert.Equal(newDate, updated.Date);
        Assert.Equal("Some notes", updated.Notes);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateSessionCommand(99999, DateTime.UtcNow, "Title", null)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenTitleIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "Title", null));
        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new UpdateSessionCommand(created.Id, DateTime.UtcNow, "", null)));
    }
}
```

`tests/FootballPlanner.Unit.Tests/Session/DeleteSessionCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Queries.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class DeleteSessionCommandTests
{
    [Fact]
    public async Task Send_DeletesSession_WhenSessionExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "To Delete", null));

        await mediator.Send(new DeleteSessionCommand(created.Id));

        var sessions = await mediator.Send(new GetAllSessionsQuery());
        Assert.DoesNotContain(sessions, s => s.Id == created.Id);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new DeleteSessionCommand(99999)));
    }
}
```

`tests/FootballPlanner.Unit.Tests/Session/GetAllSessionsQueryTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Queries.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class GetAllSessionsQueryTests
{
    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoSessionsExist()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var sessions = await mediator.Send(new GetAllSessionsQuery());
        Assert.NotNull(sessions);
        Assert.Empty(sessions);
    }

    [Fact]
    public async Task Send_ReturnsSessionsOrderedByDateDescending()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreateSessionCommand(
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), "March Session", null));
        await mediator.Send(new CreateSessionCommand(
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), "April Session", null));

        var sessions = await mediator.Send(new GetAllSessionsQuery());

        Assert.Equal("April Session", sessions[0].Title);
        Assert.Equal("March Session", sessions[1].Title);
    }
}
```

`tests/FootballPlanner.Unit.Tests/Session/GetSessionByIdQueryTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Queries.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.Session;

public class GetSessionByIdQueryTests
{
    [Fact]
    public async Task Send_ReturnsSession_WhenSessionExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "My Session", "Some notes"));

        var result = await mediator.Send(new GetSessionByIdQuery(created.Id));

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal("My Session", result.Title);
        Assert.Equal("Some notes", result.Notes);
        Assert.Empty(result.Activities);
    }

    [Fact]
    public async Task Send_ReturnsNull_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var result = await mediator.Send(new GetSessionByIdQuery(99999));
        Assert.Null(result);
    }
}
```

- [ ] **Step 6: Run all unit tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -10
```
Expected: All tests pass (existing Activity/Phase/Focus tests plus new Session tests).

- [ ] **Step 7: Commit**

```bash
git add src/FootballPlanner.Application/Commands/Session/ \
        src/FootballPlanner.Application/Queries/Session/ \
        src/FootballPlanner.Infrastructure/Configurations/SessionConfiguration.cs \
        src/FootballPlanner.Infrastructure/AppDbContext.cs \
        tests/FootballPlanner.Unit.Tests/Session/
git commit -m "feat: add Session CQRS commands, queries, validators, handlers, and unit tests"
```

---

## Chunk 2: SessionActivity CQRS and EF Migration

### Task 3: SessionActivity CQRS, validators, handlers, and unit tests

**Files (all new):**
- `src/FootballPlanner.Application/Commands/SessionActivity/AddSessionActivityCommand.cs` + Validator + Handler
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityCommand.cs` + Validator + Handler
- `src/FootballPlanner.Application/Commands/SessionActivity/RemoveSessionActivityCommand.cs` + Validator + Handler
- `src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityKeyPointsCommand.cs` + Validator + Handler
- `tests/FootballPlanner.Unit.Tests/SessionActivity/AddSessionActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/UpdateSessionActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/RemoveSessionActivityCommandTests.cs`
- `tests/FootballPlanner.Unit.Tests/SessionActivity/UpdateSessionActivityKeyPointsCommandTests.cs`

**Context:** SessionActivity tests need prerequisite data (Session, Activity, Phase, Focus). Create them inline in each test using `CreateSessionCommand`, `CreateActivityCommand`, `CreatePhaseCommand`, `CreateFocusCommand`.

- [ ] **Step 1: Write the failing test**

Create `tests/FootballPlanner.Unit.Tests/SessionActivity/AddSessionActivityCommandTests.cs` with just the first test (the full test class is written in Step 6):
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class AddSessionActivityCommandTests
{
    [Fact]
    public async Task Send_AddsSessionActivity_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test Session", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Circle passing", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        var result = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 15, null));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(session.Id, result.SessionId);
        Assert.Equal(15, result.Duration);
        Assert.Equal(1, result.DisplayOrder);
    }
}
```

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~AddSessionActivityCommandTests" 2>&1 | tail -5
```
Expected: FAIL (types not found yet).

- [ ] **Step 3: Create command types**

`src/FootballPlanner.Application/Commands/SessionActivity/AddSessionActivityCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public record AddSessionActivityCommand(
    int SessionId,
    int ActivityId,
    int PhaseId,
    int FocusId,
    int Duration,
    string? Notes) : IRequest<Domain.Entities.SessionActivity>;
```

`src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public record UpdateSessionActivityCommand(
    int Id,
    int PhaseId,
    int FocusId,
    int Duration,
    string? Notes) : IRequest<Domain.Entities.SessionActivity>;
```

`src/FootballPlanner.Application/Commands/SessionActivity/RemoveSessionActivityCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public record RemoveSessionActivityCommand(int Id) : IRequest;
```

`src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityKeyPointsCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public record UpdateSessionActivityKeyPointsCommand(
    int SessionActivityId,
    List<string> KeyPoints) : IRequest;
```

- [ ] **Step 4: Create validators**

`src/FootballPlanner.Application/Commands/SessionActivity/AddSessionActivityCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class AddSessionActivityCommandValidator : AbstractValidator<AddSessionActivityCommand>
{
    public AddSessionActivityCommandValidator()
    {
        RuleFor(x => x.SessionId).GreaterThan(0);
        RuleFor(x => x.ActivityId).GreaterThan(0);
        RuleFor(x => x.PhaseId).GreaterThan(0);
        RuleFor(x => x.FocusId).GreaterThan(0);
        RuleFor(x => x.Duration).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
```

`src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class UpdateSessionActivityCommandValidator : AbstractValidator<UpdateSessionActivityCommand>
{
    public UpdateSessionActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.PhaseId).GreaterThan(0);
        RuleFor(x => x.FocusId).GreaterThan(0);
        RuleFor(x => x.Duration).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
```

`src/FootballPlanner.Application/Commands/SessionActivity/RemoveSessionActivityCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class RemoveSessionActivityCommandValidator : AbstractValidator<RemoveSessionActivityCommand>
{
    public RemoveSessionActivityCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
```

`src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityKeyPointsCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class UpdateSessionActivityKeyPointsCommandValidator : AbstractValidator<UpdateSessionActivityKeyPointsCommand>
{
    public UpdateSessionActivityKeyPointsCommandValidator()
    {
        RuleFor(x => x.SessionActivityId).GreaterThan(0);
        RuleForEach(x => x.KeyPoints).NotEmpty().MaximumLength(500);
    }
}
```

- [ ] **Step 5: Create handlers**

`src/FootballPlanner.Application/Commands/SessionActivity/AddSessionActivityCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class AddSessionActivityCommandHandler(AppDbContext db)
    : IRequestHandler<AddSessionActivityCommand, Domain.Entities.SessionActivity>
{
    public async Task<Domain.Entities.SessionActivity> Handle(
        AddSessionActivityCommand request, CancellationToken cancellationToken)
    {
        var sessionExists = await db.Sessions.AnyAsync(
            s => s.Id == request.SessionId, cancellationToken);
        if (!sessionExists)
            throw new KeyNotFoundException($"Session {request.SessionId} not found.");

        var maxOrder = await db.SessionActivities
            .Where(sa => sa.SessionId == request.SessionId)
            .MaxAsync(sa => (int?)sa.DisplayOrder, cancellationToken) ?? 0;

        var sessionActivity = Domain.Entities.SessionActivity.Create(
            request.SessionId, request.ActivityId, request.PhaseId, request.FocusId,
            request.Duration, maxOrder + 1, request.Notes);

        db.SessionActivities.Add(sessionActivity);
        await db.SaveChangesAsync(cancellationToken);
        return sessionActivity;
    }
}
```

`src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class UpdateSessionActivityCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateSessionActivityCommand, Domain.Entities.SessionActivity>
{
    public async Task<Domain.Entities.SessionActivity> Handle(
        UpdateSessionActivityCommand request, CancellationToken cancellationToken)
    {
        var sa = await db.SessionActivities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"SessionActivity {request.Id} not found.");
        sa.Update(request.PhaseId, request.FocusId, request.Duration, request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return sa;
    }
}
```

`src/FootballPlanner.Application/Commands/SessionActivity/RemoveSessionActivityCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class RemoveSessionActivityCommandHandler(AppDbContext db) : IRequestHandler<RemoveSessionActivityCommand>
{
    public async Task Handle(RemoveSessionActivityCommand request, CancellationToken cancellationToken)
    {
        var sa = await db.SessionActivities.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"SessionActivity {request.Id} not found.");
        db.SessionActivities.Remove(sa);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

`src/FootballPlanner.Application/Commands/SessionActivity/UpdateSessionActivityKeyPointsCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Commands.SessionActivity;

public class UpdateSessionActivityKeyPointsCommandHandler(AppDbContext db)
    : IRequestHandler<UpdateSessionActivityKeyPointsCommand>
{
    public async Task Handle(
        UpdateSessionActivityKeyPointsCommand request, CancellationToken cancellationToken)
    {
        var sa = await db.SessionActivities
            .Include(x => x.KeyPoints)
            .FirstOrDefaultAsync(x => x.Id == request.SessionActivityId, cancellationToken)
            ?? throw new KeyNotFoundException($"SessionActivity {request.SessionActivityId} not found.");

        db.SessionActivityKeyPoints.RemoveRange(sa.KeyPoints);

        var order = 1;
        foreach (var text in request.KeyPoints)
        {
            db.SessionActivityKeyPoints.Add(
                Domain.Entities.SessionActivityKeyPoint.Create(sa.Id, order++, text));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 6: Write remaining unit tests**

Update `tests/FootballPlanner.Unit.Tests/SessionActivity/AddSessionActivityCommandTests.cs` to add the remaining tests to the class (the class was created in Step 1 with one test — append these):

`tests/FootballPlanner.Unit.Tests/SessionActivity/AddSessionActivityCommandTests.cs` (complete class replacing the placeholder):
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class AddSessionActivityCommandTests
{
    private async Task<(IMediator mediator, int sessionId, int activityId, int phaseId, int focusId)> SetupAsync()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test Session", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Circle passing", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        return (mediator, session.Id, activity.Id, phase.Id, focus.Id);
    }

    [Fact]
    public async Task Send_AddsSessionActivity_WhenCommandIsValid()
    {
        var (mediator, sessionId, activityId, phaseId, focusId) = await SetupAsync();

        var result = await mediator.Send(
            new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 15, null));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(activityId, result.ActivityId);
        Assert.Equal(phaseId, result.PhaseId);
        Assert.Equal(focusId, result.FocusId);
        Assert.Equal(15, result.Duration);
        Assert.Equal(1, result.DisplayOrder);
    }

    [Fact]
    public async Task Send_AssignsIncreasingDisplayOrder_WhenMultipleActivitiesAdded()
    {
        var (mediator, sessionId, activityId, phaseId, focusId) = await SetupAsync();

        var first = await mediator.Send(
            new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 10, null));
        var second = await mediator.Send(
            new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 20, null));

        Assert.Equal(1, first.DisplayOrder);
        Assert.Equal(2, second.DisplayOrder);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new AddSessionActivityCommand(99999, activity.Id, phase.Id, focus.Id, 10, null)));
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenDurationIsZero()
    {
        var (mediator, sessionId, activityId, phaseId, focusId) = await SetupAsync();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new AddSessionActivityCommand(sessionId, activityId, phaseId, focusId, 0, null)));
    }
}
```

`tests/FootballPlanner.Unit.Tests/SessionActivity/UpdateSessionActivityCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class UpdateSessionActivityCommandTests
{
    [Fact]
    public async Task Send_UpdatesSessionActivity_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase1 = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var phase2 = await mediator.Send(new CreatePhaseCommand("Main", 2));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase1.Id, focus.Id, 10, null));

        var updated = await mediator.Send(
            new UpdateSessionActivityCommand(sa.Id, phase2.Id, focus.Id, 20, "Coach notes"));

        Assert.Equal(phase2.Id, updated.PhaseId);
        Assert.Equal(20, updated.Duration);
        Assert.Equal("Coach notes", updated.Notes);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateSessionActivityCommand(99999, phase.Id, focus.Id, 10, null)));
    }
}
```

`tests/FootballPlanner.Unit.Tests/SessionActivity/RemoveSessionActivityCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Application.Queries.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class RemoveSessionActivityCommandTests
{
    [Fact]
    public async Task Send_RemovesSessionActivity_WhenActivityExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));

        await mediator.Send(new RemoveSessionActivityCommand(sa.Id));

        var loaded = await mediator.Send(new GetSessionByIdQuery(session.Id));
        Assert.DoesNotContain(loaded!.Activities, a => a.Id == sa.Id);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new RemoveSessionActivityCommand(99999)));
    }
}
```

`tests/FootballPlanner.Unit.Tests/SessionActivity/UpdateSessionActivityKeyPointsCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Application.Queries.Session;
using FootballPlanner.Unit.Tests.Infrastructure;

namespace FootballPlanner.Unit.Tests.SessionActivity;

public class UpdateSessionActivityKeyPointsCommandTests
{
    [Fact]
    public async Task Send_ReplacesKeyPoints_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));

        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(
            sa.Id, ["Keep possession", "Press immediately on turnover"]));

        var loaded = await mediator.Send(new GetSessionByIdQuery(session.Id));
        var keyPoints = loaded!.Activities.First(a => a.Id == sa.Id).KeyPoints;
        Assert.Equal(2, keyPoints.Count);
        Assert.Equal("Keep possession", keyPoints[0].Text);
        Assert.Equal("Press immediately on turnover", keyPoints[1].Text);
    }

    [Fact]
    public async Task Send_ClearsKeyPoints_WhenEmptyListProvided()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var session = await mediator.Send(new CreateSessionCommand(DateTime.UtcNow, "Test", null));
        var activity = await mediator.Send(new CreateActivityCommand("Rondo", "Desc", null, 10));
        var phase = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));
        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(sa.Id, ["Initial point"]));

        await mediator.Send(new UpdateSessionActivityKeyPointsCommand(sa.Id, []));

        var loaded = await mediator.Send(new GetSessionByIdQuery(session.Id));
        Assert.Empty(loaded!.Activities.First(a => a.Id == sa.Id).KeyPoints);
    }

    [Fact]
    public async Task Send_ThrowsKeyNotFoundException_WhenSessionActivityNotFound()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => mediator.Send(new UpdateSessionActivityKeyPointsCommand(99999, ["Point"])));
    }
}
```

- [ ] **Step 7: Run all unit tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -10
```
Expected: All tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FootballPlanner.Application/Commands/SessionActivity/ \
        tests/FootballPlanner.Unit.Tests/SessionActivity/
git commit -m "feat: add SessionActivity CQRS commands, validators, handlers, and unit tests"
```

---

### Task 4: EF Core configurations and migration

**Files:**
- Create: `src/FootballPlanner.Infrastructure/Configurations/SessionActivityConfiguration.cs`
- Create: `src/FootballPlanner.Infrastructure/Configurations/SessionActivityKeyPointConfiguration.cs`
- Modify: `src/FootballPlanner.Infrastructure/AppDbContext.cs`
- New migration file under `src/FootballPlanner.Infrastructure/Migrations/`

- [ ] **Step 1: Create EF configurations**

`src/FootballPlanner.Infrastructure/Configurations/SessionActivityConfiguration.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class SessionActivityConfiguration : IEntityTypeConfiguration<SessionActivity>
{
    public void Configure(EntityTypeBuilder<SessionActivity> builder)
    {
        builder.ToTable("SessionActivities");
        builder.HasKey(sa => sa.Id);
        builder.Property(sa => sa.SessionId).IsRequired();
        builder.Property(sa => sa.ActivityId).IsRequired();
        builder.Property(sa => sa.PhaseId).IsRequired();
        builder.Property(sa => sa.FocusId).IsRequired();
        builder.Property(sa => sa.Duration).IsRequired();
        builder.Property(sa => sa.DisplayOrder).IsRequired();
        builder.Property(sa => sa.Notes).HasMaxLength(2000);
        builder.HasOne(sa => sa.Activity)
            .WithMany()
            .HasForeignKey(sa => sa.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(sa => sa.Phase)
            .WithMany()
            .HasForeignKey(sa => sa.PhaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(sa => sa.Focus)
            .WithMany()
            .HasForeignKey(sa => sa.FocusId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(sa => sa.KeyPoints)
            .WithOne()
            .HasForeignKey(kp => kp.SessionActivityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

`src/FootballPlanner.Infrastructure/Configurations/SessionActivityKeyPointConfiguration.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class SessionActivityKeyPointConfiguration : IEntityTypeConfiguration<SessionActivityKeyPoint>
{
    public void Configure(EntityTypeBuilder<SessionActivityKeyPoint> builder)
    {
        builder.ToTable("SessionActivityKeyPoints");
        builder.HasKey(kp => kp.Id);
        builder.Property(kp => kp.SessionActivityId).IsRequired();
        builder.Property(kp => kp.Order).IsRequired();
        builder.Property(kp => kp.Text).IsRequired().HasMaxLength(500);
    }
}
```

- [ ] **Step 2: Update AppDbContext**

Read `src/FootballPlanner.Infrastructure/AppDbContext.cs` and add the remaining DbSets and ApplyConfiguration calls:

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
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<SessionActivity> SessionActivities => Set<SessionActivity>();
    public DbSet<SessionActivityKeyPoint> SessionActivityKeyPoints => Set<SessionActivityKeyPoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PhaseConfiguration());
        modelBuilder.ApplyConfiguration(new FocusConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityConfiguration());
        modelBuilder.ApplyConfiguration(new SessionConfiguration());
        modelBuilder.ApplyConfiguration(new SessionActivityConfiguration());
        modelBuilder.ApplyConfiguration(new SessionActivityKeyPointConfiguration());
    }
}
```

- [ ] **Step 3: Add the migration**

```bash
dotnet ef migrations add AddSession \
  --project src/FootballPlanner.Infrastructure \
  --startup-project src/FootballPlanner.Infrastructure
```
Expected: Migration file created in `src/FootballPlanner.Infrastructure/Migrations/`.

- [ ] **Step 4: Verify migration creates all three tables**

Read the generated migration and confirm it contains:
- `CreateTable` for `Sessions` — Id, Date, Title, Notes, CreatedAt, UpdatedAt
- `CreateTable` for `SessionActivities` — Id, SessionId (FK→Sessions cascade), ActivityId (FK→Activities restrict), PhaseId (FK→Phases restrict), FocusId (FK→Focuses restrict), Duration, DisplayOrder, Notes
- `CreateTable` for `SessionActivityKeyPoints` — Id, SessionActivityId (FK→SessionActivities cascade), Order, Text

- [ ] **Step 5: Run all unit tests to confirm nothing broken**

```bash
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -5
```
Expected: All tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/FootballPlanner.Infrastructure/Configurations/SessionActivityConfiguration.cs \
        src/FootballPlanner.Infrastructure/Configurations/SessionActivityKeyPointConfiguration.cs \
        src/FootballPlanner.Infrastructure/AppDbContext.cs \
        src/FootballPlanner.Infrastructure/Migrations/
git commit -m "feat: add EF Core configurations and migration for Session tables"
```

---

## Chunk 3: Integration Tests and HTTP API

### Task 5: Integration tests

**Files:**
- Create: `tests/FootballPlanner.Integration.Tests/Session/SessionIntegrationTests.cs`

- [ ] **Step 1: Create integration tests**

`tests/FootballPlanner.Integration.Tests/Session/SessionIntegrationTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Application.Queries.Session;
using FootballPlanner.Integration.Tests.Infrastructure;

namespace FootballPlanner.Integration.Tests.Session;

public class SessionIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrieveSession_RoundTrip()
    {
        var date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var created = await app.Mediator.Send(new CreateSessionCommand(date, "Integration Session", "Notes here"));

        var sessions = await app.Mediator.Send(new GetAllSessionsQuery());

        Assert.Contains(sessions, s => s.Id == created.Id && s.Title == "Integration Session");
    }

    [Fact]
    public async Task UpdateSession_PersistsChanges()
    {
        var created = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "Old Title", null));
        var newDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await app.Mediator.Send(new UpdateSessionCommand(created.Id, newDate, "New Title", "Updated notes"));

        var sessions = await app.Mediator.Send(new GetAllSessionsQuery());
        var updated = sessions.First(s => s.Id == created.Id);
        Assert.Equal("New Title", updated.Title);
    }

    [Fact]
    public async Task DeleteSession_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "To Delete", null));

        await app.Mediator.Send(new DeleteSessionCommand(created.Id));

        var sessions = await app.Mediator.Send(new GetAllSessionsQuery());
        Assert.DoesNotContain(sessions, s => s.Id == created.Id);
    }

    [Fact]
    public async Task AddSessionActivity_AndLoadWithGetById_PersistsRelatedData()
    {
        var session = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "Activity Test Session", null));
        var activity = await app.Mediator.Send(
            new CreateActivityCommand("Pressing Drill", "High press drill", null, 15));
        var phase = await app.Mediator.Send(new CreatePhaseCommand("Main", 2));
        var focus = await app.Mediator.Send(new CreateFocusCommand("Pressing"));

        await app.Mediator.Send(new AddSessionActivityCommand(
            session.Id, activity.Id, phase.Id, focus.Id, 20, "Focus on trigger"));

        var loaded = await app.Mediator.Send(new GetSessionByIdQuery(session.Id));
        Assert.NotNull(loaded);
        Assert.Single(loaded.Activities);
        var sa = loaded.Activities[0];
        Assert.Equal(activity.Id, sa.ActivityId);
        Assert.Equal("Pressing Drill", sa.Activity.Name);
        Assert.Equal("Main", sa.Phase.Name);
        Assert.Equal("Pressing", sa.Focus.Name);
        Assert.Equal(20, sa.Duration);
        Assert.Equal(1, sa.DisplayOrder);
    }

    [Fact]
    public async Task UpdateSessionActivityKeyPoints_PersistsKeyPoints()
    {
        var session = await app.Mediator.Send(
            new CreateSessionCommand(DateTime.UtcNow, "KP Test Session", null));
        var activity = await app.Mediator.Send(
            new CreateActivityCommand("Rondo", "Circle passing", null, 10));
        var phase = await app.Mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        var focus = await app.Mediator.Send(new CreateFocusCommand("Possession"));
        var sa = await app.Mediator.Send(
            new AddSessionActivityCommand(session.Id, activity.Id, phase.Id, focus.Id, 10, null));

        await app.Mediator.Send(new UpdateSessionActivityKeyPointsCommand(
            sa.Id, ["Stay compact", "Quick release"]));

        var loaded = await app.Mediator.Send(new GetSessionByIdQuery(session.Id));
        var keyPoints = loaded!.Activities.First(a => a.Id == sa.Id).KeyPoints;
        Assert.Equal(2, keyPoints.Count);
        Assert.Equal("Stay compact", keyPoints[0].Text);
        Assert.Equal("Quick release", keyPoints[1].Text);
    }
}
```

- [ ] **Step 2: Run integration tests**

```bash
dotnet test tests/FootballPlanner.Integration.Tests --filter "FullyQualifiedName~SessionIntegrationTests" 2>&1 | tail -15
```
Expected: 5 tests pass. Uses Testcontainers (real SQL Server), may take ~1–2 minutes.

- [ ] **Step 3: Commit**

```bash
git add tests/FootballPlanner.Integration.Tests/Session/
git commit -m "feat: add Session integration tests using TestApplication"
```

---

### Task 6: SessionFunctions HTTP API

**Files:**
- Create: `src/FootballPlanner.Api/Functions/SessionFunctions.cs`

- [ ] **Step 1: Create SessionFunctions**

`src/FootballPlanner.Api/Functions/SessionFunctions.cs`:
```csharp
using FootballPlanner.Application.Commands.Session;
using FootballPlanner.Application.Commands.SessionActivity;
using FootballPlanner.Application.Queries.Session;
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

    private record UpdateSessionRequest(DateTime Date, string Title, string? Notes);
    private record AddSessionActivityRequest(int ActivityId, int PhaseId, int FocusId, int Duration, string? Notes);
    private record UpdateSessionActivityRequest(int PhaseId, int FocusId, int Duration, string? Notes);
    private record UpdateKeyPointsRequest(List<string> KeyPoints);
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build src/FootballPlanner.Api 2>&1 | tail -5
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Api/Functions/SessionFunctions.cs
git commit -m "feat: add Session HTTP Azure Functions"
```

---

## Chunk 4: Blazor UI

### Task 7: ApiClient update and Sessions list page

**Files:**
- Modify: `src/FootballPlanner.Web/Services/ApiClient.cs`
- Create: `src/FootballPlanner.Web/Pages/Sessions.razor`

- [ ] **Step 1: Update ApiClient**

Read `src/FootballPlanner.Web/Services/ApiClient.cs` first, then add session DTOs and methods alongside the existing Phase, Focus, Activity members.

The complete updated `ApiClient.cs`:
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
}
```

- [ ] **Step 2: Create Sessions list page**

`src/FootballPlanner.Web/Pages/Sessions.razor`:
```razor
@page "/sessions"
@inject Services.ApiClient Api
@inject NavigationManager Nav

<h1>Sessions</h1>

@if (editingId == null)
{
    <div>
        <input @bind="newDate" type="date" />
        <input @bind="newTitle" placeholder="Title" />
        <textarea @bind="newNotes" placeholder="Notes (optional)"></textarea>
        <button @onclick="AddSession">Add Session</button>
    </div>

    @if (sessions == null)
    {
        <p>Loading...</p>
    }
    else
    {
        <table>
            <thead>
                <tr>
                    <th>Date</th>
                    <th>Title</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                @foreach (var session in sessions)
                {
                    <tr>
                        <td>@session.Date.ToString("yyyy-MM-dd")</td>
                        <td>@session.Title</td>
                        <td>
                            <button @onclick="() => Nav.NavigateTo($"/sessions/{session.Id}")">Edit</button>
                            <button @onclick="() => StartEdit(session)">Rename</button>
                            <button @onclick="() => DeleteSession(session.Id)">Delete</button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }
}
else
{
    <h2>Edit Session</h2>
    <input @bind="editDate" type="date" />
    <input @bind="editTitle" placeholder="Title" />
    <textarea @bind="editNotes" placeholder="Notes (optional)"></textarea>
    <button @onclick="SaveEdit">Save</button>
    <button @onclick="CancelEdit">Cancel</button>
}

@code {
    private List<Services.ApiClient.SessionDto>? sessions;

    private DateTime newDate = DateTime.Today;
    private string newTitle = string.Empty;
    private string newNotes = string.Empty;

    private int? editingId;
    private DateTime editDate;
    private string editTitle = string.Empty;
    private string editNotes = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        sessions = await Api.GetSessionsAsync();
    }

    private async Task AddSession()
    {
        if (string.IsNullOrWhiteSpace(newTitle)) return;
        await Api.CreateSessionAsync(new Services.ApiClient.CreateSessionRequest(
            DateTime.SpecifyKind(newDate, DateTimeKind.Utc),
            newTitle,
            string.IsNullOrWhiteSpace(newNotes) ? null : newNotes));
        sessions = await Api.GetSessionsAsync();
        newTitle = string.Empty;
        newNotes = string.Empty;
        newDate = DateTime.Today;
    }

    private void StartEdit(Services.ApiClient.SessionDto session)
    {
        editingId = session.Id;
        editDate = session.Date;
        editTitle = session.Title;
        editNotes = session.Notes ?? string.Empty;
    }

    private async Task SaveEdit()
    {
        if (editingId == null || string.IsNullOrWhiteSpace(editTitle)) return;
        await Api.UpdateSessionAsync(editingId.Value, new Services.ApiClient.UpdateSessionRequest(
            DateTime.SpecifyKind(editDate, DateTimeKind.Utc),
            editTitle,
            string.IsNullOrWhiteSpace(editNotes) ? null : editNotes));
        sessions = await Api.GetSessionsAsync();
        CancelEdit();
    }

    private void CancelEdit()
    {
        editingId = null;
        editTitle = string.Empty;
        editNotes = string.Empty;
    }

    private async Task DeleteSession(int id)
    {
        await Api.DeleteSessionAsync(id);
        sessions = await Api.GetSessionsAsync();
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
        src/FootballPlanner.Web/Pages/Sessions.razor
git commit -m "feat: add Sessions list page and ApiClient session methods"
```

---

### Task 8: SessionEditor page

**Files:**
- Create: `src/FootballPlanner.Web/Pages/SessionEditor.razor`

The SessionEditor loads a session with all its activities and allows:
- Editing the session title/date/notes
- Adding activities (dropdown of all activities, phase/focus dropdowns, duration, notes)
- Editing a session activity (phase/focus/duration/notes/key points)
- Removing a session activity

Key points are edited as a textarea with one point per line; split on save, joined on load.

- [ ] **Step 1: Create SessionEditor page**

`src/FootballPlanner.Web/Pages/SessionEditor.razor`:
```razor
@page "/sessions/{Id:int}"
@inject Services.ApiClient Api

@if (session == null)
{
    <p>Loading...</p>
}
else
{
    <a href="/sessions">&larr; Back to Sessions</a>

    @if (!editingSession)
    {
        <h1>@session.Title</h1>
        <p>@session.Date.ToString("yyyy-MM-dd")</p>
        @if (session.Notes != null) { <p>@session.Notes</p> }
        <button @onclick="StartSessionEdit">Edit Details</button>
    }
    else
    {
        <h1>Edit Session</h1>
        <input @bind="editTitle" placeholder="Title" />
        <input @bind="editDate" type="date" />
        <textarea @bind="editNotes" placeholder="Notes (optional)"></textarea>
        <button @onclick="SaveSessionEdit">Save</button>
        <button @onclick="CancelSessionEdit">Cancel</button>
    }

    <h2>Activities</h2>

    @if (editingSessionActivityId == null)
    {
        @foreach (var sa in session.Activities)
        {
            <div style="border:1px solid #ccc; margin:8px; padding:8px;">
                <strong>@(sa.Activity?.Name ?? "Unknown")</strong>
                — @(sa.Phase?.Name ?? "") / @(sa.Focus?.Name ?? "")
                — @sa.Duration min
                @if (sa.Notes != null) { <div><em>@sa.Notes</em></div> }
                @if (sa.KeyPoints.Count > 0)
                {
                    <ul>
                        @foreach (var kp in sa.KeyPoints)
                        {
                            <li>@kp.Text</li>
                        }
                    </ul>
                }
                <button @onclick="() => StartActivityEdit(sa)">Edit</button>
                <button @onclick="() => RemoveActivity(sa.Id)">Remove</button>
            </div>
        }

        <h3>Add Activity</h3>
        <select @bind="newActivityId">
            <option value="0">-- Select Activity --</option>
            @foreach (var a in allActivities ?? [])
            {
                <option value="@a.Id">@a.Name (@a.EstimatedDuration min)</option>
            }
        </select>
        <select @bind="newPhaseId">
            <option value="0">-- Select Phase --</option>
            @foreach (var p in allPhases ?? [])
            {
                <option value="@p.Id">@p.Name</option>
            }
        </select>
        <select @bind="newFocusId">
            <option value="0">-- Select Focus --</option>
            @foreach (var f in allFocuses ?? [])
            {
                <option value="@f.Id">@f.Name</option>
            }
        </select>
        <input @bind="newDuration" type="number" placeholder="Duration (mins)" />
        <textarea @bind="newActivityNotes" placeholder="Notes (optional)"></textarea>
        <button @onclick="AddActivity">Add Activity</button>
    }
    else
    {
        <h3>Edit Activity</h3>
        <select @bind="editPhaseId">
            @foreach (var p in allPhases ?? [])
            {
                <option value="@p.Id">@p.Name</option>
            }
        </select>
        <select @bind="editFocusId">
            @foreach (var f in allFocuses ?? [])
            {
                <option value="@f.Id">@f.Name</option>
            }
        </select>
        <input @bind="editDuration" type="number" placeholder="Duration (mins)" />
        <textarea @bind="editActivityNotes" placeholder="Notes (optional)"></textarea>
        <label>Key Points (one per line):</label>
        <textarea @bind="editKeyPoints" rows="5" placeholder="One key point per line"></textarea>
        <button @onclick="SaveActivityEdit">Save</button>
        <button @onclick="CancelActivityEdit">Cancel</button>
    }
}

@code {
    [Parameter] public int Id { get; set; }

    private Services.ApiClient.SessionDto? session;
    private List<Services.ApiClient.ActivityDto>? allActivities;
    private List<Services.ApiClient.PhaseDto>? allPhases;
    private List<Services.ApiClient.FocusDto>? allFocuses;

    // Session edit state
    private bool editingSession;
    private string editTitle = string.Empty;
    private DateTime editDate;
    private string editNotes = string.Empty;

    // Add activity form state
    private int newActivityId;
    private int newPhaseId;
    private int newFocusId;
    private int newDuration = 30;
    private string newActivityNotes = string.Empty;

    // Edit session activity state
    private int? editingSessionActivityId;
    private int editPhaseId;
    private int editFocusId;
    private int editDuration;
    private string editActivityNotes = string.Empty;
    private string editKeyPoints = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadAll();
    }

    private async Task LoadAll()
    {
        session = await Api.GetSessionAsync(Id);
        allActivities = await Api.GetActivitiesAsync();
        allPhases = await Api.GetPhasesAsync();
        allFocuses = await Api.GetFocusesAsync();
    }

    private void StartSessionEdit()
    {
        editingSession = true;
        editTitle = session!.Title;
        editDate = session.Date;
        editNotes = session.Notes ?? string.Empty;
    }

    private async Task SaveSessionEdit()
    {
        if (string.IsNullOrWhiteSpace(editTitle)) return;
        await Api.UpdateSessionAsync(Id, new Services.ApiClient.UpdateSessionRequest(
            DateTime.SpecifyKind(editDate, DateTimeKind.Utc),
            editTitle,
            string.IsNullOrWhiteSpace(editNotes) ? null : editNotes));
        session = await Api.GetSessionAsync(Id);
        CancelSessionEdit();
    }

    private void CancelSessionEdit()
    {
        editingSession = false;
    }

    private async Task AddActivity()
    {
        if (newActivityId == 0 || newPhaseId == 0 || newFocusId == 0 || newDuration <= 0) return;
        await Api.AddSessionActivityAsync(Id, new Services.ApiClient.AddSessionActivityRequest(
            newActivityId, newPhaseId, newFocusId, newDuration,
            string.IsNullOrWhiteSpace(newActivityNotes) ? null : newActivityNotes));
        session = await Api.GetSessionAsync(Id);
        newActivityId = 0;
        newPhaseId = 0;
        newFocusId = 0;
        newDuration = 30;
        newActivityNotes = string.Empty;
    }

    private void StartActivityEdit(Services.ApiClient.SessionActivityDto sa)
    {
        editingSessionActivityId = sa.Id;
        editPhaseId = sa.PhaseId;
        editFocusId = sa.FocusId;
        editDuration = sa.Duration;
        editActivityNotes = sa.Notes ?? string.Empty;
        editKeyPoints = string.Join("\n", sa.KeyPoints.OrderBy(kp => kp.Order).Select(kp => kp.Text));
    }

    private async Task SaveActivityEdit()
    {
        if (editingSessionActivityId == null) return;
        await Api.UpdateSessionActivityAsync(Id, editingSessionActivityId.Value,
            new Services.ApiClient.UpdateSessionActivityRequest(
                editPhaseId, editFocusId, editDuration,
                string.IsNullOrWhiteSpace(editActivityNotes) ? null : editActivityNotes));
        var keyPointLines = editKeyPoints
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        await Api.UpdateSessionActivityKeyPointsAsync(Id, editingSessionActivityId.Value, keyPointLines);
        session = await Api.GetSessionAsync(Id);
        CancelActivityEdit();
    }

    private void CancelActivityEdit()
    {
        editingSessionActivityId = null;
        editActivityNotes = string.Empty;
        editKeyPoints = string.Empty;
    }

    private async Task RemoveActivity(int sessionActivityId)
    {
        await Api.RemoveSessionActivityAsync(Id, sessionActivityId);
        session = await Api.GetSessionAsync(Id);
    }
}
```

- [ ] **Step 2: Build the Web project**

```bash
dotnet build src/FootballPlanner.Web 2>&1 | tail -5
```
Expected: Build succeeded.

- [ ] **Step 3: Run all tests one final time**

```bash
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -5
dotnet test tests/FootballPlanner.Integration.Tests 2>&1 | tail -5
```
Expected: All unit and integration tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/FootballPlanner.Web/Pages/SessionEditor.razor
git commit -m "feat: add SessionEditor Blazor page for managing session activities"
```

---

## Summary

After completing all tasks, you will have:

1. **Three domain entities** — `Session`, `SessionActivity`, `SessionActivityKeyPoint` with private setters and factory/mutator methods
2. **Session CQRS** — Create, Update, Delete, GetAll (ordered by date desc), GetById (with all activities, phases, focuses, key points eagerly loaded)
3. **SessionActivity CQRS** — Add (auto-assigns DisplayOrder), Update, Remove, UpdateKeyPoints (replaces entire list)
4. **EF migration** — Three new tables: Sessions, SessionActivities (cascade delete from Session, restrict from Activity/Phase/Focus), SessionActivityKeyPoints (cascade from SessionActivity)
5. **Unit tests** — 20+ tests covering all commands/queries and validation failures
6. **Integration tests** — 5 tests against real SQL Server via Testcontainers
7. **9 Azure Functions endpoints** — full CRUD for sessions and session activities
8. **Blazor Sessions page** (`/sessions`) — list, create, rename, delete sessions
9. **Blazor SessionEditor page** (`/sessions/{id}`) — full session editor with activity management and key points
