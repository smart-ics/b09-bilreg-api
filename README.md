# b09-bilreg-api

Hospital billing / registration API (Bilreg).

## Documentation

All engineering and domain artifacts are centralized under [`docs/`](docs/).

**Start here:** [docs/ARTIFACTS.md](docs/ARTIFACTS.md) — canonical index, migration map, and AI prompt recipe.

For Cursor / coding agents, see also [AGENTS.md](AGENTS.md).

## Taksaka V2 (background processing platform)

Taksaka V2 is the operational background processing platform scaffold. Domain and architecture artifacts:

- [docs/contexts/taksaka/taksaka-01-domain.md](docs/contexts/taksaka/taksaka-01-domain.md)
- [docs/contexts/taksaka/taksaka-02-architecture.md](docs/contexts/taksaka/taksaka-02-architecture.md)

### Prerequisites

- .NET 9 SDK
- Node.js 20+
- SQL Server (optional for scaffold — not required to start the API)

### Backend

```bash
cd src/taksaka.backend
dotnet build Taksaka.sln
dotnet run --project Taksaka.Server
```

- Swagger: http://localhost:5000/swagger
- Health: http://localhost:5000/health
- API health: http://localhost:5000/api/health
- SignalR hub: http://localhost:5000/hubs/operations

### Frontend (Operator Console)

```bash
cd src/taksaka.frontend/Taksaka.Web
npm install
npm run dev
```

- Dev server: http://localhost:5173

### Tests

```bash
cd src/taksaka.backend
dotnet test Taksaka.sln
```
