# Local Development Docker Setup Design

## Goal

Run the full application stack locally using Docker Compose, with containers that faithfully emulate the Azure production environment (Azure Static Web Apps + Azure Functions).

## Architecture

Three containers orchestrated by Docker Compose:

- **`db`** — SQL Server 2022. Persistent volume, healthcheck before dependent services start.
- **`api`** — Azure Functions isolated worker (.NET 10), started with Azure Functions Core Tools (`func start --no-build`). Auto-runs EF migrations on startup.
- **`swa`** — Azure Static Web Apps CLI (`swa start`). Serves the published Blazor WASM static files and proxies `/api/*` requests to the `api` container. Listens on port 4280.

The SWA CLI intercepts `/api/*` from the browser and proxies to `http://api:7071`, exactly as Azure Static Web Apps does in production. The browser only communicates with `http://localhost:4280`.

## Configuration

Secrets are stored in a `.env` file (gitignored). A `.env.example` is committed as a template:

```
DB_PASSWORD=YourStrongPassword123!
AUTH0_DOMAIN=your-tenant.auth0.com
AUTH0_AUDIENCE=your-api-identifier
```

The `api` container receives configuration via environment variables using .NET's double-underscore nested key convention:
- `ConnectionStrings__DefaultConnection` — `Server=tcp:db,1433;Database=FootballPlanner;User Id=sa;Password=${DB_PASSWORD};TrustServerCertificate=True`
- `Auth0__Domain` — Auth0 tenant domain
- `Auth0__Audience` — Auth0 API audience

**Auth0 requirement:** The `AuthMiddleware` fetches `https://{Auth0Domain}/.well-known/openid-configuration` on every request to validate JWTs. A real Auth0 dev application with a valid domain and audience is required. Developers must create a dev Auth0 tenant and populate the `.env` file with their credentials before running the stack.

No `local.settings.json` is needed when running via Docker. A `local.settings.json.example` is committed for developers who want to run `func start` directly outside Docker.

## API Container

Multi-stage Dockerfile:
1. **Build stage** — `mcr.microsoft.com/dotnet/sdk:10.0`, runs `dotnet publish` on `FootballPlanner.Api`
2. **Runtime stage** — `mcr.microsoft.com/dotnet/aspnet:10.0` with Azure Functions Core Tools v4 installed via the Microsoft apt repository

Entry point: `func start --no-build` from the published output directory. `host.json` is committed alongside the project (required by the Functions host) with the following content:

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

The `api` service uses `depends_on: db: condition: service_healthy` to ensure SQL Server is accepting connections before the Functions host starts.

## SWA Container

Multi-stage Dockerfile:
1. **Build stage** — `mcr.microsoft.com/dotnet/sdk:10.0`, runs `dotnet publish` on `FootballPlanner.Web`
2. **Runtime stage** — `node:20-slim` (Node 20+ required by `@azure/static-web-apps-cli` v1.x) with `@azure/static-web-apps-cli` installed globally

The published Blazor WASM `wwwroot` output is copied into the image. Entry point: `swa start /app/wwwroot --api-location http://api:7071 --host 0.0.0.0`.

## Blazor WASM API URL

The `ApiBaseUrl` is currently hardcoded to `http://localhost:7071/api` in `wwwroot/appsettings.json`. This is incorrect for the SWA pattern — the browser should call the SWA proxy, not Functions directly.

**Fix:** Change the `Program.cs` fallback to derive the API URL from the host the app is served from:

```csharp
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? $"{builder.HostEnvironment.BaseAddress}api";
```

Remove the hardcoded URL from `wwwroot/appsettings.json`. This works for both local SWA (port 4280) and production Azure SWA with no configuration changes.

## CORS

Since the browser only communicates with the SWA container (port 4280), and the SWA CLI proxies to Functions internally, CORS is not needed between the browser and the API in normal usage. However, CORS is configured on the Functions app to allow requests from the SWA origin in case the API is called directly during development.

In `Program.cs` (Api), register CORS before building the host:

```csharp
.ConfigureServices((context, services) =>
{
    var allowedOrigins = context.Configuration["AllowedOrigins"] ?? "http://localhost:4280";
    services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(allowedOrigins.Split(','))
                  .AllowAnyHeader()
                  .AllowAnyMethod()));
    ...
})
.ConfigureFunctionsWebApplication(worker =>
{
    worker.UseCors();
    worker.UseMiddleware<AuthMiddleware>();
})
```

`AllowedOrigins` is set in `docker-compose.yml` as an environment variable on the `api` service (`http://localhost:4280` for local dev).

## Auto-Migration

`Program.cs` (Api) runs `db.Database.MigrateAsync()` in a scoped service before `host.Run()`. This ensures the schema is always current when the container starts, without requiring a manual migration step.

## Files

**New files:**
- `src/FootballPlanner.Api/host.json`
- `src/FootballPlanner.Api/local.settings.json.example`
- `src/FootballPlanner.Api/Dockerfile`
- `src/FootballPlanner.Web/Dockerfile`
- `docker-compose.yml`
- `.env.example`

**Modified files:**
- `src/FootballPlanner.Api/Program.cs` — CORS + auto-migration
- `src/FootballPlanner.Web/Program.cs` — `ApiBaseUrl` derived from `HostEnvironment.BaseAddress`
- `src/FootballPlanner.Web/wwwroot/appsettings.json` — remove hardcoded `localhost:7071`
- `.gitignore` — add `.env` and `local.settings.json`
