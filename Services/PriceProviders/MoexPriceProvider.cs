using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Finefolio.ValuationApi.Services.PriceProviders;
using System.Globalization;

namespace Finefolio.ValuationApi.Services.PriceProviders
{
    public class MoexPriceProvider : IMoexPriceProvider
    {
        private readonly IHttpClientFactory _httpFactory;
        private const string BaseUrl = "https://iss.moex.com";

        public MoexPriceProvider(IHttpClientFactory httpFactory)
        {
            _httpFactory = httpFactory;
        }

        public async Task<decimal> GetPriceAsync(string exchange, string ticker)
        {
            var client = _httpFactory.CreateClient("moex");
            const string board = "TQBR";

            // First try the 15-minute delayed marketdata endpoint for the latest available quote.
            var marketDataUrl = $"{BaseUrl}/iss/engines/stock/markets/shares/securities/{Uri.EscapeDataString(ticker)}.json?iss.only=marketdata&iss.meta=off";
            using var marketResp = await client.GetAsync(marketDataUrl);
            marketResp.EnsureSuccessStatusCode();

            using var marketStream = await marketResp.Content.ReadAsStreamAsync();
            using var marketDoc = JsonDocument.Parse(marketStream);

            if (marketDoc.RootElement.TryGetProperty("marketdata", out var md)
                && md.TryGetProperty("data", out var dataEl)
                && dataEl.GetArrayLength() > 0)
            {
                var lastRow = dataEl[dataEl.GetArrayLength() - 1];
                const int idxPrice = 12;

                if (lastRow.ValueKind == JsonValueKind.Array && lastRow.GetArrayLength() > idxPrice)
                {
                    var priceEl = lastRow[idxPrice];
                    if (priceEl.ValueKind != JsonValueKind.Null
                        && decimal.TryParse(priceEl.ToString(), NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out var price))
                    {
                        return price;
                    }
                }
            }

            // Fallback to the last seven days of daily candles so we handle weekends and non-trading days.
            var today = DateTime.UtcNow.Date;
            var fromDate = today.AddDays(-7);
            var candlesUrl = $"{BaseUrl}/iss/engines/stock/markets/shares/securities/{Uri.EscapeDataString(ticker)}/candles.json?interval=24&from={fromDate:yyyy-MM-dd}&till={today:yyyy-MM-dd}&iss.meta=off";

            using var candlesResp = await client.GetAsync(candlesUrl);
            candlesResp.EnsureSuccessStatusCode();

            using var candlesStream = await candlesResp.Content.ReadAsStreamAsync();
            using var candlesDoc = JsonDocument.Parse(candlesStream);

            if (candlesDoc.RootElement.TryGetProperty("candles", out var candles)
                && candles.TryGetProperty("columns", out var colsEl2)
                && candles.TryGetProperty("data", out var dataEl2))
            {
                var cols = colsEl2.EnumerateArray().Select(c => c.GetString()).ToList();
                int idxClose = cols.IndexOf("close");
                int idxBegin = cols.IndexOf("begin");

                DateTime? latestDate = null;
                decimal latestClose = 0m;

                foreach (var row in dataEl2.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Array) continue;
                    if (idxBegin < 0 || row[idxBegin].ValueKind == JsonValueKind.Null) continue;
                    if (!TryParseDate(row[idxBegin].GetString(), out var rowDate)) continue;
                    if (idxClose < 0 || row[idxClose].ValueKind == JsonValueKind.Null) continue;
                    if (!decimal.TryParse(row[idxClose].ToString(), out var closePrice)) continue;

                    if (!latestDate.HasValue || rowDate > latestDate.Value)
                    {
                        latestDate = rowDate;
                        latestClose = closePrice;
                    }
                }

                if (latestDate.HasValue) return latestClose;
            }

            // Fallback to history endpoint: choose the most recent TRADEDATE close for the requested board.
            var historyUrl = $"{BaseUrl}/iss/history/engines/stock/markets/shares/securities/{Uri.EscapeDataString(ticker)}.json?iss.meta=off";
            using var histResp = await client.GetAsync(historyUrl);
            if (histResp.IsSuccessStatusCode)
            {
                using var hStream = await histResp.Content.ReadAsStreamAsync();
                using var hDoc = JsonDocument.Parse(hStream);
                if (hDoc.RootElement.TryGetProperty("history", out var hist)
                    && hist.TryGetProperty("columns", out var hCols)
                    && hist.TryGetProperty("data", out var hData))
                {
                    var hcols = hCols.EnumerateArray().Select(c => c.GetString()).ToList();
                    int idxCloseH = hcols.IndexOf("CLOSE");
                    int idxBoardH = hcols.IndexOf("BOARDID");
                    int idxDateH = hcols.IndexOf("TRADEDATE");

                    DateTime? latestDate = null;
                    decimal latestClose = 0m;

                    foreach (var row in hData.EnumerateArray())
                    {
                        if (idxBoardH >= 0 && row[idxBoardH].GetString() != board) continue;
                        if (idxDateH < 0 || idxCloseH < 0) continue;
                        if (row[idxDateH].ValueKind == JsonValueKind.Null) continue;
                        if (!TryParseDate(row[idxDateH].GetString(), out var date)) continue;
                        if (row[idxCloseH].ValueKind == JsonValueKind.Null) continue;
                        if (!decimal.TryParse(row[idxCloseH].ToString(), out var close)) continue;

                        if (!latestDate.HasValue || date > latestDate.Value)
                        {
                            latestDate = date;
                            latestClose = close;
                        }
                    }

                    if (latestDate.HasValue) return latestClose;
                }
            }

            throw new InvalidOperationException("No price found for ticker on MOEX");
        }
        private static bool TryParseDate(string? dateText, out DateTime result)
        {
            if (string.IsNullOrWhiteSpace(dateText))
            {
                result = default;
                return false;
            }

            return DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result);
        }
    }
}
