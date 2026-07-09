namespace Finefolio.ValuationApi.Models;

public class TradingViewQuote
{
    public string? Description { get; set; }
    public string? Country { get; set; }
    public decimal? Close { get; set; }
    public decimal? MarketCapBasic { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public decimal? EarningsPerShareBasicTtm { get; set; }
    public decimal? PriceEarningsTtm { get; set; }
    public decimal? DividendsYield { get; set; }
    public decimal? FreeCashFlowFy { get; set; }
    public decimal? DebtToEquity { get; set; }
}
