using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Repositories;

namespace Finefolio.ValuationApi.Services;

public class ValuationService : IValuationService
{
    private readonly IValuationRepository _repository;
    private readonly PriceService _priceService;

    public ValuationService(IValuationRepository repository, PriceService priceService)
    {
        _repository = repository;
        _priceService = priceService;
    }

    public async Task<ValuationResultDto?> GetValuationAsync(string exchange, string ticker, string lang)
    {
        var assetDto = await _repository.GetAssetDataAsync(exchange, ticker, lang);

        var netProfitHistory = null as IEnumerable<NetProfitHistoryDto>;
        if (assetDto != null)
        {
            netProfitHistory = await _repository.GetNetIncomeHistoryAsync(assetDto.Id);
        }

        var assetLabels = null as IEnumerable<AssetLabelDto>;
        if (assetDto != null)
        {
            assetLabels = await _repository.GetAssetLabelsAsync(assetDto.Id);
        }

        var sectorLabel = assetLabels?.Where(l => l?.Language == lang && l?.Label == "sector").FirstOrDefault()?.Value;
        var industryLabel = assetLabels?.Where(l => l?.Language == lang && l?.Label == "industry").FirstOrDefault()?.Value;

        if (exchange != "MOEX" && lang == "ru")
        {
            sectorLabel = assetLabels?.Where(l => l?.Language == "en" && l?.Label == "sector").FirstOrDefault()?.Value;
            industryLabel = assetLabels?.Where(l => l?.Language == "en" && l?.Label == "industry").FirstOrDefault()?.Value;
        }

        var price = 0m;
        if (!string.IsNullOrEmpty(exchange) && !string.IsNullOrEmpty(ticker))
        {
            try
            {
                var priceResult = await _priceService.GetPriceAsync(exchange, ticker);
                price = priceResult.Price;
            }
            catch
            {
                price = assetDto?.Close ?? 0m;
            }
        }

        var country = assetDto?.Country ?? string.Empty;
        var capitalization = assetDto?.MarketCapBasic ?? 0;
        var peTtm = assetDto?.PriceEarningsTtm ?? 0;
        var debtToEquity = assetDto?.DebtToEquity ?? 0;
        if (exchange == "MOEX")
        {
            country = "Russia";
            capitalization = assetDto?.Issue * assetDto?.Close ?? 0;
            peTtm = assetDto?.Close / assetDto?.EarningsPerShareBasicTtm ?? 0;
            debtToEquity = assetDto?.Equity != 0 ? assetDto?.Debt / assetDto?.Equity ?? 0 : 0;
        }

        var effectivePrice = price > 0 ? price : assetDto?.Close ?? 0m;

        var stockInfo = new StockInfoDto
        {
            Name = assetDto?.Description ?? string.Empty,
            Ticker = assetDto?.Ticker ?? string.Empty,
            Exchange = assetDto?.Exchange ?? string.Empty,
            Price = effectivePrice,
            Country = country,
            Capitalization = capitalization,
            Sector = sectorLabel ?? string.Empty,
            Industry = industryLabel ?? string.Empty,
            EpsTtm = assetDto?.EarningsPerShareBasicTtm ?? 0,
            PeTtm = peTtm,
            DividendYield = assetDto?.DividendsYield ?? 0,
            FreeCashFlow = assetDto?.FreeCashFlowFy ?? 0,
            DebtToEquity = debtToEquity
        };

        AverageGrowthDto? averageGrowth = null;
        if (netProfitHistory != null)
        {
            averageGrowth = CalculateAverageGrowth(netProfitHistory.ToList());
        }

        var valuation = FillValuation(
            (double?)stockInfo.EpsTtm,
            averageGrowth,
            netProfitHistory?.ToList(),
            (double)stockInfo.Price,
            (double?)stockInfo.PeTtm);

        var isPreviousDayData = stockInfo.Price == 0;

        return new ValuationResultDto
        {
            StockInfo = stockInfo,
            Valuation = valuation,
            IsPreviousDayData = isPreviousDayData
        };
    }
    public static double CalculateCagr(double beginningValue, double endingValue, double numberOfYears)
    {
        if (numberOfYears <= 0)
        {
            throw new ArgumentException("Number of years must be greater than zero.", nameof(numberOfYears));
        }

        if (beginningValue == 0)
        {
            beginningValue = 0.1;
        }

        double cagr;

        if (endingValue < 0 && beginningValue < 0)
        {
            /*
             * Calculates CAGR for negative start and end value:
             * ((|Ending / Beginning|)^(1/n) - 1) * -1
             */
            double ratio = Math.Abs(endingValue / beginningValue);

            if (Math.Abs(endingValue) < Math.Abs(beginningValue))
            {
                cagr = (Math.Pow(ratio, 1.0 / numberOfYears) - 1) * -1;
            }
            else
            {
                cagr = Math.Pow(ratio, 1.0 / numberOfYears) - 1;
            }
        }
        else
        {
            /*
             * Calculates CAGR based on formula:
             * ((Ending - Beginning + |Beginning|) / |Beginning|)^(1/n) - 1
             */
            double absBeg = Math.Abs(beginningValue);
            double numerator = endingValue - beginningValue + absBeg;
            double ratio = numerator / absBeg;

            if (ratio < 0)
            {
                cagr = (Math.Pow(Math.Abs(ratio), 1.0 / numberOfYears) - 1) * -1;
            }
            else
            {
                cagr = Math.Pow(ratio, 1.0 / numberOfYears) - 1;
            }
        }

        return cagr;
    }

