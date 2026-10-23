# Finefolio Valuation API

.NET 10 Web API that runs Evolve migrations and exposes valuation and asset endpoints.

{url}/valuation/en/MOEX/SBER

Stock screener

`GET {url}/valuation/screener?maxPe=20&minDividend=2&minEps=15`

Optionally restrict results to one exchange with `&exchange=NASDAQ`; omit the
parameter (or leave it blank) to search all exchanges. The `minEps` parameter
filters for EPS greater than the supplied value and defaults to `15`. Returns up to ten
candidates ranked by EPS descending. Candidates have a market capitalization
from 0 through 5 billion, P/E no greater than `maxPe`, EPS greater than `minEps`,
dividend yield at least `minDividend`, and debt-to-equity below 0.5. They must
also have net-income data for the latest reported year and exactly five years
before it, with the latest value greater than the earlier value. The response
includes ticker, exchange, price, P/E, and EPS.

Historical free cash flow

Use `POST {url}/valuation/{exchange}/{ticker}/free-cash-flow` to add or update an
annual free-cash-flow value. The request body uses the `year` and `value` fields.
DCF valuations use this history to calculate the average FCF growth rate used
for projections.

Quick start

1. Ensure PostgreSQL is running and reachable. Update `appsettings.json` `ConnectionStrings:DefaultConnection` if needed.
2. From the `valuation-api` folder run:

```bash
dotnet restore
dotnet run --project valuation-api.csproj
```

The app will apply SQL migrations from `db/migrations` using Evolve at startup.
