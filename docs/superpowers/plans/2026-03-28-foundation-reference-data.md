# Foundation + Reference Data Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold the complete solution structure with CQRS infrastructure, Phase and Focus reference data management, Blazor WASM frontend, Pulumi infrastructure, and GitHub Actions CI/CD pipelines.

**Architecture:** Azure Functions v4 (isolated) backend with MediatR CQRS, Blazor WebAssembly frontend hosted on Azure Static Web Apps, EF Core + Azure SQL Serverless, Auth0 JWT authentication via middleware.

**Tech Stack:** .NET 10, Azure Functions v4 isolated, Blazor WebAssembly, MediatR, FluentValidation, EF Core, Azure SQL Serverless, Pulumi (C#), Testcontainers, xUnit, GitHub Actions

---

## Chunk 1: Solution Scaffolding

### Task 1: Create solution and project structure

**Files:**
- Create: `global.json`
- Create: `FootballPlanner.slnx`
- Create: `src/FootballPlanner.Domain/FootballPlanner.Domain.csproj`
- Create: `src/FootballPlanner.Application/FootballPlanner.Application.csproj`
- Create: `src/FootballPlanner.Infrastructure/FootballPlanner.Infrastructure.csproj`
- Create: `src/FootballPlanner.Api/FootballPlanner.Api.csproj`
- Create: `src/FootballPlanner.Web/FootballPlanner.Web.csproj`
- Create: `tests/FootballPlanner.Unit.Tests/FootballPlanner.Unit.Tests.csproj`
- Create: `tests/FootballPlanner.Integration.Tests/FootballPlanner.Integration.Tests.csproj`
- Create: `tests/FootballPlanner.Feature.Tests/FootballPlanner.Feature.Tests.csproj`
- Create: `infra/FootballPlanner.Infra.csproj`
- Create: `.gitignore`

- [ ] **Step 1: Create global.json to pin .NET version**

`global.json`:
```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestMinor"
  }
}
```

`rollForward: latestMinor` allows patch and minor SDK updates while staying on .NET 10. All workflows use `global-json-file: global.json` with `actions/setup-dotnet@v4` so the version is sourced from a single place.

- [ ] **Step 2: Create the solution and project directories**

```bash
mkdir -p src tests infra
dotnet new slnx -n FootballPlanner
dotnet new classlib -n FootballPlanner.Domain -o src/FootballPlanner.Domain --framework net10.0
dotnet new classlib -n FootballPlanner.Application -o src/FootballPlanner.Application --framework net10.0
dotnet new classlib -n FootballPlanner.Infrastructure -o src/FootballPlanner.Infrastructure --framework net10.0
dotnet new func -n FootballPlanner.Api -o src/FootballPlanner.Api --worker-runtime dotnet-isolated --target-framework net10.0
dotnet new blazorwasm -n FootballPlanner.Web -o src/FootballPlanner.Web --framework net10.0
dotnet new xunit -n FootballPlanner.Unit.Tests -o tests/FootballPlanner.Unit.Tests --framework net10.0
dotnet new xunit -n FootballPlanner.Integration.Tests -o tests/FootballPlanner.Integration.Tests --framework net10.0
dotnet new xunit -n FootballPlanner.Feature.Tests -o tests/FootballPlanner.Feature.Tests --framework net10.0
dotnet new console -n FootballPlanner.Infra -o infra --framework net10.0
```

- [ ] **Step 3: Add all projects to the solution**

```bash
dotnet sln FootballPlanner.slnx add src/FootballPlanner.Domain
dotnet sln FootballPlanner.slnx add src/FootballPlanner.Application
dotnet sln FootballPlanner.slnx add src/FootballPlanner.Infrastructure
dotnet sln FootballPlanner.slnx add src/FootballPlanner.Api
dotnet sln FootballPlanner.slnx add src/FootballPlanner.Web
dotnet sln FootballPlanner.slnx add tests/FootballPlanner.Unit.Tests
dotnet sln FootballPlanner.slnx add tests/FootballPlanner.Integration.Tests
dotnet sln FootballPlanner.slnx add tests/FootballPlanner.Feature.Tests
dotnet sln FootballPlanner.slnx add infra
```

- [ ] **Step 4: Add project references**

```bash
# Application depends on Domain
dotnet add src/FootballPlanner.Application reference src/FootballPlanner.Domain

# Infrastructure depends on Domain and Application
dotnet add src/FootballPlanner.Infrastructure reference src/FootballPlanner.Domain
dotnet add src/FootballPlanner.Infrastructure reference src/FootballPlanner.Application

# Api depends on Application and Infrastructure
dotnet add src/FootballPlanner.Api reference src/FootballPlanner.Application
dotnet add src/FootballPlanner.Api reference src/FootballPlanner.Infrastructure

# Test projects
dotnet add tests/FootballPlanner.Unit.Tests reference src/FootballPlanner.Domain
dotnet add tests/FootballPlanner.Unit.Tests reference src/FootballPlanner.Application
dotnet add tests/FootballPlanner.Unit.Tests reference src/FootballPlanner.Infrastructure
dotnet add tests/FootballPlanner.Integration.Tests reference src/FootballPlanner.Domain
dotnet add tests/FootballPlanner.Integration.Tests reference src/FootballPlanner.Application
dotnet add tests/FootballPlanner.Integration.Tests reference src/FootballPlanner.Infrastructure
```

- [ ] **Step 5: Create `.gitignore`**

```
bin/
obj/
.vs/
*.user
local.settings.json
.pulumi/
```

- [ ] **Step 6: Verify solution builds**

```bash
dotnet build FootballPlanner.slnx
```
Expected: Build succeeded with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: scaffold solution with all projects and references"
```

---

### Task 2: Add NuGet packages

**Files:**
- Modify: all project `.csproj` files

- [ ] **Step 1: Add Application layer packages**

```bash
dotnet add src/FootballPlanner.Application package MediatR
dotnet add src/FootballPlanner.Application package FluentValidation
dotnet add src/FootballPlanner.Application package FluentValidation.DependencyInjectionExtensions
dotnet add src/FootballPlanner.Application package Microsoft.Extensions.DependencyInjection.Abstractions
```

- [ ] **Step 2: Add Infrastructure layer packages**

```bash
dotnet add src/FootballPlanner.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/FootballPlanner.Infrastructure package Microsoft.EntityFrameworkCore.InMemory
dotnet add src/FootballPlanner.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/FootballPlanner.Infrastructure package Microsoft.Extensions.Configuration.Abstractions
dotnet add src/FootballPlanner.Infrastructure package Microsoft.Extensions.DependencyInjection.Abstractions
```

- [ ] **Step 3: Add API packages**

```bash
dotnet add src/FootballPlanner.Api package MediatR
dotnet add src/FootballPlanner.Api package Microsoft.Azure.Functions.Worker
dotnet add src/FootballPlanner.Api package Microsoft.Azure.Functions.Worker.Sdk
dotnet add src/FootballPlanner.Api package Microsoft.Azure.Functions.Worker.Extensions.Http
dotnet add src/FootballPlanner.Api package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore
dotnet add src/FootballPlanner.Api package Microsoft.AspNetCore.Authentication.JwtBearer
```

- [ ] **Step 4: Add Unit Test packages**

```bash
dotnet add tests/FootballPlanner.Unit.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/FootballPlanner.Unit.Tests package Microsoft.Extensions.DependencyInjection
dotnet add tests/FootballPlanner.Unit.Tests package Microsoft.Extensions.Configuration
```

- [ ] **Step 5: Add Integration Test packages**

```bash
dotnet add tests/FootballPlanner.Integration.Tests package Testcontainers.MsSql
dotnet add tests/FootballPlanner.Integration.Tests package Microsoft.Extensions.DependencyInjection
dotnet add tests/FootballPlanner.Integration.Tests package Microsoft.Extensions.Configuration
dotnet add tests/FootballPlanner.Integration.Tests package Microsoft.EntityFrameworkCore.SqlServer
```

- [ ] **Step 6: Add Feature Test packages**

```bash
dotnet add tests/FootballPlanner.Feature.Tests package Microsoft.Playwright
dotnet add tests/FootballPlanner.Feature.Tests package Microsoft.Playwright.NUnit
```

- [ ] **Step 7: Add Pulumi infra packages**

```bash
dotnet add infra package Pulumi
dotnet add infra package Pulumi.AzureNative
```

- [ ] **Step 8: Verify build**

```bash
dotnet build FootballPlanner.slnx
```
Expected: Build succeeded.

- [ ] **Step 9: Commit**

```bash
git add .
git commit -m "feat: add NuGet packages to all projects"
```

---

## Chunk 2: Domain and Application Infrastructure

### Task 3: Domain entities

**Files:**
- Create: `src/FootballPlanner.Domain/Entities/Phase.cs`
- Create: `src/FootballPlanner.Domain/Entities/Focus.cs`
- Delete: `src/FootballPlanner.Domain/Class1.cs`

- [ ] **Step 1: Create Phase entity**

`src/FootballPlanner.Domain/Entities/Phase.cs`:
```csharp
namespace FootballPlanner.Domain.Entities;

public class Phase
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }

    private Phase() { }

    public static Phase Create(string name, int order)
    {
        return new Phase { Name = name, Order = order };
    }

    public void Update(string name, int order)
    {
        Name = name;
        Order = order;
    }
}
```

- [ ] **Step 2: Create Focus entity**

`src/FootballPlanner.Domain/Entities/Focus.cs`:
```csharp
namespace FootballPlanner.Domain.Entities;

