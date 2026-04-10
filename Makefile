.PHONY: build test test-unit test-integration test-feature up down logs migration update-db

build:
	dotnet build FootballPlanner.slnx

test: test-unit test-integration

test-unit:
	dotnet test tests/FootballPlanner.Unit.Tests --configuration Release

test-integration:
	dotnet test tests/FootballPlanner.Integration.Tests --configuration Release

test-feature:
	dotnet test tests/FootballPlanner.Feature.Tests --configuration Release

up:
	docker compose up -d

down:
	docker compose down

logs:
	docker compose logs -f

migration:
	@test -n "$(NAME)" || (echo "Usage: make migration NAME=<MigrationName>" && exit 1)
	dotnet ef migrations add $(NAME) \
		--project src/FootballPlanner.Infrastructure \
		--startup-project src/FootballPlanner.Infrastructure

update-db:
	dotnet ef database update \
		--project src/FootballPlanner.Infrastructure \
		--startup-project src/FootballPlanner.Infrastructure
