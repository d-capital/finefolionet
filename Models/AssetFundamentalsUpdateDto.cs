namespace Finefolio.ValuationApi.Models;

public class AssetFundamentalsUpdateDto
{
    public decimal? EarningsPerShareBasicTtm { get; set; }
    public decimal? Debt { get; set; }
    public decimal? Equity { get; set; }
    public decimal? FreeCashFlowFy { get; set; }
    public decimal? NetDebt { get; set; }
    public decimal? DividendsYield { get; set; }
    public decimal? InterestExpense { get; set; }

    public bool HasAnyValue =>
        EarningsPerShareBasicTtm.HasValue ||
        Debt.HasValue ||
        Equity.HasValue ||
        FreeCashFlowFy.HasValue ||
        NetDebt.HasValue ||
        DividendsYield.HasValue ||
        InterestExpense.HasValue;
}