public class Focus
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Focus() { }

    public static Focus Create(string name)
    {
        return new Focus { Name = name };
    }

    public void Update(string name)
    {
        Name = name;
    }
}
```

- [ ] **Step 3: Delete placeholder file**

```bash
rm src/FootballPlanner.Domain/Class1.cs
```

- [ ] **Step 4: Verify build**

```bash
dotnet build src/FootballPlanner.Domain
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat: add Phase and Focus domain entities"
```

---

### Task 4: CQRS application infrastructure

**Files:**
- Create: `src/FootballPlanner.Application/Behaviours/ValidationBehaviour.cs`
- Create: `src/FootballPlanner.Application/ServiceCollectionExtensions.cs`
- Delete: `src/FootballPlanner.Application/Class1.cs`

- [ ] **Step 1: Create ValidationBehaviour**

`src/FootballPlanner.Application/Behaviours/ValidationBehaviour.cs`:
```csharp
using FluentValidation;
using MediatR;

namespace FootballPlanner.Application.Behaviours;

public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

- [ ] **Step 2: Create AddApplication service collection extension**

`src/FootballPlanner.Application/ServiceCollectionExtensions.cs`:
```csharp
using FluentValidation;
using FootballPlanner.Application.Behaviours;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        return services;
    }
}
```

- [ ] **Step 3: Delete placeholder file**

```bash
rm src/FootballPlanner.Application/Class1.cs
```

- [ ] **Step 4: Verify build**

```bash
dotnet build src/FootballPlanner.Application
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat: add CQRS application infrastructure with ValidationBehaviour"
```

---

### Task 5: EF Core DbContext and Infrastructure setup

**Files:**
- Create: `src/FootballPlanner.Infrastructure/AppDbContext.cs`
- Create: `src/FootballPlanner.Infrastructure/Configurations/PhaseConfiguration.cs`
- Create: `src/FootballPlanner.Infrastructure/Configurations/FocusConfiguration.cs`
- Create: `src/FootballPlanner.Infrastructure/ServiceCollectionExtensions.cs`
- Delete: `src/FootballPlanner.Infrastructure/Class1.cs`

- [ ] **Step 1: Create AppDbContext**

`src/FootballPlanner.Infrastructure/AppDbContext.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using FootballPlanner.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Phase> Phases => Set<Phase>();
    public DbSet<Focus> Focuses => Set<Focus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PhaseConfiguration());
        modelBuilder.ApplyConfiguration(new FocusConfiguration());
    }
}
```

- [ ] **Step 2: Create PhaseConfiguration**

`src/FootballPlanner.Infrastructure/Configurations/PhaseConfiguration.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class PhaseConfiguration : IEntityTypeConfiguration<Phase>
{
    public void Configure(EntityTypeBuilder<Phase> builder)
    {
        builder.ToTable("Phases");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Order).IsRequired();
    }
}
```

- [ ] **Step 3: Create FocusConfiguration**

`src/FootballPlanner.Infrastructure/Configurations/FocusConfiguration.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FootballPlanner.Infrastructure.Configurations;

public class FocusConfiguration : IEntityTypeConfiguration<Focus>
{
    public void Configure(EntityTypeBuilder<Focus> builder)
    {
        builder.ToTable("Focuses");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
    }
}
```

- [ ] **Step 4: Create AddInfrastructure service collection extension**

`src/FootballPlanner.Infrastructure/ServiceCollectionExtensions.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDb = null)
    {
        var dbOptions = configureDb
            ?? (options => options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddDbContext<AppDbContext>(dbOptions);
        return services;
    }
}
```

- [ ] **Step 5: Delete placeholder file**

```bash
rm src/FootballPlanner.Infrastructure/Class1.cs
```

- [ ] **Step 6: Verify build**

```bash
dotnet build src/FootballPlanner.Infrastructure
```
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add EF Core DbContext with Phase and Focus configurations"
```

---

## Chunk 3: Phase CQRS

### Task 6: Phase commands and queries — tests first

**Files:**
- Create: `src/FootballPlanner.Application/Commands/Phase/CreatePhaseCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Phase/CreatePhaseCommandValidator.cs`
- Create: `src/FootballPlanner.Application/Commands/Phase/CreatePhaseCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Commands/Phase/UpdatePhaseCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Phase/UpdatePhaseCommandValidator.cs`
- Create: `src/FootballPlanner.Application/Commands/Phase/UpdatePhaseCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Commands/Phase/DeletePhaseCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Phase/DeletePhaseCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Queries/Phase/GetAllPhasesQuery.cs`
- Create: `src/FootballPlanner.Application/Queries/Phase/GetAllPhasesQueryHandler.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Infrastructure/TestServiceProvider.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Phase/CreatePhaseCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Phase/UpdatePhaseCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Phase/DeletePhaseCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Phase/GetAllPhasesQueryTests.cs`

- [ ] **Step 1: Create TestServiceProvider**

`tests/FootballPlanner.Unit.Tests/Infrastructure/TestServiceProvider.cs`:
```csharp
using FootballPlanner.Application;
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Unit.Tests.Infrastructure;

public static class TestServiceProvider
{
    public static IMediator CreateMediator()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddApplication();
        services.AddInfrastructure(
            configuration,
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }
}
```

- [ ] **Step 2: Write failing tests for CreatePhaseCommand**

`tests/FootballPlanner.Unit.Tests/Phase/CreatePhaseCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Phase;

public class CreatePhaseCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsPhase_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Warm Up", result.Name);
        Assert.Equal(1, result.Order);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreatePhaseCommand("", 1)));
    }
}
```

- [ ] **Step 3: Run tests — expect compilation failure (command not yet defined)**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~CreatePhaseCommandTests"
```
Expected: FAIL — type not found.

- [ ] **Step 4: Create CreatePhaseCommand**

`src/FootballPlanner.Application/Commands/Phase/CreatePhaseCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public record CreatePhaseCommand(string Name, int Order) : IRequest<Domain.Entities.Phase>;
```

- [ ] **Step 5: Create CreatePhaseCommandValidator**

`src/FootballPlanner.Application/Commands/Phase/CreatePhaseCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Phase;

public class CreatePhaseCommandValidator : AbstractValidator<CreatePhaseCommand>
{
    public CreatePhaseCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}
```

