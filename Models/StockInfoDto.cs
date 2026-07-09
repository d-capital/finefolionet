namespace Finefolio.ValuationApi.Models;
public class StockInfoDto
{
    public string? Name { get; set; }
    public string? Ticker { get; set; }
    public string? Exchange { get; set; }
    public decimal? Price { get; set; }
    public string? Country { get; set; }
    public decimal? Capitalization { get; set; }
    public string? Sector { get; set; }
    public string? Industry { get; set; }
    public decimal? EpsTtm { get; set; }
    public decimal? PeTtm { get; set; }
    public decimal? DividendYield { get; set; }
    public decimal? FreeCashFlow { get; set; }
    public decimal? DebtToEquity { get; set; }
}
