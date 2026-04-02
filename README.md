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
