namespace Finefolio.ValuationApi.Models;

public class DcfResultDto
{
    public double? Beta { get; set; }
    public double? RiskFreeRate { get; set; }
    public double? MarketRate { get; set; }
    public double? Capm { get; set; }
    public double? Equity { get; set; }
    public double? Debt { get; set; }
    public double? TaxRate { get; set; }
    public double? InterestRateOnDebt { get; set; }
    public double? AverageGrowthRate { get; set; }
    public double? TerminalGrowthRate { get; set; }
    public double? EnterpriseValue { get; set; }
    public double? NetDebt { get; set; }
    public double? EquityValue { get; set; }
    public double? SharesOutstanding { get; set; }
    public double? FairValue { get; set; }
    public double? MarketValueOfDebt { get; set; }
    public double? Wacc { get; set; }
    public double? TerminalValue { get; set; }
    public double? PresentValueOfTerminalValue { get; set; }
    public List<DcfProjectionDto> Projections { get; set; } = new();
}