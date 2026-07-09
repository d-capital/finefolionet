# Finefolio Valuation API

.NET 10 Web API that runs Evolve migrations and exposes a single GET endpoint:

{url}/valuation/en/MOEX/SBER

Quick start

1. Ensure PostgreSQL is running and reachable. Update `appsettings.json` `ConnectionStrings:DefaultConnection` if needed.
2. From the `valuation-api` folder run:

```bash
dotnet restore
dotnet run --project valuation-api.csproj
```

The app will apply SQL migrations from `db/migrations` using Evolve at startup.
