# Logging Behavior Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `LoggingBehaviour` MediatR pipeline behavior that logs request start, completion with duration, and errors for every request.

**Architecture:** `LoggingBehaviour<TRequest, TResponse>` implements `IPipelineBehavior<TRequest, TResponse>` and is registered as the outermost pipeline behavior (before `ValidationBehaviour`). It lives in `Application/Common/Behaviours/` per the feature-based organization convention for shared code. It uses `ILogger<T>` from `Microsoft.Extensions.Logging.Abstractions`. Tests use a custom `CapturingLoggerProvider` (in the test infrastructure) to capture log output without any additional NuGet packages.

**Tech Stack:** .NET 10, MediatR 14, Microsoft.Extensions.Logging.Abstractions

---

## File Map

**New files:**
- `src/FootballPlanner.Application/Common/Behaviours/LoggingBehaviour.cs` — the behavior implementation
- `tests/FootballPlanner.Unit.Tests/Infrastructure/CapturingLoggerProvider.cs` — test helper that captures rendered log messages
- `tests/FootballPlanner.Unit.Tests/Common/LoggingBehaviourTests.cs` — behavior tests

**Modified files:**
- `src/FootballPlanner.Application/ServiceCollectionExtensions.cs` — register `LoggingBehaviour` before `ValidationBehaviour`

---

## Chunk 1: LoggingBehaviour

### Task 1: Implement LoggingBehaviour with tests

**Files:**
- Create: `src/FootballPlanner.Application/Common/Behaviours/LoggingBehaviour.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Infrastructure/CapturingLoggerProvider.cs`
- Create: `tests/FootballPlanner.Unit.Tests/Common/LoggingBehaviourTests.cs`
- Modify: `src/FootballPlanner.Application/ServiceCollectionExtensions.cs`

- [ ] **Step 1: Create CapturingLoggerProvider in test infrastructure**

Create `tests/FootballPlanner.Unit.Tests/Infrastructure/CapturingLoggerProvider.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace FootballPlanner.Unit.Tests.Infrastructure;

public record LogRecord(LogLevel Level, string Message);

public class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<LogRecord> _records = [];
    public IReadOnlyList<LogRecord> Records => _records;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_records);
    public void Dispose() { }
}

public class CapturingLogger(List<LogRecord> records) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        records.Add(new LogRecord(logLevel, formatter(state, exception)));
    }
}
```

- [ ] **Step 2: Write failing tests**

Create `tests/FootballPlanner.Unit.Tests/Common/LoggingBehaviourTests.cs`:

```csharp
using FootballPlanner.Application;
using FootballPlanner.Application.Commands.Activity;
using FootballPlanner.Infrastructure;
using FootballPlanner.Unit.Tests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FootballPlanner.Unit.Tests.Common;

public class LoggingBehaviourTests
{
    private static (IMediator mediator, CapturingLoggerProvider logs) CreateMediatorWithLogging()
    {
        var logProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging(b => b.AddProvider(logProvider).SetMinimumLevel(LogLevel.Information));
        services.AddApplication();
        services.AddInfrastructure(
            configuration,
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return (services.BuildServiceProvider().GetRequiredService<IMediator>(), logProvider);
    }

    [Fact]
    public async Task Send_LogsHandlingAtInformationLevel_WhenRequestIsSent()
    {
        var (mediator, logs) = CreateMediatorWithLogging();

        await mediator.Send(new CreateActivityCommand("Rondo", "Description", null, 10));

        Assert.Contains(logs.Records, r =>
            r.Level == LogLevel.Information &&
            r.Message.Contains("Handling") &&
            r.Message.Contains("CreateActivityCommand"));
    }

    [Fact]
    public async Task Send_LogsHandledWithDurationAtInformationLevel_WhenRequestSucceeds()
    {
        var (mediator, logs) = CreateMediatorWithLogging();

        await mediator.Send(new CreateActivityCommand("Rondo", "Description", null, 10));

        Assert.Contains(logs.Records, r =>
            r.Level == LogLevel.Information &&
            r.Message.Contains("Handled") &&
            r.Message.Contains("CreateActivityCommand") &&
            r.Message.Contains("ms"));
    }

    [Fact]
    public async Task Send_LogsFailedAtErrorLevel_WhenRequestThrows()
    {
        var (mediator, logs) = CreateMediatorWithLogging();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateActivityCommand("", "Description", null, 10)));

        Assert.Contains(logs.Records, r =>
            r.Level == LogLevel.Error &&
            r.Message.Contains("failed") &&
            r.Message.Contains("CreateActivityCommand") &&
            r.Message.Contains("ms"));
    }
}
```

- [ ] **Step 3: Run tests to confirm they fail**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~LoggingBehaviourTests" 2>&1 | tail -10
```

Expected: FAIL — `LoggingBehaviour` does not exist yet, so log messages are absent.

- [ ] **Step 4: Create LoggingBehaviour**

Create `src/FootballPlanner.Application/Common/Behaviours/LoggingBehaviour.cs`:

```csharp
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FootballPlanner.Application.Common.Behaviours;

public class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{RequestName} failed after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

- [ ] **Step 5: Register LoggingBehaviour in ServiceCollectionExtensions**

Replace the entire `src/FootballPlanner.Application/ServiceCollectionExtensions.cs` with:

```csharp
using FluentValidation;
using FootballPlanner.Application.Behaviours;
using FootballPlanner.Application.Common.Behaviours;
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
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        });
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        return services;
    }
}
```

`LoggingBehaviour` is registered first — MediatR executes behaviors in registration order, outermost first, so this wraps `ValidationBehaviour` and catches all exceptions including validation failures.

- [ ] **Step 6: Run tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Unit.Tests --filter "FullyQualifiedName~LoggingBehaviourTests" 2>&1 | tail -10
```

Expected: PASS — 3 tests.

- [ ] **Step 7: Run all unit and integration tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -5
dotnet test tests/FootballPlanner.Integration.Tests 2>&1 | tail -5
```

Expected: 52 unit tests passing (49 + 3 new), 14 integration tests passing.

- [ ] **Step 8: Commit**

```bash
git add src/FootballPlanner.Application/Common/Behaviours/LoggingBehaviour.cs \
        src/FootballPlanner.Application/ServiceCollectionExtensions.cs \
        tests/FootballPlanner.Unit.Tests/Infrastructure/CapturingLoggerProvider.cs \
        tests/FootballPlanner.Unit.Tests/Common/LoggingBehaviourTests.cs
git commit -m "feat: add LoggingBehaviour MediatR pipeline behavior"
```
