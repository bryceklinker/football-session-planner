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
     "--api-devserver-url", "http://api:7071"]
