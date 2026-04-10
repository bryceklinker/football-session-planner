# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/FootballPlanner.Web/FootballPlanner.Web.csproj \
    -c Release -o /app/publish

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
     "--api-devserver-url", "http://api:7071"]
