# Add Project

Scaffold a new .NET project in the Football Session Planner solution following established conventions.

## Steps

1. **Determine project type** from the user's request:
   - Source project (`src/FootballPlanner.<Name>`) — class library or Azure Functions app
   - Test project (`tests/FootballPlanner.<Name>.Tests`) — xUnit test project

2. **Create the project** using `dotnet new`:
   - Class library: `dotnet new classlib`
   - Azure Functions: `dotnet new func`
   - xUnit test project: `dotnet new xunit`
   - Use the latest .NET version available

3. **Add to solution:**
   ```
   dotnet sln add <path-to-csproj>
   ```

4. **Add standard project references** based on layer:
   - `Application` → references `Domain`
   - `Infrastructure` → references `Application`, `Domain`
   - `Api` → references `Application`, `Infrastructure`
   - `Web` → references `Application`, `Domain`
   - `Unit.Tests` → references the project under test
   - `Integration.Tests` → references `Api`, `Infrastructure`
   - `Feature.Tests` → no src references (tests via HTTP)

5. **Install standard NuGet packages** based on project type (always latest stable versions):
   - All source projects: nothing by default
   - `Application`: `MediatR`, `FluentValidation.DependencyInjectionExtensions`
   - `Infrastructure`: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Tools`
   - `Api`: `Microsoft.Azure.Functions.Worker`, `Microsoft.Azure.Functions.Worker.Extensions.Http`
   - `Unit.Tests`: `Microsoft.EntityFrameworkCore.InMemory`, `xunit`, `FluentAssertions`
   - `Integration.Tests`: `Testcontainers.MsSql`, `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`
   - `Feature.Tests`: `Microsoft.Playwright`, `FluentAssertions`

6. **Clean up** the default generated files (Class1.cs, UnitTest1.cs, etc.) before presenting the result.

7. **Confirm** the project builds: `dotnet build`