- [ ] **Step 6: Create CreatePhaseCommandHandler**

`src/FootballPlanner.Application/Commands/Phase/CreatePhaseCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public class CreatePhaseCommandHandler(AppDbContext db)
    : IRequestHandler<CreatePhaseCommand, Domain.Entities.Phase>
{
    public async Task<Domain.Entities.Phase> Handle(
        CreatePhaseCommand request, CancellationToken cancellationToken)
    {
        var phase = Domain.Entities.Phase.Create(request.Name, request.Order);
        db.Phases.Add(phase);
        await db.SaveChangesAsync(cancellationToken);
        return phase;
    }
}
```

- [ ] **Step 7: Run CreatePhase tests — expect pass**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~CreatePhaseCommandTests"
```
Expected: PASS (2 tests).

- [ ] **Step 8: Write UpdatePhase tests**

`tests/FootballPlanner.Unit.Tests/Phase/UpdatePhaseCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Phase;

public class UpdatePhaseCommandTests
{
    [Fact]
    public async Task Send_UpdatesPhase_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        await mediator.Send(new UpdatePhaseCommand(created.Id, "Activation", 2));

        var phases = await mediator.Send(new GetAllPhasesQuery());
        var updated = phases.Single(p => p.Id == created.Id);
        Assert.Equal("Activation", updated.Name);
        Assert.Equal(2, updated.Order);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new UpdatePhaseCommand(created.Id, "", 1)));
    }
}
```

- [ ] **Step 9: Create UpdatePhase CQRS**

`src/FootballPlanner.Application/Commands/Phase/UpdatePhaseCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public record UpdatePhaseCommand(int Id, string Name, int Order) : IRequest;
```

`src/FootballPlanner.Application/Commands/Phase/UpdatePhaseCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Phase;

public class UpdatePhaseCommandValidator : AbstractValidator<UpdatePhaseCommand>
{
    public UpdatePhaseCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}
```

`src/FootballPlanner.Application/Commands/Phase/UpdatePhaseCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public class UpdatePhaseCommandHandler(AppDbContext db) : IRequestHandler<UpdatePhaseCommand>
{
    public async Task Handle(UpdatePhaseCommand request, CancellationToken cancellationToken)
    {
        var phase = await db.Phases.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Phase {request.Id} not found.");
        phase.Update(request.Name, request.Order);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 10: Write DeletePhase tests**

`tests/FootballPlanner.Unit.Tests/Phase/DeletePhaseCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Phase;

public class DeletePhaseCommandTests
{
    [Fact]
    public async Task Send_RemovesPhase_WhenPhaseExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreatePhaseCommand("Warm Up", 1));

        await mediator.Send(new DeletePhaseCommand(created.Id));

        var phases = await mediator.Send(new GetAllPhasesQuery());
        Assert.DoesNotContain(phases, p => p.Id == created.Id);
    }
}
```

- [ ] **Step 11: Create DeletePhase CQRS**

`src/FootballPlanner.Application/Commands/Phase/DeletePhaseCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public record DeletePhaseCommand(int Id) : IRequest;
```

`src/FootballPlanner.Application/Commands/Phase/DeletePhaseCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Phase;

public class DeletePhaseCommandHandler(AppDbContext db) : IRequestHandler<DeletePhaseCommand>
{
    public async Task Handle(DeletePhaseCommand request, CancellationToken cancellationToken)
    {
        var phase = await db.Phases.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Phase {request.Id} not found.");
        db.Phases.Remove(phase);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 12: Write GetAllPhases tests**

`tests/FootballPlanner.Unit.Tests/Phase/GetAllPhasesQueryTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Phase;

public class GetAllPhasesQueryTests
{
    [Fact]
    public async Task Send_ReturnsAllPhases_OrderedByOrder()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreatePhaseCommand("Scrimmage", 3));
        await mediator.Send(new CreatePhaseCommand("Warm Up", 1));
        await mediator.Send(new CreatePhaseCommand("Main Activity", 2));

        var result = await mediator.Send(new GetAllPhasesQuery());

        Assert.Equal(3, result.Count);
        Assert.Equal("Warm Up", result[0].Name);
        Assert.Equal("Main Activity", result[1].Name);
        Assert.Equal("Scrimmage", result[2].Name);
    }

    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoPhasesExist()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new GetAllPhasesQuery());

        Assert.Empty(result);
    }
}
```

- [ ] **Step 13: Create GetAllPhases CQRS**

`src/FootballPlanner.Application/Queries/Phase/GetAllPhasesQuery.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using MediatR;

namespace FootballPlanner.Application.Queries.Phase;

public record GetAllPhasesQuery : IRequest<List<Phase>>;
```

`src/FootballPlanner.Application/Queries/Phase/GetAllPhasesQueryHandler.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Queries.Phase;

public class GetAllPhasesQueryHandler(AppDbContext db) : IRequestHandler<GetAllPhasesQuery, List<Phase>>
{
    public async Task<List<Phase>> Handle(GetAllPhasesQuery request, CancellationToken cancellationToken)
    {
        return await db.Phases
            .OrderBy(p => p.Order)
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 14: Run all Phase unit tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests
```
Expected: All tests pass.

- [ ] **Step 15: Commit**

```bash
git add .
git commit -m "feat: add Phase CQRS commands and queries with unit tests"
```

---

## Chunk 4: Focus CQRS

### Task 7: Focus commands and queries — tests first

**Files:**
- Create: `src/FootballPlanner.Application/Commands/Focus/CreateFocusCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Focus/CreateFocusCommandValidator.cs`
- Create: `src/FootballPlanner.Application/Commands/Focus/CreateFocusCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Commands/Focus/UpdateFocusCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Focus/UpdateFocusCommandValidator.cs`
- Create: `src/FootballPlanner.Application/Commands/Focus/UpdateFocusCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Commands/Focus/DeleteFocusCommand.cs`
- Create: `src/FootballPlanner.Application/Commands/Focus/DeleteFocusCommandHandler.cs`
- Create: `src/FootballPlanner.Application/Queries/Focus/GetAllFocusesQuery.cs`
- Create: `src/FootballPlanner.Application/Queries/Focus/GetAllFocusesQueryHandler.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Focus/CreateFocusCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Focus/UpdateFocusCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Focus/DeleteFocusCommandTests.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Focus/GetAllFocusesQueryTests.cs`

- [ ] **Step 1: Write failing tests for Focus CQRS**

`tests/FootballPlanner.Unit.Tests/Focus/CreateFocusCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Focus;

public class CreateFocusCommandTests
{
    [Fact]
    public async Task Send_CreatesAndReturnsFocus_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new CreateFocusCommand("Pressing"));

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Pressing", result.Name);
    }

    [Fact]
    public async Task Send_ThrowsValidationException_WhenNameIsEmpty()
    {
        var mediator = TestServiceProvider.CreateMediator();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateFocusCommand("")));
    }
}
```

`tests/FootballPlanner.Unit.Tests/Focus/UpdateFocusCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Queries.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Focus;

public class UpdateFocusCommandTests
{
    [Fact]
    public async Task Send_UpdatesFocus_WhenCommandIsValid()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreateFocusCommand("Pressing"));

        await mediator.Send(new UpdateFocusCommand(created.Id, "Counter Press"));

        var focuses = await mediator.Send(new GetAllFocusesQuery());
        var updated = focuses.Single(f => f.Id == created.Id);
        Assert.Equal("Counter Press", updated.Name);
    }
}
```

`tests/FootballPlanner.Unit.Tests/Focus/DeleteFocusCommandTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Queries.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Focus;

