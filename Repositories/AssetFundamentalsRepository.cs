using Dapper;
using Finefolio.ValuationApi.Models;
using Npgsql;

namespace Finefolio.ValuationApi.Repositories;

public class AssetFundamentalsRepository : IAssetFundamentalsRepository
{
    private readonly string _connectionString;

    public AssetFundamentalsRepository(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<AssetDto?> GetAssetDataAsync(string exchange, string ticker, string lang)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT * FROM sp_get_asset_data(@p_exchange, @p_ticker) LIMIT 1;";

        return await conn.QueryFirstOrDefaultAsync<AssetDto>(sql, new { p_exchange = exchange, p_ticker = ticker });
    }

    public async Task<bool> UpdateFundamentalsAsync(int assetId, AssetFundamentalsUpdateDto request)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            UPDATE asset
            SET earnings_per_share_basic_ttm = COALESCE(@earningsPerShareBasicTtm, earnings_per_share_basic_ttm),
                debt = COALESCE(@debt, debt),
                equity = COALESCE(@equity, equity),
                free_cash_flow_fy = COALESCE(@freeCashFlowFy, free_cash_flow_fy),
                net_debt = COALESCE(@netDebt, net_debt),
                dividends_yield = COALESCE(@dividendsYield, dividends_yield),
                interest_expense = COALESCE(@interestExpense, interest_expense)
            WHERE id = @assetId;";

        var affected = await conn.ExecuteAsync(sql, new
        {
            assetId,
            earningsPerShareBasicTtm = request.EarningsPerShareBasicTtm,
            debt = request.Debt,
            equity = request.Equity,
            freeCashFlowFy = request.FreeCashFlowFy,
            netDebt = request.NetDebt,
            dividendsYield = request.DividendsYield,
            interestExpense = request.InterestExpense
        });

        return affected > 0;
    }
}
