using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Services.PriceProviders;
using System.Globalization;

namespace Finefolio.ValuationApi.Services.PriceProviders
{
    public class TradingViewPriceProvider : ITradingViewPriceProvider
    {
        private readonly IHttpClientFactory _httpFactory;

        public TradingViewPriceProvider(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<decimal> GetPriceAsync(string exchange, string ticker)
        {
            var quote = await GetQuoteAsync(exchange, ticker);
            return quote?.Close ?? 0m;
        }

        public async Task<TradingViewQuote?> GetQuoteAsync(string exchange, string ticker)
        {
            var quotes = await GetQuotesAsync(exchange, new[] { ticker });
            return quotes.TryGetValue(ticker.Trim().ToUpperInvariant(), out var quote) ? quote : null;
        }

        public async Task<IDictionary<string, TradingViewQuote?>> GetQuotesAsync(string exchange, IReadOnlyList<string> tickers)
        {
            var client = _httpFactory.CreateClient("tradingview");
            var region = GetTradingViewRegion(exchange);
            var fullTickers = tickers
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => string.IsNullOrEmpty(exchange) ? t.Trim() : $"{exchange}:{t.Trim()}")
                .ToList();

            if (fullTickers.Count == 0)
            {
                return new Dictionary<string, TradingViewQuote?>(StringComparer.OrdinalIgnoreCase);
            }

            var columns = new[]
            {
                "name",
                "description",
                "exchange",
                "close",
                "country",
                "market_cap_basic",
                "sector",
                "industry",
                "earnings_per_share_basic_ttm",
                "price_earnings_ttm",
                "dividends_yield",
                "free_cash_flow_fy",
                "debt_to_equity"
            };

            var payload = new
            {
                symbols = new { tickers = fullTickers },
                columns
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var url = $"https://scanner.tradingview.com/{region}/scan";
            using var resp = await client.PostAsync(url, content);
            resp.EnsureSuccessStatusCode();
            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = JsonDocument.Parse(stream);

            var result = new Dictionary<string, TradingViewQuote?>(StringComparer.OrdinalIgnoreCase);
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            var rows = data.EnumerateArray().ToArray();
            if (rows.Length != fullTickers.Count)
            {
                // If the response row count doesn't match requested tickers, still process what we can.
            }

            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];

                if (!row.TryGetProperty("d", out var d) || d.ValueKind != JsonValueKind.Array)
                {
                    // fallback to requested ticker at the same index if available
                    var fullTickerFallback = i < fullTickers.Count ? fullTickers[i] : string.Empty;
                    var fallbackKey = !string.IsNullOrEmpty(fullTickerFallback)
                        ? (fullTickerFallback.Contains(':') ? fullTickerFallback.Split(':')[1].ToUpperInvariant() : fullTickerFallback.ToUpperInvariant())
                        : $"UNKNOWN_{i}";
                    result[fallbackKey] = null;
                    continue;
                }

                var values = d.EnumerateArray().ToArray();
                // Try to derive the symbol reported by TradingView from the `name` column
                var reportedName = ParseString(values, columns, "name");
                string key;

                if (!string.IsNullOrEmpty(reportedName))
                {
                    key = reportedName.Contains(':') ? reportedName.Split(':')[1].ToUpperInvariant() : reportedName.ToUpperInvariant();
                }
                else
                {
                    // fallback to requested ticker at the same index
                    var fullTickerFallback = i < fullTickers.Count ? fullTickers[i] : string.Empty;
                    key = !string.IsNullOrEmpty(fullTickerFallback)
                        ? (fullTickerFallback.Contains(':') ? fullTickerFallback.Split(':')[1].ToUpperInvariant() : fullTickerFallback.ToUpperInvariant())
                        : $"UNKNOWN_{i}";
                }

                if (values.Length < columns.Length)
                {
                    result[key] = null;
                    continue;
                }

                result[key] = new TradingViewQuote
                {
                    Description = ParseString(values, columns, "description"),
                    Country = ParseString(values, columns, "country"),
                    Close = ParseDecimal(values, columns, "close"),
                    MarketCapBasic = ParseDecimal(values, columns, "market_cap_basic"),
                    Sector = ParseString(values, columns, "sector"),
                    Industry = ParseString(values, columns, "industry"),
                    EarningsPerShareBasicTtm = ParseDecimal(values, columns, "earnings_per_share_basic_ttm"),
                    PriceEarningsTtm = ParseDecimal(values, columns, "price_earnings_ttm"),
                    DividendsYield = ParseDecimal(values, columns, "dividends_yield"),
                    FreeCashFlowFy = ParseDecimal(values, columns, "free_cash_flow_fy"),
                    DebtToEquity = ParseDecimal(values, columns, "debt_to_equity")
                };
            }

            return result;
        }

        private static string? ParseString(JsonElement[] values, string[] columns, string columnName)
        {
            var index = Array.IndexOf(columns, columnName);
            if (index < 0 || index >= values.Length) return null;
            var element = values[index];
            if (element.ValueKind == JsonValueKind.String) return element.GetString();
            return element.ValueKind == JsonValueKind.Number ? element.ToString() : null;
        }

        private static decimal? ParseDecimal(JsonElement[] values, string[] columns, string columnName)
        {
            var index = Array.IndexOf(columns, columnName);
            if (index < 0 || index >= values.Length) return null;
            var element = values[index];
            if (element.ValueKind == JsonValueKind.Null) return null;
            if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var value)) return value;
            if (element.ValueKind == JsonValueKind.String && decimal.TryParse(element.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)) return parsed;
            return null;
        }

        private static string GetTradingViewRegion(string exchange)
        {
            return exchange?.ToUpperInvariant() switch
            {
                "MOEX" => "russia",
                "SPB" => "russia",
                "NASDAQ" => "america",
                "NYSE" => "america",
                "LSE" => "uk",
                "HKEX" => "hongkong",
                _ => "america"
            };
        }
    }
}
