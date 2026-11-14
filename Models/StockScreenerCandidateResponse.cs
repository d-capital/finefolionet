namespace Finefolio.ValuationApi.Models;

/// <summary>Represents a stock that matches the valuation screener criteria.</summary>
public class StockScreenerCandidateResponse
{
    public string? Name { get; set; }
    public string? Ticker { get; set; }
    public string? Exchange { get; set; }
    public decimal? Price { get; set; }
    public decimal? Eps { get; set; }
    public double? NetIncome5yCagr { get; set; }
}
