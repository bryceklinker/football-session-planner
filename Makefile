.PHONY: build test test-unit test-integration test-component test-feature ci up down logs migration update-db

build:
	dotnet build FootballPlanner.slnx --configuration Release

ci: build test

test: test-unit test-integration test-component up wait test-feature down logs

test-unit:
	dotnet test tests/FootballPlanner.Unit.Tests --configuration Release --logger trx

test-integration:
	dotnet test tests/FootballPlanner.Integration.Tests --configuration Release --logger trx

test-component:
	dotnet test tests/FootballPlanner.Component.Tests --configuration Release --logger trx

test-feature:
	dotnet test tests/FootballPlanner.Feature.Tests --configuration Release --logger trx

up:
	docker compose up -d --build

down:
	docker compose down

wait:
	curl --retry 30 --retry-delay 5 --retry-all-errors -s http://localhost:7071/api/health > /dev/null
	curl --retry 20 --retry-delay 5 --retry-all-errors -s http://localhost:4280/ > /dev/null

logs:
	docker compose logs --no-color

migration:
	@test -n "$(NAME)" || (echo "Usage: make migration NAME=<MigrationName>" && exit 1)
	dotnet ef migrations add $(NAME) \
		--project src/FootballPlanner.Infrastructure \
		--startup-project src/FootballPlanner.Infrastructure

update-db:
	dotnet ef database update \
		--project src/FootballPlanner.Infrastructure \
		--startup-project src/FootballPlanner.Infrastructure
