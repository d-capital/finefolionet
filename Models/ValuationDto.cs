namespace Finefolio.ValuationApi.Models;

public class ValuationDto
{
    public double? FairPrice { get; set; }
    public double? ResultPercent { get; set; }
    public string? ResultLabel { get; set; }
    public string? Formula { get; set; }
    public string? Explanation { get; set; }
    public List<NetProfitHistoryDto>? NetProfitHistory { get; set; }
    public AverageGrowthDto? AvgGrowth { get; set; }
    public double? Peg { get; set; }
}