namespace Finefolio.ValuationApi.Models;

public class ValuationResultDto
{
    public StockInfoDto StockInfo { get; set; } = new();
    public ValuationDto? Valuation { get; set; }
    public bool? IsPreviousDayData { get; set; }
}