public class DeleteFocusCommandTests
{
    [Fact]
    public async Task Send_RemovesFocus_WhenFocusExists()
    {
        var mediator = TestServiceProvider.CreateMediator();
        var created = await mediator.Send(new CreateFocusCommand("Pressing"));

        await mediator.Send(new DeleteFocusCommand(created.Id));

        var focuses = await mediator.Send(new GetAllFocusesQuery());
        Assert.DoesNotContain(focuses, f => f.Id == created.Id);
    }
}
```

`tests/FootballPlanner.Unit.Tests/Focus/GetAllFocusesQueryTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Queries.Focus;
using FootballPlanner.Unit.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Unit.Tests.Focus;

public class GetAllFocusesQueryTests
{
    [Fact]
    public async Task Send_ReturnsAllFocuses()
    {
        var mediator = TestServiceProvider.CreateMediator();
        await mediator.Send(new CreateFocusCommand("Pressing"));
        await mediator.Send(new CreateFocusCommand("Possession"));

        var result = await mediator.Send(new GetAllFocusesQuery());

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Send_ReturnsEmptyList_WhenNoFocusesExist()
    {
        var mediator = TestServiceProvider.CreateMediator();

        var result = await mediator.Send(new GetAllFocusesQuery());

        Assert.Empty(result);
    }
}
```

- [ ] **Step 2: Create Focus CQRS commands and queries**

`src/FootballPlanner.Application/Commands/Focus/CreateFocusCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public record CreateFocusCommand(string Name) : IRequest<Domain.Entities.Focus>;
```

`src/FootballPlanner.Application/Commands/Focus/CreateFocusCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Focus;

public class CreateFocusCommandValidator : AbstractValidator<CreateFocusCommand>
{
    public CreateFocusCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

`src/FootballPlanner.Application/Commands/Focus/CreateFocusCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public class CreateFocusCommandHandler(AppDbContext db)
    : IRequestHandler<CreateFocusCommand, Domain.Entities.Focus>
{
    public async Task<Domain.Entities.Focus> Handle(
        CreateFocusCommand request, CancellationToken cancellationToken)
    {
        var focus = Domain.Entities.Focus.Create(request.Name);
        db.Focuses.Add(focus);
        await db.SaveChangesAsync(cancellationToken);
        return focus;
    }
}
```

`src/FootballPlanner.Application/Commands/Focus/UpdateFocusCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public record UpdateFocusCommand(int Id, string Name) : IRequest;
```

`src/FootballPlanner.Application/Commands/Focus/UpdateFocusCommandValidator.cs`:
```csharp
using FluentValidation;

namespace FootballPlanner.Application.Commands.Focus;

public class UpdateFocusCommandValidator : AbstractValidator<UpdateFocusCommand>
{
    public UpdateFocusCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
```

`src/FootballPlanner.Application/Commands/Focus/UpdateFocusCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public class UpdateFocusCommandHandler(AppDbContext db) : IRequestHandler<UpdateFocusCommand>
{
    public async Task Handle(UpdateFocusCommand request, CancellationToken cancellationToken)
    {
        var focus = await db.Focuses.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Focus {request.Id} not found.");
        focus.Update(request.Name);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

`src/FootballPlanner.Application/Commands/Focus/DeleteFocusCommand.cs`:
```csharp
using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public record DeleteFocusCommand(int Id) : IRequest;
```

`src/FootballPlanner.Application/Commands/Focus/DeleteFocusCommandHandler.cs`:
```csharp
using FootballPlanner.Infrastructure;
using MediatR;

namespace FootballPlanner.Application.Commands.Focus;

public class DeleteFocusCommandHandler(AppDbContext db) : IRequestHandler<DeleteFocusCommand>
{
    public async Task Handle(DeleteFocusCommand request, CancellationToken cancellationToken)
    {
        var focus = await db.Focuses.FindAsync([request.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"Focus {request.Id} not found.");
        db.Focuses.Remove(focus);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

`src/FootballPlanner.Application/Queries/Focus/GetAllFocusesQuery.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using MediatR;

namespace FootballPlanner.Application.Queries.Focus;

public record GetAllFocusesQuery : IRequest<List<Focus>>;
```

`src/FootballPlanner.Application/Queries/Focus/GetAllFocusesQueryHandler.cs`:
```csharp
using FootballPlanner.Domain.Entities;
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FootballPlanner.Application.Queries.Focus;

public class GetAllFocusesQueryHandler(AppDbContext db) : IRequestHandler<GetAllFocusesQuery, List<Focus>>
{
    public async Task<List<Focus>> Handle(GetAllFocusesQuery request, CancellationToken cancellationToken)
    {
        return await db.Focuses
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);
    }
}
```

- [ ] **Step 3: Run all unit tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests
```
Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add .
git commit -m "feat: add Focus CQRS commands and queries with unit tests"
```

---

## Chunk 5: EF Core Migration and Integration Tests

### Task 8: Initial EF Core migration

**Files:**
- Create: `src/FootballPlanner.Infrastructure/Migrations/` (generated)
- Create: `src/FootballPlanner.Infrastructure/AppDbContextFactory.cs`

- [ ] **Step 1: Add design-time DbContext factory to enable migrations**

`src/FootballPlanner.Infrastructure/AppDbContextFactory.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FootballPlanner.Infrastructure;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=FootballPlanner;Trusted_Connection=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}
```

- [ ] **Step 2: Add EF Core tools to Infrastructure project**

```bash
dotnet add src/FootballPlanner.Infrastructure package Microsoft.EntityFrameworkCore.Tools
```

- [ ] **Step 3: Generate the initial migration**

```bash
dotnet ef migrations add InitialCreate \
  --project src/FootballPlanner.Infrastructure \
  --startup-project src/FootballPlanner.Infrastructure
```

- [ ] **Step 4: Review generated migration**

Open `src/FootballPlanner.Infrastructure/Migrations/<timestamp>_InitialCreate.cs` and verify:
- `Up()` creates `Phases` table with Id, Name (nvarchar(100)), Order columns
- `Up()` creates `Focuses` table with Id, Name (nvarchar(100)) columns
- `Down()` drops both tables

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat: add initial EF Core migration for Phase and Focus"
```

---

### Task 9: Integration tests with TestApplication

**Files:**
- Create: `tests/FootballPlanner.Integration.Tests/Infrastructure/TestApplication.cs`
- Create: `tests/FootballPlanner.Integration.Tests/Phase/PhaseIntegrationTests.cs`
- Create: `tests/FootballPlanner.Integration.Tests/Focus/FocusIntegrationTests.cs`

- [ ] **Step 1: Create TestApplication**

`tests/FootballPlanner.Integration.Tests/Infrastructure/TestApplication.cs`:
```csharp
using FootballPlanner.Application;
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace FootballPlanner.Integration.Tests.Infrastructure;

public class TestApplication : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public IMediator Mediator { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        Mediator = provider.GetRequiredService<IMediator>();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

- [ ] **Step 2: Write Phase integration tests**

`tests/FootballPlanner.Integration.Tests/Phase/PhaseIntegrationTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using FootballPlanner.Integration.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Integration.Tests.Phase;

public class PhaseIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrievePhase_RoundTrip()
    {
        var created = await app.Mediator.Send(new CreatePhaseCommand("Integration Warm Up", 1));

        var phases = await app.Mediator.Send(new GetAllPhasesQuery());

        Assert.Contains(phases, p => p.Id == created.Id && p.Name == "Integration Warm Up");
    }

    [Fact]
    public async Task UpdatePhase_PersistsChanges()
    {
        var created = await app.Mediator.Send(new CreatePhaseCommand("Old Name", 5));

        await app.Mediator.Send(new UpdatePhaseCommand(created.Id, "New Name", 6));

        var phases = await app.Mediator.Send(new GetAllPhasesQuery());
        var updated = phases.First(p => p.Id == created.Id);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task DeletePhase_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(new CreatePhaseCommand("To Delete", 9));

        await app.Mediator.Send(new DeletePhaseCommand(created.Id));

        var phases = await app.Mediator.Send(new GetAllPhasesQuery());
        Assert.DoesNotContain(phases, p => p.Id == created.Id);
    }
}
```

- [ ] **Step 3: Write Focus integration tests**

`tests/FootballPlanner.Integration.Tests/Focus/FocusIntegrationTests.cs`:
```csharp
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Queries.Focus;
using FootballPlanner.Integration.Tests.Infrastructure;
using Xunit;

namespace FootballPlanner.Integration.Tests.Focus;

public class FocusIntegrationTests(TestApplication app) : IClassFixture<TestApplication>
{
    [Fact]
    public async Task CreateAndRetrieveFocus_RoundTrip()
    {
        var created = await app.Mediator.Send(new CreateFocusCommand("Integration Pressing"));

        var focuses = await app.Mediator.Send(new GetAllFocusesQuery());

        Assert.Contains(focuses, f => f.Id == created.Id && f.Name == "Integration Pressing");
    }

    [Fact]
    public async Task UpdateFocus_PersistsChanges()
    {
        var created = await app.Mediator.Send(new CreateFocusCommand("Old Focus"));

        await app.Mediator.Send(new UpdateFocusCommand(created.Id, "New Focus"));

        var focuses = await app.Mediator.Send(new GetAllFocusesQuery());
        var updated = focuses.First(f => f.Id == created.Id);
        Assert.Equal("New Focus", updated.Name);
    }

    [Fact]
    public async Task DeleteFocus_RemovesFromDatabase()
    {
        var created = await app.Mediator.Send(new CreateFocusCommand("To Delete Focus"));

        await app.Mediator.Send(new DeleteFocusCommand(created.Id));

        var focuses = await app.Mediator.Send(new GetAllFocusesQuery());
        Assert.DoesNotContain(focuses, f => f.Id == created.Id);
    }
}
```

- [ ] **Step 4: Run integration tests**

```bash
dotnet test tests/FootballPlanner.Integration.Tests
```
Expected: All tests pass (Docker must be running for Testcontainers).

- [ ] **Step 5: Commit**

```bash
git add .
git commit -m "feat: add integration tests for Phase and Focus using TestApplication and Testcontainers"
```

---

## Chunk 6: Azure Functions API

### Task 10: Phase and Focus HTTP Functions

**Files:**
- Create: `src/FootballPlanner.Api/Functions/PhaseFunctions.cs`
- Create: `src/FootballPlanner.Api/Functions/FocusFunctions.cs`
- Create: `src/FootballPlanner.Api/Middleware/AuthMiddleware.cs`
- Modify: `src/FootballPlanner.Api/Program.cs`

- [ ] **Step 1: Create Auth0 JWT middleware**

`src/FootballPlanner.Api/Middleware/AuthMiddleware.cs`:
```csharp
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FootballPlanner.Api.Middleware;

public class AuthMiddleware(IConfiguration configuration) : IFunctionsWorkerMiddleware
{
    private static string ExtractBearerToken(FunctionContext context)
    {
        var request = context.GetHttpContext()?.Request;
        var auth = request?.Headers.Authorization.FirstOrDefault();
        if (auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return auth["Bearer ".Length..];
        return string.Empty;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var token = ExtractBearerToken(context);
        if (string.IsNullOrEmpty(token))
        {
            await RespondUnauthorized(context);
            return;
        }

        var domain = configuration["Auth0:Domain"];
        var audience = configuration["Auth0:Audience"];

        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"https://{domain}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever());

        var openIdConfig = await configManager.GetConfigurationAsync();

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = $"https://{domain}/",
            ValidAudience = audience,
            IssuerSigningKeys = openIdConfig.SigningKeys,
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validationParameters, out _);
            await next(context);
        }
        catch (SecurityTokenException)
        {
            await RespondUnauthorized(context);
        }
    }

    private static async Task RespondUnauthorized(FunctionContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext != null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        }
    }
}
```

- [ ] **Step 2: Create PhaseFunctions**

`src/FootballPlanner.Api/Functions/PhaseFunctions.cs`:
```csharp
using FootballPlanner.Application.Commands.Phase;
using FootballPlanner.Application.Queries.Phase;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FootballPlanner.Api.Functions;

public class PhaseFunctions(IMediator mediator)
{
    [Function("GetPhases")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "phases")] HttpRequestData req)
    {
        var phases = await mediator.Send(new GetAllPhasesQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(phases);
        return response;
    }

    [Function("CreatePhase")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "phases")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreatePhaseCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var phase = await mediator.Send(command);
        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteAsJsonAsync(phase);
        return response;
    }

    [Function("UpdatePhase")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "phases/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdatePhaseRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdatePhaseCommand(id, body.Name, body.Order));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    [Function("DeletePhase")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "phases/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeletePhaseCommand(id));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    private record UpdatePhaseRequest(string Name, int Order);
}
```

- [ ] **Step 3: Create FocusFunctions**

`src/FootballPlanner.Api/Functions/FocusFunctions.cs`:
```csharp
using FootballPlanner.Application.Commands.Focus;
using FootballPlanner.Application.Queries.Focus;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace FootballPlanner.Api.Functions;

public class FocusFunctions(IMediator mediator)
{
    [Function("GetFocuses")]
    public async Task<HttpResponseData> GetAll(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "focuses")] HttpRequestData req)
    {
        var focuses = await mediator.Send(new GetAllFocusesQuery());
        var response = req.CreateResponse();
        await response.WriteAsJsonAsync(focuses);
        return response;
    }

    [Function("CreateFocus")]
    public async Task<HttpResponseData> Create(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "focuses")] HttpRequestData req)
    {
        var command = await req.ReadFromJsonAsync<CreateFocusCommand>()
            ?? throw new InvalidOperationException("Invalid request body.");
        var focus = await mediator.Send(command);
        var response = req.CreateResponse(System.Net.HttpStatusCode.Created);
        await response.WriteAsJsonAsync(focus);
        return response;
    }

    [Function("UpdateFocus")]
    public async Task<HttpResponseData> Update(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "focuses/{id:int}")] HttpRequestData req,
        int id)
    {
        var body = await req.ReadFromJsonAsync<UpdateFocusRequest>()
            ?? throw new InvalidOperationException("Invalid request body.");
        await mediator.Send(new UpdateFocusCommand(id, body.Name));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    [Function("DeleteFocus")]
    public async Task<HttpResponseData> Delete(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "focuses/{id:int}")] HttpRequestData req,
        int id)
    {
        await mediator.Send(new DeleteFocusCommand(id));
        return req.CreateResponse(System.Net.HttpStatusCode.NoContent);
    }

    private record UpdateFocusRequest(string Name);
}
```

- [ ] **Step 4: Update Program.cs to wire up DI and middleware**

`src/FootballPlanner.Api/Program.cs`:
```csharp
using FootballPlanner.Api.Middleware;
using FootballPlanner.Application;
using FootballPlanner.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker =>
    {
        worker.UseMiddleware<AuthMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
    })
    .Build();

host.Run();
```

- [ ] **Step 5: Build the API project**

```bash
dotnet build src/FootballPlanner.Api
```
Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "feat: add Phase and Focus HTTP Azure Functions with Auth0 middleware"
```

---

## Chunk 7: Blazor WebAssembly Frontend

### Task 11: Blazor WASM Phase and Focus management pages

**Files:**
- Create: `src/FootballPlanner.Web/Pages/Phases.razor`
- Create: `src/FootballPlanner.Web/Pages/Focuses.razor`
- Create: `src/FootballPlanner.Web/Services/ApiClient.cs`
- Modify: `src/FootballPlanner.Web/wwwroot/appsettings.json`
- Modify: `src/FootballPlanner.Web/Program.cs`

- [ ] **Step 1: Configure appsettings.json**

`src/FootballPlanner.Web/wwwroot/appsettings.json`:
```json
{
  "ApiBaseUrl": "http://localhost:7071/api",
  "Auth0": {
    "Domain": "",
    "ClientId": "",
    "Audience": ""
  }
}
```

- [ ] **Step 2: Create ApiClient**

`src/FootballPlanner.Web/Services/ApiClient.cs`:
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

    public record PhaseDto(int Id, string Name, int Order);
    public record FocusDto(int Id, string Name);
    public record CreatePhaseRequest(string Name, int Order);
    public record UpdatePhaseRequest(string Name, int Order);
    public record CreateFocusRequest(string Name);
    public record UpdateFocusRequest(string Name);
}
```

- [ ] **Step 3: Create Phases management page**

`src/FootballPlanner.Web/Pages/Phases.razor`:
```razor
@page "/phases"
@inject Services.ApiClient Api

<h1>Phases</h1>

<div>
    <input @bind="newPhaseName" placeholder="Phase name" />
    <input @bind="newPhaseOrder" type="number" placeholder="Order" />
    <button @onclick="AddPhase">Add</button>
</div>

@if (phases == null)
{
    <p>Loading...</p>
}
else
{
    <table>
        <thead>
            <tr><th>Order</th><th>Name</th><th></th></tr>
        </thead>
        <tbody>
            @foreach (var phase in phases.OrderBy(p => p.Order))
            {
                <tr>
                    <td>@phase.Order</td>
                    <td>@phase.Name</td>
                    <td>
                        <button @onclick="() => DeletePhase(phase.Id)">Delete</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<Services.ApiClient.PhaseDto>? phases;
    private string newPhaseName = string.Empty;
    private int newPhaseOrder = 1;

    protected override async Task OnInitializedAsync()
    {
        phases = await Api.GetPhasesAsync();
    }

    private async Task AddPhase()
    {
        if (string.IsNullOrWhiteSpace(newPhaseName)) return;
        await Api.CreatePhaseAsync(new Services.ApiClient.CreatePhaseRequest(newPhaseName, newPhaseOrder));
        phases = await Api.GetPhasesAsync();
        newPhaseName = string.Empty;
        newPhaseOrder = 1;
    }

    private async Task DeletePhase(int id)
    {
        await Api.DeletePhaseAsync(id);
        phases = await Api.GetPhasesAsync();
    }
}
```

- [ ] **Step 4: Create Focuses management page**

`src/FootballPlanner.Web/Pages/Focuses.razor`:
```razor
@page "/focuses"
@inject Services.ApiClient Api

<h1>Focuses</h1>

<div>
    <input @bind="newFocusName" placeholder="Focus name" />
    <button @onclick="AddFocus">Add</button>
</div>

@if (focuses == null)
{
    <p>Loading...</p>
}
else
{
    <table>
        <thead>
            <tr><th>Name</th><th></th></tr>
        </thead>
        <tbody>
            @foreach (var focus in focuses)
            {
                <tr>
                    <td>@focus.Name</td>
                    <td>
                        <button @onclick="() => DeleteFocus(focus.Id)">Delete</button>
                    </td>
                </tr>
            }
        </tbody>
    </table>
}

@code {
    private List<Services.ApiClient.FocusDto>? focuses;
    private string newFocusName = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        focuses = await Api.GetFocusesAsync();
    }

    private async Task AddFocus()
    {
        if (string.IsNullOrWhiteSpace(newFocusName)) return;
        await Api.CreateFocusAsync(new Services.ApiClient.CreateFocusRequest(newFocusName));
        focuses = await Api.GetFocusesAsync();
        newFocusName = string.Empty;
    }

    private async Task DeleteFocus(int id)
    {
        await Api.DeleteFocusAsync(id);
        focuses = await Api.GetFocusesAsync();
    }
}
```

- [ ] **Step 5: Register ApiClient in Program.cs**

`src/FootballPlanner.Web/Program.cs`:
```csharp
using FootballPlanner.Web;
using FootballPlanner.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
```

- [ ] **Step 6: Build Blazor project**

```bash
dotnet build src/FootballPlanner.Web
```
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add Blazor WASM Phase and Focus management pages"
```

---

## Chunk 8: Pulumi Infrastructure

### Task 12: Pulumi infrastructure with Naming utility

**Files:**
- Create: `infra/Naming.cs`
- Create: `infra/Program.cs` (overwrite generated)
- Create: `infra/FootballPlannerStack.cs`
- Create: `infra/Pulumi.yaml`
- Create: `infra/Pulumi.prod.yaml`

The Azure location is read from the `AZURE_LOCATION` environment variable (set by CI/CD workflows) with a fallback default of `centralus`. This is the **single authoritative place** for the default — no hard-coded region values anywhere else, not in `Pulumi.prod.yaml`, not in the composite action.

- [ ] **Step 1: Create Naming utility**

`infra/Naming.cs`:
```csharp
namespace FootballPlanner.Infra;

public class Naming(string environment)
{
    public string Environment { get; } = environment;

    public string Resource(string baseName) => $"{baseName}-{environment}";
}
```

- [ ] **Step 2: Create Pulumi.yaml**

`infra/Pulumi.yaml`:
```yaml
name: football-planner
runtime: dotnet
description: Football Session Planner infrastructure
```

- [ ] **Step 3: Create Pulumi.prod.yaml**

`infra/Pulumi.prod.yaml`:
```yaml
config:
  football-planner:environment: prod
```

Note: No `azure-native:location` here. Location comes exclusively from the `AZURE_LOCATION` environment variable, with the default `centralus` defined once in `FootballPlannerStack.cs`.

- [ ] **Step 4: Create FootballPlannerStack**

`infra/FootballPlannerStack.cs`:
```csharp
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;

namespace FootballPlanner.Infra;

public class FootballPlannerStack : Stack
{
    public FootballPlannerStack()
    {
        var config = new Config();
        var environment = config.Require("environment");
        var naming = new Naming(environment);

        // Location is read from AZURE_LOCATION env var (set by CI/CD workflows).
        // centralus is the default; override the env var to deploy to a different region.
        // This is the single place the default is defined — do not add it elsewhere.
        var location = System.Environment.GetEnvironmentVariable("AZURE_LOCATION") ?? "centralus";

        var resourceGroup = new ResourceGroup(naming.Resource("football-planner-rg"), new ResourceGroupArgs
        {
            Location = location,
            ResourceGroupName = naming.Resource("football-planner-rg"),
        });

        var sqlServer = new Server(naming.Resource("football-planner-sql"), new ServerArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ServerName = naming.Resource("football-planner-sql"),
            AdministratorLogin = config.RequireSecret("sqlAdminLogin"),
            AdministratorLoginPassword = config.RequireSecret("sqlAdminPassword"),
        });

        var sqlDatabase = new Database(naming.Resource("football-planner-db"), new DatabaseArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            ServerName = sqlServer.Name,
            DatabaseName = naming.Resource("football-planner-db"),
            Sku = new Pulumi.AzureNative.Sql.Inputs.SkuArgs { Name = "GP_S_Gen5_1", Tier = "GeneralPurpose" },
            AutoPauseDelay = 60,
            MinCapacity = 0.5,
        });

        var staticWebApp = new StaticSite(naming.Resource("football-planner-swa"), new StaticSiteArgs
        {
            ResourceGroupName = resourceGroup.Name,
            Location = resourceGroup.Location,
            Name = naming.Resource("football-planner-swa"),
            Sku = new SkuDescriptionArgs { Name = "Standard", Tier = "Standard" },
        });

        // Retrieve the SWA deployment token from Azure so the deploy workflow
        // can read it as a stack output — no manual secret configuration needed.
        var swaSecrets = Pulumi.AzureNative.Web.ListStaticSiteSecrets.Invoke(
            new Pulumi.AzureNative.Web.ListStaticSiteSecretsInvokeArgs
            {
                ResourceGroupName = resourceGroup.Name,
                Name = staticWebApp.Name,
            });

        ResourceGroupName = resourceGroup.Name;
        StaticWebAppUrl = staticWebApp.DefaultHostname;
        SqlServerName = sqlServer.Name;
        DatabaseName = sqlDatabase.Name;
        StaticWebAppDeployToken = swaSecrets.Apply(s => s.Properties["apiKey"].Value!);
    }

    [Output] public Output<string> ResourceGroupName { get; set; }
    [Output] public Output<string> StaticWebAppUrl { get; set; }
    [Output] public Output<string> SqlServerName { get; set; }
    [Output] public Output<string> DatabaseName { get; set; }
    [Output] public Output<string> StaticWebAppDeployToken { get; set; }
}
```

- [ ] **Step 5: Update Program.cs for Pulumi**

`infra/Program.cs`:
```csharp
using FootballPlanner.Infra;
using Pulumi;

return await Deployment.RunAsync<FootballPlannerStack>();
```

- [ ] **Step 6: Build infra project**

```bash
dotnet build infra
```
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add .
git commit -m "feat: add Pulumi infrastructure stack with Naming utility and SWA token output"
```

---

## Chunk 9: GitHub Actions Workflows

### Task 13: Shared composite action for Pulumi state storage

**Files:**
- Create: `.github/actions/ensure-pulumi-state/action.yml`

The composite action reads `AZURE_LOCATION` from the calling workflow's environment (passed through automatically) with a shell-level fallback of `centralus`. This mirrors the pattern used in `FootballPlannerStack.cs` — the location flows from the workflow env var into all Azure resource creation.

- [ ] **Step 1: Create the shared composite action**

`.github/actions/ensure-pulumi-state/action.yml`:
```yaml
name: Ensure Pulumi State Storage
description: Idempotently creates the Azure Storage Account used for Pulumi state

inputs:
  client_id:
    description: Azure service principal client ID
    required: true
  client_secret:
    description: Azure service principal client secret
    required: true
  tenant_id:
    description: Azure tenant ID
    required: true
  subscription_id:
    description: Azure subscription ID
    required: true

runs:
  using: composite
  steps:
    - name: Azure Login
      uses: azure/login@v2
      with:
        client-id: ${{ inputs.client_id }}
        client-secret: ${{ inputs.client_secret }}
        tenant-id: ${{ inputs.tenant_id }}
        subscription-id: ${{ inputs.subscription_id }}

    - name: Ensure Pulumi state storage account exists
      shell: bash
      run: |
        RESOURCE_GROUP="pulumi-state-rg"
        STORAGE_ACCOUNT="pulumistatestore"
        CONTAINER="pulumi-state"
        # Inherit AZURE_LOCATION from the calling workflow env; fall back to centralus
        LOCATION="${AZURE_LOCATION:-centralus}"

        az group create \
          --name "$RESOURCE_GROUP" \
          --location "$LOCATION" \
          --output none || true

        az storage account create \
          --name "$STORAGE_ACCOUNT" \
          --resource-group "$RESOURCE_GROUP" \
          --location "$LOCATION" \
          --sku Standard_LRS \
          --output none || true

        az storage container create \
          --name "$CONTAINER" \
          --account-name "$STORAGE_ACCOUNT" \
          --auth-mode login \
          --output none || true
```

- [ ] **Step 2: Commit shared action**

```bash
git add .
git commit -m "feat: add shared composite action for idempotent Pulumi state storage setup"
```

---

### Task 14: CI workflow

**Files:**
- Create: `.github/workflows/ci.yml`

`AZURE_LOCATION: centralus` is set at the workflow level so all jobs and steps (including the composite action) inherit it. The .NET version is sourced from `global.json` via `global-json-file`.

- [ ] **Step 1: Create CI workflow**

`.github/workflows/ci.yml`:
```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

env:
  AZURE_LOCATION: centralus

jobs:
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

      - name: Run unit tests
        run: dotnet test tests/FootballPlanner.Unit.Tests --no-build --configuration Release --logger trx

      - name: Run integration tests
        run: dotnet test tests/FootballPlanner.Integration.Tests --no-build --configuration Release --logger trx

      - name: Run feature tests
        run: dotnet test tests/FootballPlanner.Feature.Tests --no-build --configuration Release --logger trx

  pulumi-preview:
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.ref == 'refs/heads/main' || github.event_name == 'pull_request'
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Ensure Pulumi state storage
        uses: ./.github/actions/ensure-pulumi-state
        with:
          client_id: ${{ secrets.ARM_CLIENT_ID }}
          client_secret: ${{ secrets.ARM_CLIENT_SECRET }}
          tenant_id: ${{ secrets.ARM_TENANT_ID }}
          subscription_id: ${{ secrets.ARM_SUBSCRIPTION_ID }}

      - name: Pulumi Preview
        uses: pulumi/actions@v6
        with:
          command: preview
          stack-name: prod
          work-dir: infra
          backend-url: azblob://pulumi-state?storage_account=pulumistatestore
        env:
          PULUMI_ACCESS_TOKEN: ""
          ARM_CLIENT_ID: ${{ secrets.ARM_CLIENT_ID }}
          ARM_CLIENT_SECRET: ${{ secrets.ARM_CLIENT_SECRET }}
          ARM_TENANT_ID: ${{ secrets.ARM_TENANT_ID }}
          ARM_SUBSCRIPTION_ID: ${{ secrets.ARM_SUBSCRIPTION_ID }}
```

- [ ] **Step 2: Commit CI workflow**

```bash
git add .
git commit -m "feat: add CI workflow with build, tests, and Pulumi preview"
```

---

### Task 15: Deploy workflow

**Files:**
- Create: `.github/workflows/deploy.yml`

- [ ] **Step 1: Create deploy workflow**

`.github/workflows/deploy.yml`:
```yaml
name: Deploy

on:
  workflow_run:
    workflows: [CI]
    types: [completed]
    branches: [main]

env:
  AZURE_LOCATION: centralus

jobs:
  deploy:
    runs-on: ubuntu-latest
    if: ${{ github.event.workflow_run.conclusion == 'success' }}
    environment: production
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - name: Ensure Pulumi state storage
        uses: ./.github/actions/ensure-pulumi-state
        with:
          client_id: ${{ secrets.ARM_CLIENT_ID }}
          client_secret: ${{ secrets.ARM_CLIENT_SECRET }}
          tenant_id: ${{ secrets.ARM_TENANT_ID }}
          subscription_id: ${{ secrets.ARM_SUBSCRIPTION_ID }}

      - name: Pulumi Up
        uses: pulumi/actions@v6
        with:
          command: up
          stack-name: prod
          work-dir: infra
          backend-url: azblob://pulumi-state?storage_account=pulumistatestore
        env:
          PULUMI_ACCESS_TOKEN: ""
          ARM_CLIENT_ID: ${{ secrets.ARM_CLIENT_ID }}
          ARM_CLIENT_SECRET: ${{ secrets.ARM_CLIENT_SECRET }}
          ARM_TENANT_ID: ${{ secrets.ARM_TENANT_ID }}
          ARM_SUBSCRIPTION_ID: ${{ secrets.ARM_SUBSCRIPTION_ID }}

      - name: Get SWA deploy token from Pulumi output
        id: swa-token
        working-directory: infra
        run: |
          TOKEN=$(pulumi stack output StaticWebAppDeployToken --show-secrets \
            --stack prod \
            --non-interactive \
            --backend-url azblob://pulumi-state?storage_account=pulumistatestore)
          echo "token=$TOKEN" >> "$GITHUB_OUTPUT"
        env:
          ARM_CLIENT_ID: ${{ secrets.ARM_CLIENT_ID }}
          ARM_CLIENT_SECRET: ${{ secrets.ARM_CLIENT_SECRET }}
          ARM_TENANT_ID: ${{ secrets.ARM_TENANT_ID }}
          ARM_SUBSCRIPTION_ID: ${{ secrets.ARM_SUBSCRIPTION_ID }}

      - name: Publish API
        run: dotnet publish src/FootballPlanner.Api --configuration Release --output ./publish/api

      - name: Publish Web
        run: dotnet publish src/FootballPlanner.Web --configuration Release --output ./publish/web

      - name: Deploy to Azure Static Web Apps
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ steps.swa-token.outputs.token }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: upload
          app_location: ./publish/web
          api_location: ./publish/api
          skip_app_build: true
```

- [ ] **Step 2: Commit deploy workflow**

```bash
git add .
git commit -m "feat: add deploy workflow triggered after CI with manual approval gate"
```

---

### Task 16: Required GitHub secrets documentation

The following secrets must be configured in your GitHub repository under **Settings → Secrets and variables → Actions**:

| Secret | Description | Setup Guide |
|--------|-------------|-------------|
| `ARM_CLIENT_ID` | Azure service principal client ID | [Create SP for GitHub Actions](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure) — use `az ad sp create-for-rbac` |
| `ARM_CLIENT_SECRET` | Azure service principal client secret | From `az ad sp create-for-rbac` output field `password` |
| `ARM_TENANT_ID` | Azure tenant ID | From `az account show --query tenantId` |
| `ARM_SUBSCRIPTION_ID` | Azure subscription ID | From `az account show --query id` |

The `production` environment must be configured in GitHub under **Settings → Environments → production** with a required reviewer for the manual approval gate before each deployment.

The SWA deployment token does **not** need to be stored as a secret — it is read from the Pulumi stack output after `pulumi up`. The Azure location does **not** need to be a secret — it is set via `AZURE_LOCATION: centralus` in the workflow `env` block.

- [ ] **Step 1: No code change — this is reference documentation**

---

## Chunk 10: CLAUDE.md and Final Cleanup

### Task 17: Create CLAUDE.md

**Files:**
- Create: `CLAUDE.md`

- [ ] **Step 1: Create CLAUDE.md**

`CLAUDE.md`:
```markdown
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Test Commands

```bash
# Build entire solution
dotnet build FootballPlanner.slnx

# Run unit tests
dotnet test tests/FootballPlanner.Unit.Tests

# Run integration tests (requires Docker for Testcontainers)
dotnet test tests/FootballPlanner.Integration.Tests

# Run feature tests (requires a running app instance)
dotnet test tests/FootballPlanner.Feature.Tests

# Run a single test by name
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~CreatePhaseCommandTests"

# Add EF Core migration
dotnet ef migrations add <MigrationName> \
  --project src/FootballPlanner.Infrastructure \
  --startup-project src/FootballPlanner.Infrastructure

# Apply migrations to local database
dotnet ef database update \
  --project src/FootballPlanner.Infrastructure \
  --startup-project src/FootballPlanner.Infrastructure
```

## Solution Structure

- `src/FootballPlanner.Domain` — Entities with private setters and static `Create()` factory methods. No dependencies on other layers.
- `src/FootballPlanner.Application` — MediatR commands/queries, FluentValidation validators, `ValidationBehaviour` pipeline. `AddApplication()` registers all.
- `src/FootballPlanner.Infrastructure` — EF Core `AppDbContext`, entity configurations, `AddInfrastructure(config, configureDb?)` registration. Optional `configureDb` action allows tests to swap to InMemory.
- `src/FootballPlanner.Api` — Azure Functions v4 isolated worker. Thin HTTP trigger functions only — deserialise request, send to MediatR, return result.
- `src/FootballPlanner.Web` — Blazor WebAssembly. All C# inline in `@code {}` blocks (no code-behind `.razor.cs` files).
- `tests/FootballPlanner.Unit.Tests` — Uses `TestServiceProvider.CreateMediator()` with InMemory EF Core. Never instantiates handlers directly.
- `tests/FootballPlanner.Integration.Tests` — Uses `TestApplication` (IClassFixture) with Testcontainers SQL Server and real migrations.
- `tests/FootballPlanner.Feature.Tests` — Playwright tests against a running app instance.
- `infra/` — Pulumi C# stack. `Naming` class scopes resource names by environment (e.g., `naming.Resource("rg")` → `"rg-prod"`).

## CQRS Conventions

- Commands and queries are `record` types implementing `IRequest<T>`.
- Handlers use primary constructor injection: `public class Handler(AppDbContext db) : IRequestHandler<Command, Result>`.
- Every command has a paired `AbstractValidator<TCommand>`.
- `ValidationBehaviour` runs validators automatically via the MediatR pipeline.

## Testing Conventions

- **No mocking anywhere.** Use real implementations.
- Unit tests: `TestServiceProvider.CreateMediator()` — sets up real DI with `AddApplication()` + `AddInfrastructure()` (InMemory EF Core).
- Integration tests: `TestApplication` class (implements `IAsyncLifetime`) — starts a SQL Server container, runs migrations, exposes `IMediator Mediator`.
- Tests call `mediator.Send(command)` — never instantiate handlers or validators directly.
- Do NOT use FluentAssertions (licensing change). Use standard xUnit `Assert.*` methods.

## Infrastructure

- Pulumi state is stored in Azure Blob Storage: `azblob://pulumi-state?storage_account=pulumistatestore`
- Stack is created per environment. Current environment: `prod`
- Resource names are always environment-scoped via the `Naming` utility: `naming.Resource("base-name")` → `"base-name-prod"`
- Azure region is controlled by the `AZURE_LOCATION` env var. The default (`centralus`) is defined once in `FootballPlannerStack.cs` — do not duplicate it elsewhere.
- The SWA deployment token is exposed as a Pulumi stack output (`StaticWebAppDeployToken`) and read in the deploy workflow — no manual secret needed.

## GitHub Actions

- .NET version is pinned in `global.json`; all `setup-dotnet` steps use `global-json-file: global.json`.
- CI (`ci.yml`): build + unit + integration + feature tests, then Pulumi preview (on main/PRs).
- Deploy (`deploy.yml`): auto-triggers when CI succeeds on main, requires manual approval via `production` environment gate.
- Both workflows set `AZURE_LOCATION: centralus` and use the shared `.github/actions/ensure-pulumi-state` composite action before any Pulumi commands.
- Required secrets: `ARM_CLIENT_ID`, `ARM_CLIENT_SECRET`, `ARM_TENANT_ID`, `ARM_SUBSCRIPTION_ID`.

## Custom Commands

- `/add-project` — scaffolds a new .NET project and adds it to the solution
- `/add-domain-entity` — creates a domain entity with full CQRS stack (commands, validators, handlers, queries, tests)
- `/add-migration` — adds a new EF Core migration and optionally applies it
```

- [ ] **Step 2: Build and run all unit tests to verify solution state**

```bash
dotnet build FootballPlanner.slnx && dotnet test tests/FootballPlanner.Unit.Tests
```
Expected: Build succeeded, all unit tests pass.

- [ ] **Step 3: Commit CLAUDE.md**

```bash
git add CLAUDE.md
git commit -m "docs: add CLAUDE.md with project conventions and commands"
```

---

## Summary

After completing all chunks, you will have:

1. **.NET 10 solution** with `global.json` pinning the SDK version, all projects properly referenced
2. **Domain entities** for Phase and Focus with private setters and factory methods
3. **CQRS infrastructure** with MediatR, FluentValidation, ValidationBehaviour
4. **EF Core** with Azure SQL Serverless configuration and initial migration
5. **Unit tests** using InMemory EF Core via `TestServiceProvider.CreateMediator()`
6. **Integration tests** using Testcontainers SQL Server via `TestApplication`
7. **Azure Functions API** with Auth0 JWT middleware
8. **Blazor WebAssembly** Phase and Focus management pages (desktop edit, mobile read)
9. **Pulumi infrastructure** with `Naming` utility, Azure SQL Serverless + Static Web Apps, SWA deploy token as stack output, `centralus` default in one place
10. **GitHub Actions**: shared composite action for Pulumi state, CI (build + tests + preview), Deploy (auto-triggered after CI, manual approval gate), .NET version from `global.json`
