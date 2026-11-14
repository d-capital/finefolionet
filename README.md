# Finefolio Valuation API

.NET 10 Web API that runs Evolve migrations and exposes valuation and asset endpoints.

{url}/valuation/en/MOEX/SBER

Stock screener

`GET {url}/valuation/screener?minDividend=2&minEps=15`

Optionally restrict results to one exchange with `&exchange=NASDAQ`; omit the
parameter (or leave it blank) to search all exchanges. The `minEps` parameter
filters for EPS greater than the supplied value and defaults to `15`. Returns up to ten
candidates ranked by five-year net-income CAGR descending. Candidates have a market capitalization
from 0 through 5 billion, P/E no greater than `maxPe`, EPS greater than `minEps`,
and dividend yield at least `minDividend`. They must also have a positive stored
net-income five-year CAGR. The response includes the stock name, ticker,
exchange, price, EPS, and `netIncome5yCagr` (a decimal rate, e.g. `0.12` for 12%).

Historical free cash flow

Use `POST {url}/valuation/{exchange}/{ticker}/free-cash-flow` to add or update an
annual free-cash-flow value. The request body uses the `year` and `value` fields.
DCF valuations use this history to calculate the average FCF growth rate used
for projections.

Asset fundamentals

Use `PATCH {url}/asset-fundamentals/{exchange}/{ticker}` to update asset
fundamentals. The request may include `peg`, `netIncome5yCagr`, and
`isBroadIndexPart`; omitted fields are left unchanged. These values are
available in the valuation response (`valuation.peg`,
`valuation.averagePeg`,
`stockInfo.netIncome5yCagr`, and `stockInfo.isBroadIndexPart`).
The five-year net-income CAGR is calculated from the latest five available
annual observations, matching the valuation UI.
For stocks marked as broad-index constituents, `valuation.averagePeg` is the
average PEG of marked NYSE/NASDAQ stocks (S&P 500 group) or marked MOEX stocks,
depending on the stock's exchange. The averages are stored separately and
refreshed by the nightly metrics job. The individual `valuation.peg` is unchanged.

Quick start

1. Ensure PostgreSQL is running and reachable. Update `appsettings.json` `ConnectionStrings:DefaultConnection` if needed.
2. From the `valuation-api` folder run:

```bash
dotnet restore
dotnet run --project valuation-api.csproj
```

The app will apply SQL migrations from `db/migrations` using Evolve at startup.
The hosted asset price update runs daily at 4:00 a.m. Moscow time. A separate
hosted job recalculates net-income five-year CAGR and PEG for broad-index stocks
daily at 4:30 a.m. Moscow time.
