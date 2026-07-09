using Dapper;
using Npgsql;
using Finefolio.ValuationApi.Models;
using System.Linq;

namespace Finefolio.ValuationApi.Repositories;

public class ValuationRepository : IValuationRepository
{
    private readonly string _connectionString;

    public ValuationRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? Environment.GetEnvironmentVariable("CONNECTION_STRING")
                            ?? "Host=localhost;Port=5432;Database=valuation;Username=postgres;Password=postgres";
    }

    public async Task<AssetDto?> GetAssetDataAsync(string exchange, string ticker, string lang) 
    { 
        await using var conn = new NpgsqlConnection(_connectionString); 
        
        // Select directly from the function table result
        const string sql = "SELECT * FROM sp_get_asset_data(@p_exchange, @p_ticker) LIMIT 1;"; 
        
        var asset = await conn.QueryFirstOrDefaultAsync<AssetDto>(
            sql, 
            new { p_exchange = exchange, p_ticker = ticker }
            // CommandType.Text is used implicitly here, DO NOT add CommandType.StoredProcedure
        ); 
        return asset; 
    }

    public async Task<IList<NetProfitHistoryDto>> GetNetIncomeHistoryAsync(int assetId) 
    { 
        await using var conn = new NpgsqlConnection(_connectionString); 
        
        // Select directly from the function table result
        const string sql = "SELECT * FROM sp_get_net_income_history(@p_asset_id);"; 
        
        var result = await conn.QueryAsync<NetProfitHistoryDto>(
            sql,
            new { p_asset_id = assetId }
            // CommandType.Text is used implicitly here, DO NOT add CommandType.StoredProcedure
        );

        return result.ToList();
    }

    public async Task<IList<AssetLabelDto>> GetAssetLabelsAsync(int assetId) 
    { 
        await using var conn = new NpgsqlConnection(_connectionString); 
        
        // Select directly from the function table result
        const string sql = "SELECT * FROM sp_get_asset_labels(@p_asset_id);"; 
        
        var result = await conn.QueryAsync<AssetLabelDto>(
            sql,
            new { p_asset_id = assetId }
            // CommandType.Text is used implicitly here, DO NOT add CommandType.StoredProcedure
        );

        return result.ToList();
    }

    public async Task<IList<AssetDto>> GetAssetsByExchangeAsync(string exchange)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT * FROM asset WHERE exchange = @exchange;";
        var result = await conn.QueryAsync<AssetDto>(sql, new { exchange });
        return result.ToList();
    }

    public async Task UpdateAssetPriceAsync(int assetId, decimal close, DateTime closeLastUpdated)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            UPDATE asset
            SET close = @close,
                close_last_updated = @closeLastUpdated
            WHERE id = @assetId;";

        await conn.ExecuteAsync(sql, new { assetId, close, closeLastUpdated });
    }

    public async Task UpdateAssetMetadataAsync(
        int assetId,
        string? description,
        string? country,
        decimal? close,
        DateTime? closeLastUpdated,
        decimal? marketCapBasic,
        decimal? earningsPerShareBasicTtm,
        decimal? priceEarningsTtm,
        decimal? dividendsYield,
        decimal? freeCashFlowFy,
        decimal? debtToEquity)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            UPDATE asset
            SET description = COALESCE(@description, description),
                country = COALESCE(@country, country),
                close = COALESCE(@close, close),
                close_last_updated = COALESCE(@closeLastUpdated, close_last_updated),
                market_cap_basic = COALESCE(@marketCapBasic, market_cap_basic),
                earnings_per_share_basic_ttm = COALESCE(@earningsPerShareBasicTtm, earnings_per_share_basic_ttm),
                price_earnings_ttm = COALESCE(@priceEarningsTtm, price_earnings_ttm),
                dividends_yield = COALESCE(@dividendsYield, dividends_yield),
                free_cash_flow_fy = COALESCE(@freeCashFlowFy, free_cash_flow_fy),
                debt_to_equity = COALESCE(@debtToEquity, debt_to_equity)
            WHERE id = @assetId;";

        await conn.ExecuteAsync(sql, new
        {
            assetId,
            description,
            country,
            close,
            closeLastUpdated,
            marketCapBasic,
            earningsPerShareBasicTtm,
            priceEarningsTtm,
            dividendsYield,
            freeCashFlowFy,
            debtToEquity
        });
    }

    public async Task UpsertNetIncomeAsync(int assetId, int year, double value)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            WITH upsert AS (
                UPDATE net_income
                SET value = @value
                WHERE asset_id = @assetId AND year = @year
                RETURNING *
            )
            INSERT INTO net_income (asset_id, year, value)
            SELECT @assetId, @year, @value
            WHERE NOT EXISTS (SELECT 1 FROM upsert);
        ";

        await conn.ExecuteAsync(sql, new { assetId, year, value });
    }

}
