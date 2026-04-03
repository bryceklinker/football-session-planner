# Logging Behavior Design

## Goal

Add a `LoggingBehaviour` MediatR pipeline behavior that logs the start, completion, and failure of every request passing through the pipeline.

## Architecture

`LoggingBehaviour<TRequest, TResponse>` implements `IPipelineBehavior<TRequest, TResponse>`. It lives in `Application/Common/Behaviours/` alongside the existing `ValidationBehaviour`.

It is registered as the **outermost** behavior in the pipeline (before `ValidationBehaviour`), so it captures all exceptions including validation failures.

## Behavior

- **On entry:** Log `Information` — request type name
- **On success:** Log `Information` — request type name + elapsed milliseconds
- **On exception:** Log `Error` — request type name + elapsed milliseconds + exception

Only the request type name is logged (not the full request object) to avoid accidentally surfacing sensitive fields if they are added in future.

## Log Format

```
[Information] Handling CreateActivityCommand
[Information] Handled CreateActivityCommand in 42ms
[Error]       CreateActivityCommand failed after 5ms: <exception message>
```

## Files

**New files:**
- `src/FootballPlanner.Application/Common/Behaviours/LoggingBehaviour.cs`

**Modified files:**
- `src/FootballPlanner.Application/ServiceCollectionExtensions.cs` — register `LoggingBehaviour` before `ValidationBehaviour`

## Registration Order

```csharp
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
});
```

MediatR executes behaviors in registration order, outermost first. `LoggingBehaviour` wraps `ValidationBehaviour`.
