namespace Finefolio.ValuationApi.Models;

/// <summary>Represents a stock that matches the valuation screener criteria.</summary>
public class StockScreenerCandidateResponse
{
    public string? Ticker { get; set; }
    public string? Exchange { get; set; }
    public decimal? Price { get; set; }
    public decimal? Pe { get; set; }
    public decimal? Eps { get; set; }
}
