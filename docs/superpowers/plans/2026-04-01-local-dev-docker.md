# Local Dev Docker Setup Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Run the full stack locally via Docker Compose using Azure Functions Core Tools for the API and the SWA CLI for the Blazor WASM frontend, faithfully emulating Azure Static Web Apps.

**Architecture:** Three containers — SQL Server 2022, Azure Functions isolated worker (`.NET 10` + `func start`), and an SWA CLI container serving the published Blazor WASM and proxying `/api/*` to the Functions container. The browser talks only to the SWA CLI on port 4280; the SWA CLI handles API proxying internally.

**Tech Stack:** Docker Compose, Azure Functions Core Tools v4, `@azure/static-web-apps-cli`, .NET 10, SQL Server 2022, Blazor WebAssembly

---

## File Map

**New files:**
- `src/FootballPlanner.Api/host.json` — Functions host configuration (version, logging, route prefix)
- `src/FootballPlanner.Api/local.settings.json.example` — template for running `func start` outside Docker
- `src/FootballPlanner.Api/Dockerfile` — multi-stage: .NET 10 SDK build → aspnet:10.0 + func tools runtime
- `src/FootballPlanner.Web/Dockerfile` — multi-stage: .NET 10 SDK build → node:20-slim + SWA CLI runtime
- `docker-compose.yml` — orchestrates db + api + swa
- `.env.example` — committed secrets template

**Modified files:**
- `src/FootballPlanner.Api/FootballPlanner.Api.csproj` — copy `host.json` to publish output
- `src/FootballPlanner.Api/Program.cs` — add CORS middleware + auto-migration before `host.Run()`
- `src/FootballPlanner.Web/Program.cs` — derive `ApiBaseUrl` from `HostEnvironment.BaseAddress`
- `src/FootballPlanner.Web/wwwroot/appsettings.json` — remove hardcoded `localhost:7071` URL
- `.gitignore` — add `.env`

---

## Chunk 1: API configuration

### Task 1: host.json, local.settings.json.example, and .csproj output config

**Files:**
- Create: `src/FootballPlanner.Api/host.json`
- Create: `src/FootballPlanner.Api/local.settings.json.example`
- Modify: `src/FootballPlanner.Api/FootballPlanner.Api.csproj`

- [ ] **Step 1: Create host.json**

Create `src/FootballPlanner.Api/host.json`:
```json
{
  "version": "2.0",
  "logging": {
    "applicationInsights": {
      "samplingSettings": {
        "isEnabled": true,
        "excludedTypes": "Request"
      }
    }
  },
  "extensions": {
    "http": {
      "routePrefix": "api"
    }
  }
}
```

- [ ] **Step 2: Create local.settings.json.example**

Create `src/FootballPlanner.Api/local.settings.json.example`:
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "DOTNET_ENVIRONMENT": "Development",
    "Auth0__Domain": "your-tenant.auth0.com",
    "Auth0__Audience": "your-api-identifier",
    "AllowedOrigins": "http://localhost:4280"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FootballPlanner;User Id=sa;Password=<your-db-password>;TrustServerCertificate=True"
  }
}
```

- [ ] **Step 3: Update AppDbContextFactory.cs to use Docker SQL Server**

Read `src/FootballPlanner.Infrastructure/AppDbContextFactory.cs`. It currently hardcodes `(localdb)\\mssqllocaldb`. Replace the connection string to read from the `ConnectionStrings__DefaultConnection` environment variable with the Docker SQL Server connection as fallback:

```csharp
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "Server=localhost,1433;Database=FootballPlanner;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True";
optionsBuilder.UseSqlServer(connectionString);
```

This ensures `dotnet ef migrations add` works when the Docker SQL Server container is running.

- [ ] **Step 4: Update .csproj to copy host.json to publish output**

Read `src/FootballPlanner.Api/FootballPlanner.Api.csproj`. Add an `<ItemGroup>` that marks `host.json` for copy to output (the `Microsoft.NET.Sdk.Worker` SDK does not copy it automatically):

```xml
  <ItemGroup>
    <None Update="host.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
```

Add this after the existing `<ItemGroup>` blocks (before `</Project>`).

- [ ] **Step 5: Build to verify**

```bash
dotnet build src/FootballPlanner.Api 2>&1 | tail -5
```
Expected: Build succeeded.

Verify `host.json` appears in output:
```bash
ls src/FootballPlanner.Api/bin/Debug/net10.0/host.json
```
Expected: file exists.

- [ ] **Step 6: Commit**

```bash
git add src/FootballPlanner.Api/host.json \
        src/FootballPlanner.Api/local.settings.json.example \
        src/FootballPlanner.Api/FootballPlanner.Api.csproj \
        src/FootballPlanner.Infrastructure/AppDbContextFactory.cs
