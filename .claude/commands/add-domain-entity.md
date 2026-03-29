# Add Domain Entity

Scaffold a new domain entity with its full CQRS stack following the conventions of this project.

## What Gets Created

Given an entity name (e.g., `Activity`), create the following:

### 1. Domain Entity — `src/FootballPlanner.Domain/Entities/<Name>.cs`
- Public properties with private setters
- A static factory method `Create(...)` for construction
- No dependencies on other layers

### 2. EF Core Configuration — `src/FootballPlanner.Infrastructure/Configurations/<Name>Configuration.cs`
- Implements `IEntityTypeConfiguration<Name>`
- Configure table name, primary key, required fields, string lengths, and relationships
- Register in `DbContext` via `modelBuilder.ApplyConfiguration(new <Name>Configuration())`

### 3. CQRS — one set per operation needed (Create, Update, Delete, GetById, GetAll):

**Command** — `src/FootballPlanner.Application/Commands/<Name>/Create<Name>Command.cs`
```csharp
public record Create<Name>Command(...) : IRequest<<Name>>;
```

**Validator** — `src/FootballPlanner.Application/Commands/<Name>/Create<Name>CommandValidator.cs`
```csharp
public class Create<Name>CommandValidator : AbstractValidator<Create<Name>Command> { }
```

**Handler** — `src/FootballPlanner.Application/Commands/<Name>/Create<Name>CommandHandler.cs`
```csharp
public class Create<Name>CommandHandler(AppDbContext db) : IRequestHandler<Create<Name>Command, <Name>> { }
```

**Query** — `src/FootballPlanner.Application/Queries/<Name>/Get<Name>Query.cs`

**Query Handler** — `src/FootballPlanner.Application/Queries/<Name>/Get<Name>QueryHandler.cs`

### 4. Azure Function — `src/FootballPlanner.Api/Functions/<Name>Functions.cs`
- Thin HTTP trigger methods only
- Each method: deserialise request → call `mediator.Send(command)` → return result
- No business logic

### 5. Unit Tests — `tests/FootballPlanner.Unit.Tests/<Name>/`
- One test class per handler
- Use EF Core InMemory provider
- No mocks

### 6. Integration Tests — `tests/FootballPlanner.Integration.Tests/<Name>/`
- One test class per Function endpoint
- Use Testcontainers SQL Server
- No mocks

## Conventions

- Commands and queries are `record` types
- Handlers take `DbContext` via primary constructor injection
- FluentValidation validators are always paired with their command
- Follow test-driven development: write tests first, then the implementation