    public static AverageGrowthDto CalculateAverageGrowth(List<NetProfitHistoryDto> history)
    {
        // Ensure chronological order
        history = history.OrderBy(x => x.Year).ToList();

        if (history.Count < 2)
        {
            return new AverageGrowthDto
            {
                Ttm = null,
                ThreeYears = null,
                FiveYears = null
            };
        }

        // Compute YoY growths (currently unused, but kept to match the Python implementation)
        var yoyGrowths = new List<double>();

        for (int i = 1; i < history.Count; i++)
        {
            double prev = history[i - 1].Value ?? 0;
            double curr = history[i].Value ?? 0;

            if (prev != 0)
            {
                yoyGrowths.Add(CalculateCagr(prev, curr, 2));
            }
        }

        // Last year's CAGR (TTM)
        double? ttmGrowth = history.Count >= 2
            ? CalculateCagr(
                history[^2].Value ?? 0,
                history[^1].Value ?? 0,
                2)
            : null;

        // Three-year CAGR
        double? threeYearsGrowth = history.Count >= 3
            ? CalculateCagr(
                history[^3].Value ?? 0,
                history[^1].Value ?? 0,
                3)
            : null;

        // Five-year CAGR
        double? fiveYearsGrowth = history.Count >= 5
            ? CalculateCagr(
                history[^5].Value ?? 0,
                history[^1].Value ?? 0,
                5)
            : null;

        return new AverageGrowthDto
        {
            Ttm = ttmGrowth.HasValue
                ? Math.Round(ttmGrowth.Value * 100, 2)
                : null,

            ThreeYears = threeYearsGrowth.HasValue
                ? Math.Round(threeYearsGrowth.Value * 100, 2)
                : null,

            FiveYears = fiveYearsGrowth.HasValue
                ? Math.Round(fiveYearsGrowth.Value * 100, 2)
                : null
        };
    }
    public static ValuationDto FillValuation(
        double? epsTtm,
        AverageGrowthDto? averageGrowth,
        List<NetProfitHistoryDto>? netProfitHistory,
        double price,
        double? peTtm)
    {
        double? calcEpsTtm = epsTtm is < 0 ? 0 : epsTtm;
        double? calcAvgGrowthRate = averageGrowth?.FiveYears is double growth
            ? Math.Min(Math.Max(growth, 0), 25)
            : null;

        double? fairPrice = null;
        if (calcAvgGrowthRate.HasValue && calcEpsTtm.HasValue)
        {
            fairPrice = Math.Round(calcAvgGrowthRate.Value * calcEpsTtm.Value, 2);
        }

        double? peg = null;
        if (calcAvgGrowthRate.HasValue && peTtm.HasValue && calcAvgGrowthRate.Value > 0)
        {
            peg = Math.Round(peTtm.Value / calcAvgGrowthRate.Value, 2);
            if (peg < 0.01)
            {
                peg = 0;
            }
        }
        else if (calcAvgGrowthRate.HasValue && calcAvgGrowthRate.Value == 0)
        {
            peg = 0;
        }

        var explanationText = BuildExplanationText(calcAvgGrowthRate, calcEpsTtm, fairPrice, peTtm);
        var resultPercent = CalculateResultPercent(price, fairPrice);
        var resultLabel = resultPercent > 0 ? "Undervalued" : "Overvalued";

        return new ValuationDto
        {
            FairPrice = fairPrice,
            ResultPercent = resultPercent,
            ResultLabel = resultLabel,
            Formula = explanationText,
            Explanation = string.Empty,
            NetProfitHistory = netProfitHistory,
            AvgGrowth = averageGrowth,
            Peg = peg,
        };
    }

    private static string BuildExplanationText(double? growthRate, double? eps, double? fairPrice, double? peTtm)
    {
        if (growthRate.HasValue && eps.HasValue && fairPrice.HasValue && peTtm.HasValue)
        {
            return $"{Math.Round(growthRate.Value, 2)} x {Math.Round(eps.Value, 2)} = {Math.Round(fairPrice.Value, 2)}";
        }

        if (growthRate.HasValue && !eps.HasValue)
        {
            return $"{Math.Round(growthRate.Value, 2)} x N/A = N/A";
        }

        if (!growthRate.HasValue && eps.HasValue)
        {
            return $"N/A x {Math.Round(eps.Value, 2)} = N/A";
        }

        return "N/A x N/A = N/A";
    }

    private static double CalculateResultPercent(double price, double? fairPrice)
    {
        if (price <= 0 || !fairPrice.HasValue)
        {
            return 0;
        }

        return Math.Round(((fairPrice.Value - price) / price) * 100, 2);
    }

    public async Task<bool> AddOrUpdateNetIncomeAsync(string exchange, string ticker, int year, double value)
    {
        var assetDto = await _repository.GetAssetDataAsync(exchange, ticker, "en");
        if (assetDto == null) return false;

        await _repository.UpsertNetIncomeAsync(assetDto.Id, year, value);
        return true;
    }
}
