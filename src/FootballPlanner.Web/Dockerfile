# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore layer — only re-runs when project files change
COPY global.json .
COPY src/FootballPlanner.Web/FootballPlanner.Web.csproj src/FootballPlanner.Web/
RUN dotnet restore src/FootballPlanner.Web/FootballPlanner.Web.csproj

# Build layer — only re-runs when source changes
COPY src/FootballPlanner.Web/ src/FootballPlanner.Web/
RUN dotnet publish src/FootballPlanner.Web/FootballPlanner.Web.csproj \
    -c Release --no-restore -o /app/publish

# Runtime stage
FROM node:24-slim AS runtime
RUN npm install -g @azure/static-web-apps-cli

COPY --from=build /app/publish/wwwroot /app/wwwroot
WORKDIR /app

# Inject Auth0 config into appsettings.json at image build time
ARG AUTH0_DOMAIN
ARG AUTH0_CLIENT_ID
ARG AUTH0_AUDIENCE
RUN sed -i \
    -e "s|__AUTH0_DOMAIN__|${AUTH0_DOMAIN}|g" \
    -e "s|__AUTH0_CLIENT_ID__|${AUTH0_CLIENT_ID}|g" \
    -e "s|__AUTH0_AUDIENCE__|${AUTH0_AUDIENCE}|g" \
    /app/wwwroot/appsettings.json

EXPOSE 4280

CMD ["swa", "start", "/app/wwwroot", \
     "--host", "0.0.0.0", \
     "--port", "4280", \
     "--api-devserver-url", "http://api:80"]
