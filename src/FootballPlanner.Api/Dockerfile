# Build stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FootballPlanner.Api/FootballPlanner.Api.csproj \
    -c Release -o /app/publish

# Runtime stage
FROM --platform=linux/amd64 mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0 AS runtime

COPY --from=build /app/publish /home/site/wwwroot
EXPOSE 80
