# AiCopilot (.NET 8 Clean Architecture)

This repository contains a production-ready starter solution using Clean Architecture with:

- **AiCopilot.Api** - ASP.NET Core Web API entry point
- **AiCopilot.Application** - business use cases and interfaces
- **AiCopilot.Domain** - core domain entities
- **AiCopilot.Infrastructure** - external dependency implementations
- **AiCopilot.Worker** - background jobs using hosted services
- **AiCopilot.Shared** - contracts shared between layers

## Architecture boundaries

- `Api` depends on `Application`, `Infrastructure`, `Shared`
- `Application` depends on `Domain`, `Shared`
- `Infrastructure` depends on `Application`, `Shared`
- `Worker` depends on `Application`, `Infrastructure`
- `Domain` and `Shared` have no outgoing project dependencies

## Cross-cutting setup

- Dependency injection via layer-specific `AddApplication` and `AddInfrastructure` extension methods
- Logging via **Serilog** in both API and Worker
- Configuration through `appsettings*.json` with strongly-typed options
- Docker support with per-project Dockerfiles and root `docker-compose.yml`

## Run locally

```bash
dotnet restore
dotnet build
```

Run API:

```bash
dotnet run --project src/AiCopilot.Api
```

Run worker:

```bash
dotnet run --project src/AiCopilot.Worker
```

## Run with Docker

```bash
docker compose up --build
```

Services started by Compose:

- API on `http://localhost:5000`
- Worker as a background container
- PostgreSQL with `pgvector` on `localhost:5432`
- Redis on `localhost:6379`
- Seed job as a one-shot container that inserts sample local data after the API is healthy

Important container environment wiring:

- `ConnectionStrings__PlmDatabase=Host=postgres;Port=5432;...`
- `PlmApi__BaseUrl=http://api:8080/`
- `Redis__ConnectionString=redis:6379`
- JWT, tenant, and sample auth users are injected through compose environment variables

Health checks are configured for:

- API via `GET /health`
- Worker via process check
- PostgreSQL via `pg_isready`
- Redis via `redis-cli ping`

Container startup notes:

- The API persists ASP.NET Core data-protection keys in a Docker volume so auth/session protection keys survive container restarts.
- The worker uses the ASP.NET Core runtime image because the app depends on `Microsoft.AspNetCore.App`.
- The seed container now waits until the migrated schema exists before running the SQL script, and it stops on the first SQL error.

## Local Docker startup flow

1. Start the full stack:

```bash
docker compose up --build
```

2. Wait until these services are healthy:

- `aicopilot-postgres`
- `aicopilot-redis`
- `aicopilot-api`
- `aicopilot-worker`

3. Let the `aicopilot-seed` container complete once. It inserts sample parts, BOM data, documents, embeddings, and digital twin data for local testing.

If you changed the compose stack after a failed startup, rebuild and recreate everything cleanly:

```bash
docker compose down -v
docker compose up --build
```

4. Open the API at:

```text
http://localhost:5000
```

5. Optional health check:

```bash
curl http://localhost:5000/health
```

## Seed data

The local seed script is:

- [`docker/seed/seed-data.sql`](/C:/Users/ShridharNanavare/AICopilotPLM/docker/seed/seed-data.sql)

It adds sample tenant-scoped data for:

- parts
- BOM rows
- documents
- embeddings
- part features
- digital twin state

If you need to rerun the seed manually after the stack is up:

```bash
docker compose run --rm seed
```
