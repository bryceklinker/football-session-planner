# Build stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FootballPlanner.Migrations/FootballPlanner.Migrations.csproj \
    -c Release -o /app/publish

# Runtime stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
COPY --from=build /app/publish /app
WORKDIR /app
ENTRYPOINT ["dotnet", "FootballPlanner.Migrations.dll"]