git commit -m "feat: add host.json, local.settings.json.example, and update AppDbContextFactory for Docker SQL Server"
```

---

### Task 2: CORS middleware and auto-migration in API Program.cs

**Files:**
- Modify: `src/FootballPlanner.Api/Program.cs`

- [ ] **Step 1: Read current Program.cs**

Read `src/FootballPlanner.Api/Program.cs`. The current file has a `HostBuilder` with `ConfigureFunctionsWebApplication` (registers `AuthMiddleware`) and `ConfigureServices` (registers `AddApplication()` + `AddInfrastructure()`).

- [ ] **Step 2: Replace Program.cs with updated version**

Replace the entire contents with:
```csharp
using FootballPlanner.Api.Middleware;
using FootballPlanner.Application;
using FootballPlanner.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(worker =>
    {
        worker.UseCors();
        worker.UseMiddleware<AuthMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        var allowedOrigins = context.Configuration["AllowedOrigins"] ?? "http://localhost:4280";
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins(allowedOrigins.Split(','))
                      .AllowAnyHeader()
                      .AllowAnyMethod()));
        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
    })
    .Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

await host.RunAsync();
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build src/FootballPlanner.Api 2>&1 | tail -5
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run unit tests to confirm nothing broken**

```bash
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -5
```
Expected: 49 passed.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Api/Program.cs
git commit -m "feat: add CORS middleware and auto-migration to API startup"
```

---

## Chunk 2: Web changes

### Task 3: Fix ApiBaseUrl and update appsettings.json and .gitignore

**Files:**
- Modify: `src/FootballPlanner.Web/Program.cs`
- Modify: `src/FootballPlanner.Web/wwwroot/appsettings.json`
- Modify: `.gitignore`

- [ ] **Step 1: Read current Web Program.cs**

Read `src/FootballPlanner.Web/Program.cs`. The current file has:
```csharp
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7071/api";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
```

- [ ] **Step 2: Update ApiBaseUrl fallback**

Replace the `apiBaseUrl` line:
```csharp
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? $"{builder.HostEnvironment.BaseAddress}api";
```

The full updated `src/FootballPlanner.Web/Program.cs`:
```csharp
using FootballPlanner.Web;
using FootballPlanner.Web.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? $"{builder.HostEnvironment.BaseAddress}api";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ApiClient>();

await builder.Build().RunAsync();
```

- [ ] **Step 3: Update wwwroot/appsettings.json**

Replace `src/FootballPlanner.Web/wwwroot/appsettings.json` with (remove the `ApiBaseUrl` key — it now falls back to `HostEnvironment.BaseAddress` when not set):
```json
{
  "Auth0": {
    "Domain": "",
    "ClientId": "",
    "Audience": ""
  }
}
```

- [ ] **Step 4: Add .env to .gitignore**

Read `.gitignore`. Add `.env` to it (it already has `local.settings.json`). The updated `.gitignore`:
```
bin/
obj/
.vs/
*.user
local.settings.json
.env
.pulumi/
```

- [ ] **Step 5: Build to verify**

```bash
dotnet build src/FootballPlanner.Web 2>&1 | tail -5
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add src/FootballPlanner.Web/Program.cs \
        src/FootballPlanner.Web/wwwroot/appsettings.json \
        .gitignore
git commit -m "feat: derive API base URL from SWA host instead of hardcoded localhost"
```

---

## Chunk 3: Dockerfiles

### Task 4: API Dockerfile

**Files:**
- Create: `src/FootballPlanner.Api/Dockerfile`

The Dockerfile must:
1. Build the published API output using the .NET 10 SDK
2. Install Azure Functions Core Tools v4 via the Microsoft apt repository in the runtime stage
3. Copy the published output to `/home/site/wwwroot`
4. Start with `func start --no-build`

- [ ] **Step 1: Create the Dockerfile**

Create `src/FootballPlanner.Api/Dockerfile`:
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FootballPlanner.Api/FootballPlanner.Api.csproj \
    -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

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
WORKDIR /home/site/wwwroot
EXPOSE 7071

CMD ["func", "start", "--no-build", "--verbose"]
```

Note: The Docker build context must be the repo root (not `src/FootballPlanner.Api/`) so the COPY copies the full solution. This is handled by `context: .` in `docker-compose.yml`.

- [ ] **Step 2: Build the image to verify**

```bash
docker build -f src/FootballPlanner.Api/Dockerfile -t football-api:local . 2>&1 | tail -20
```
Expected: `Successfully built <id>` and `Successfully tagged football-api:local`.

