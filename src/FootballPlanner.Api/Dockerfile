# Build stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer — only re-runs when project files change
COPY global.json .
COPY src/FootballPlanner.Domain/FootballPlanner.Domain.csproj src/FootballPlanner.Domain/
COPY src/FootballPlanner.Infrastructure/FootballPlanner.Infrastructure.csproj src/FootballPlanner.Infrastructure/
COPY src/FootballPlanner.Application/FootballPlanner.Application.csproj src/FootballPlanner.Application/
COPY src/FootballPlanner.Api/FootballPlanner.Api.csproj src/FootballPlanner.Api/
RUN dotnet restore src/FootballPlanner.Api/FootballPlanner.Api.csproj

# Build layer — only re-runs when source changes
COPY src/ src/
RUN dotnet publish src/FootballPlanner.Api/FootballPlanner.Api.csproj \
    -c Release --no-restore -o /app/publish

# Runtime stage
FROM --platform=linux/amd64 mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS runtime
COPY --from=build /app/publish /home/site/wwwroot
EXPOSE 80
