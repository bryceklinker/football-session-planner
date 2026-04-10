# Build stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FootballPlanner.Api/FootballPlanner.Api.csproj \
    -c Release -o /app/publish

# Runtime stage
FROM --platform=linux/amd64 mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Install Azure Functions Core Tools v4
RUN apt-get update && \
    apt-get install -y curl gnupg2 && \
    curl -sL https://packages.microsoft.com/keys/microsoft.asc | \
        gpg --dearmor > /etc/apt/trusted.gpg.d/microsoft.gpg && \
    echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-bookworm-prod bookworm main" \
        > /etc/apt/sources.list.d/dotnetdev.list && \
    apt-get update && \
    apt-get install -y azure-functions-core-tools-4 && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish /home/site/wwwroot
# Remove the SDK-generated local.settings.json so func start reads
# AzureWebJobsStorage and other settings from environment variables instead.
RUN rm -f /home/site/wwwroot/local.settings.json
WORKDIR /home/site/wwwroot
EXPOSE 7071

CMD ["func", "start", "--no-build", "--verbose"]
