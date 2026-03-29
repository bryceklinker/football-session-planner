# Add Migration

Add a new EF Core migration to the Football Session Planner.

## Steps

1. **Confirm pending changes** — check that the domain entities and EF Core configurations in `FootballPlanner.Infrastructure` reflect the intended schema change before generating a migration.

2. **Generate the migration:**
   ```bash
   dotnet ef migrations add <MigrationName> \
     --project src/FootballPlanner.Infrastructure \
     --startup-project src/FootballPlanner.Api
   ```
   Use a descriptive PascalCase name that describes what changed (e.g., `AddSessionActivityKeyPoints`, `AddEstimatedDurationToActivity`).

3. **Review the generated migration** — open the generated `.cs` file and verify:
   - The `Up()` method reflects the intended changes
   - The `Down()` method correctly reverses them
   - No unintended table drops or column removals

4. **Apply to local development database** (if one exists):
   ```bash
   dotnet ef database update \
     --project src/FootballPlanner.Infrastructure \
     --startup-project src/FootballPlanner.Api
   ```

5. **Update integration tests** if the schema change affects test database setup in `FootballPlanner.Integration.Tests`.

## Naming Conventions

- Describe the change, not the date: `AddPhaseTable`, `AddNotesToSessionActivity`
- For initial schema: `InitialCreate`
- For reference data only (no schema change): do not create a migration — seed data via `HasData()` in existing configurations or a dedicated seeder
