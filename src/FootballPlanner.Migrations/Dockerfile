# Build stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer — only re-runs when project files change
COPY src/FootballPlanner.Domain/FootballPlanner.Domain.csproj src/FootballPlanner.Domain/
COPY src/FootballPlanner.Infrastructure/FootballPlanner.Infrastructure.csproj src/FootballPlanner.Infrastructure/
COPY src/FootballPlanner.Migrations/FootballPlanner.Migrations.csproj src/FootballPlanner.Migrations/
RUN dotnet restore src/FootballPlanner.Migrations/FootballPlanner.Migrations.csproj

# Build layer — only re-runs when source changes
COPY src/ src/
RUN dotnet publish src/FootballPlanner.Migrations/FootballPlanner.Migrations.csproj \
    -c Release --no-restore -o /app/publish

# Runtime stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
COPY --from=build /app/publish /app
WORKDIR /app
ENTRYPOINT ["dotnet", "FootballPlanner.Migrations.dll"]
