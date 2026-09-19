namespace Finefolio.ValuationApi.Models;

public class DcfDto
{
    public decimal? EnterpriseValue { get; set; }
    public decimal? EquityValue { get; set; }
    public decimal? FairValue { get; set; }
    public decimal? TerminalValue { get; set; }
    public decimal? PresentValueOfTerminalValue { get; set; }
    public List<DcfProjectionDto> Projections { get; set; } = new();
}

public class DcfProjectionDto
{
    public int Year { get; set; }
    public decimal ProjectedFcf { get; set; }
    public decimal PresentValue { get; set; }
}