This will take several minutes on first run (downloading images, installing func tools).

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Api/Dockerfile
git commit -m "feat: add API Dockerfile using Azure Functions Core Tools"
```

---

### Task 5: SWA Dockerfile

**Files:**
- Create: `src/FootballPlanner.Web/Dockerfile`

The Dockerfile must:
1. Build the published Blazor WASM output using the .NET 10 SDK
2. Use `node:20-slim` as the runtime with `@azure/static-web-apps-cli` installed
3. Copy the published `wwwroot` into the image
4. Start with `swa start` pointing at the wwwroot and proxying `/api/*` to the `api` service

- [ ] **Step 1: Create the Dockerfile**

Create `src/FootballPlanner.Web/Dockerfile`:
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FootballPlanner.Web/FootballPlanner.Web.csproj \
    -c Release -o /app/publish

# Runtime stage
FROM node:20-slim AS runtime
RUN npm install -g @azure/static-web-apps-cli

COPY --from=build /app/publish/wwwroot /app/wwwroot
WORKDIR /app
EXPOSE 4280

CMD ["swa", "start", "/app/wwwroot", \
     "--host", "0.0.0.0", \
     "--port", "4280", \
     "--api-location", "http://api:7071"]
```

Note: `--host 0.0.0.0` is required so the SWA CLI binds to all interfaces inside the container (not just localhost). The `--api-location` uses the Docker Compose service name `api` which resolves to the Functions container on the internal network.

- [ ] **Step 2: Build the image to verify**

```bash
docker build -f src/FootballPlanner.Web/Dockerfile -t football-swa:local . 2>&1 | tail -20
```
Expected: `Successfully built <id>` and `Successfully tagged football-swa:local`.

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Web/Dockerfile
git commit -m "feat: add SWA Dockerfile using Azure Static Web Apps CLI"
```

---

## Chunk 4: Docker Compose and smoke test

### Task 6: docker-compose.yml and .env.example

**Files:**
- Create: `docker-compose.yml`
- Create: `.env.example`

- [ ] **Step 1: Create .env.example**

Create `.env.example` at the repo root:
```
# Copy this file to .env and fill in your values
# .env is gitignored — never commit it

# SQL Server SA password (min 8 chars, must include uppercase, lowercase, digit, symbol)
DB_PASSWORD=YourStrongPassword123!

# Auth0 dev application credentials
# Create a Machine-to-Machine application in your Auth0 dashboard
AUTH0_DOMAIN=your-tenant.auth0.com
AUTH0_AUDIENCE=https://your-api-identifier
```

- [ ] **Step 2: Create docker-compose.yml**

Create `docker-compose.yml` at the repo root:
```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "${DB_PASSWORD}"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test:
        - "CMD-SHELL"
        - "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$SA_PASSWORD\" -Q 'SELECT 1' -No || exit 1"
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s

  api:
    build:
      context: .
      dockerfile: src/FootballPlanner.Api/Dockerfile
    ports:
      - "7071:7071"
    environment:
      ConnectionStrings__DefaultConnection: "Server=tcp:db,1433;Database=FootballPlanner;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True"
      Auth0__Domain: "${AUTH0_DOMAIN}"
      Auth0__Audience: "${AUTH0_AUDIENCE}"
      AllowedOrigins: "http://localhost:4280"
      DOTNET_ENVIRONMENT: "Development"
      FUNCTIONS_WORKER_RUNTIME: "dotnet-isolated"
    depends_on:
      db:
        condition: service_healthy

  swa:
    build:
      context: .
      dockerfile: src/FootballPlanner.Web/Dockerfile
    ports:
      - "4280:4280"
    depends_on:
      - api

volumes:
  sqldata:
```

- [ ] **Step 3: Verify compose config parses correctly**

```bash
docker compose config 2>&1 | head -20
```
Expected: parsed YAML output with no errors. (You will need a `.env` file with real values — copy from `.env.example` first.)

```bash
cp .env.example .env
# Edit .env and fill in your DB_PASSWORD, AUTH0_DOMAIN, AUTH0_AUDIENCE
```

- [ ] **Step 4: Commit**

```bash
git add docker-compose.yml .env.example
git commit -m "feat: add Docker Compose for local development with SWA CLI and Azure Functions"
```

---

### Task 7: Smoke test

This task has no code changes. It verifies the full stack starts and the application is reachable.

- [ ] **Step 1: Start the stack**

```bash
docker compose up --build -d
```

First run will take 5–10 minutes (downloading images, installing tools, building .NET projects). Subsequent runs are faster.

- [ ] **Step 2: Watch migrations run**

```bash
docker compose logs api --follow
```
Expected output includes lines like:
```
Applying migration '20260401013943_AddSession'...
...
Host initialized. Waiting for a worker connection
```

Press Ctrl+C to stop following logs once the host is ready.

- [ ] **Step 3: Check all containers are running**

```bash
docker compose ps
```
Expected: `db`, `api`, and `swa` all show `running` (or `healthy` for db).

- [ ] **Step 4: Open the app in a browser**

Navigate to `http://localhost:4280`.

Expected: The Blazor WASM app loads (may show a blank page or login prompt depending on Auth0 configuration).

- [ ] **Step 5: Verify the API is reachable through SWA**

```bash
curl -s -o /dev/null -w "%{http_code}" http://localhost:4280/api/activities \
  -H "Authorization: Bearer <your-auth0-token>"
```
Expected: `200` (with a valid token) or `401` (without one — proves the API is reachable and auth is working).

Without a token:
```bash
curl -s http://localhost:4280/api/activities
```
Expected: `{"error":"Unauthorized"}` — confirms the SWA proxy is routing to the Functions app correctly.

- [ ] **Step 6: Stop the stack**

```bash
docker compose down
```

To also remove the database volume (clean slate):
```bash
docker compose down -v
```
