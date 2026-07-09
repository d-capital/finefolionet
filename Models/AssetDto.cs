namespace Finefolio.ValuationApi.Models;

public class AssetDto
{
    public int Id { get; set; }
    public string? Exchange { get; set; }
    public string? Ticker { get; set; }
    public string? Description { get; set; }
    public string? Country { get; set; }
    public decimal? Close { get; set; }
    public DateTime? CloseLastUpdated { get; set; }
    public decimal? InterestExpense { get; set; }
    public long? Issue { get; set; }
    public decimal? MarketCapBasic { get; set; }
    public decimal? EarningsPerShareBasicTtm { get; set; }
    public decimal? PriceEarningsTtm { get; set; }
    public decimal? DividendsYield { get; set; }
    public decimal? FreeCashFlowFy { get; set; }
    public decimal? Equity { get; set; }
    public decimal? Debt { get; set; }
    public decimal? NetDebt { get; set; }
    public decimal? DebtToEquity { get; set; }
    public decimal? InterestRateOnDebt { get; set; }
}
