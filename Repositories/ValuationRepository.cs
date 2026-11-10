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
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<IReadOnlyList<StockScreenerCandidateResponse>> GetScreenerCandidatesAsync(
        decimal minDividend,
        decimal minEps,
        string? exchange,
        CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT
                a.ticker,
                a.exchange,
                a.close AS price,
                a.earnings_per_share_basic_ttm AS eps
            FROM asset a
            WHERE a.earnings_per_share_basic_ttm > @minEps
              AND a.dividends_yield >= @minDividend
              AND (@exchange IS NULL OR upper(a.exchange) = upper(@exchange))
              AND EXISTS (
                  SELECT 1
                  FROM net_income latest
                  JOIN net_income five_years_ago
                    ON five_years_ago.asset_id = latest.asset_id
                   AND five_years_ago.year = latest.year - 4
                  WHERE latest.asset_id = a.id
                    AND latest.year = (
                        SELECT MAX(history.year)
                        FROM net_income history
                        WHERE history.asset_id = a.id
                    )
                    AND latest.value > five_years_ago.value
              )
            ORDER BY a.earnings_per_share_basic_ttm DESC, a.ticker, a.exchange
            LIMIT 10;";

        var command = new CommandDefinition(
            sql,
            new { minDividend, minEps, exchange },
            cancellationToken: cancellationToken);
        var candidates = await conn.QueryAsync<StockScreenerCandidateResponse>(command);
        return candidates.AsList();
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

    public async Task<double?> GetAverageBroadIndexPegAsync(string exchange)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            SELECT average_peg
            FROM broad_index_peg_average
            WHERE market = CASE
                WHEN upper(@exchange) IN ('NYSE', 'NASDAQ') THEN 'US'
                WHEN upper(@exchange) = 'MOEX' THEN 'MOEX'
                ELSE NULL
            END;";

        return await conn.QueryFirstOrDefaultAsync<double?>(sql, new { exchange });
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

    public async Task<IList<FreeCashFlowHistoryDto>> GetFreeCashFlowHistoryAsync(int assetId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = "SELECT * FROM sp_get_free_cash_flow_history(@p_asset_id);";

        var result = await conn.QueryAsync<FreeCashFlowHistoryDto>(
            sql,
            new { p_asset_id = assetId });

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
        long? totalSharesOutstandingFundamental,
        decimal? earningsPerShareBasicTtm,
        decimal? beta,
        decimal? priceEarningsTtm,
        decimal? dividendsYield,
        decimal? freeCashFlowFy,
        decimal? debtToEquity,
        decimal? interestRateOnDebt,
        decimal? netDebt,
        decimal? equity,
        decimal? debt)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            UPDATE asset
            SET description = COALESCE(@description, description),
                country = COALESCE(@country, country),
                close = COALESCE(@close, close),
                close_last_updated = COALESCE(@closeLastUpdated, close_last_updated),
                market_cap_basic = COALESCE(@marketCapBasic, market_cap_basic),
                issue = COALESCE(@totalSharesOutstandingFundamental, issue),
                earnings_per_share_basic_ttm = COALESCE(@earningsPerShareBasicTtm, earnings_per_share_basic_ttm),
                beta = COALESCE(@beta, beta),
                price_earnings_ttm = COALESCE(@priceEarningsTtm, price_earnings_ttm),
                dividends_yield = COALESCE(@dividendsYield, dividends_yield),
                free_cash_flow_fy = COALESCE(@freeCashFlowFy, free_cash_flow_fy),
                debt_to_equity = COALESCE(@debtToEquity, debt_to_equity),
                interest_rate_on_debt = COALESCE(@interestRateOnDebt, interest_rate_on_debt),
                net_debt = COALESCE(@netDebt, net_debt),
                equity = COALESCE(@equity, equity),
                debt = COALESCE(@debt, debt)
            WHERE id = @assetId;";

        await conn.ExecuteAsync(sql, new
        {
            assetId,
            description,
            country,
            close,
            closeLastUpdated,
            marketCapBasic,
            totalSharesOutstandingFundamental,
            earningsPerShareBasicTtm,
            beta,
            priceEarningsTtm,
            dividendsYield,
            freeCashFlowFy,
            debtToEquity,
            interestRateOnDebt,
            netDebt,
            equity,
            debt
        });
    }

    public async Task RecalculateBroadIndexMetricsAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        const string sql = @"
            WITH broad_assets AS (
                SELECT id
                FROM asset
                WHERE is_broad_index_part = TRUE
            ),
            latest_net_income AS (
                SELECT DISTINCT ON (history.asset_id)
                    history.asset_id,
                    history.year,
                    history.value
                FROM net_income history
                JOIN broad_assets ON broad_assets.id = history.asset_id
                ORDER BY history.asset_id, history.year DESC, history.id DESC
            ),
            income_endpoints AS (
                SELECT
                    broad_assets.id,
                    asset.price_earnings_ttm,
                    CASE
                        WHEN five_years_ago.value = 0 THEN 0.1::double precision
                        ELSE five_years_ago.value
                    END AS beginning_value,
                    latest.value AS ending_value
                FROM broad_assets
                JOIN asset ON asset.id = broad_assets.id
                LEFT JOIN latest_net_income latest ON latest.asset_id = broad_assets.id
                LEFT JOIN LATERAL (
                    SELECT history.value
                    FROM net_income history
                    WHERE history.asset_id = latest.asset_id
                      AND history.year = latest.year - 5
                    ORDER BY history.id DESC
                    LIMIT 1
                ) five_years_ago ON TRUE
            ),
            cagr_ratios AS (
                SELECT
                    id,
                    price_earnings_ttm,
                    beginning_value,
                    ending_value,
                    (ending_value - beginning_value + abs(beginning_value)) / abs(beginning_value) AS ratio
                FROM income_endpoints
            ),
            calculated_cagr AS (
                SELECT
                    id,
                    price_earnings_ttm,
                    CASE
                        WHEN beginning_value IS NULL OR ending_value IS NULL THEN NULL
                        WHEN ending_value < 0 AND beginning_value < 0 THEN
                            CASE
                                WHEN abs(ending_value) < abs(beginning_value) THEN
                                    (power(abs(ending_value / beginning_value), 1.0::double precision / 5.0) - 1) * -1
                                ELSE power(abs(ending_value / beginning_value), 1.0::double precision / 5.0) - 1
                            END
                        WHEN ratio < 0 THEN (power(abs(ratio), 1.0::double precision / 5.0) - 1) * -1
                        ELSE power(ratio, 1.0::double precision / 5.0) - 1
                    END AS cagr
                FROM cagr_ratios
            ),
            calculated_metrics AS (
                SELECT
                    id,
                    cagr,
                    CASE
                        WHEN cagr IS NULL THEN NULL
                        WHEN LEAST(GREATEST(cagr * 100, 0), 25) = 0 THEN 0
                        WHEN price_earnings_ttm IS NULL THEN NULL
                        WHEN round(
                            (price_earnings_ttm / LEAST(GREATEST(cagr * 100, 0), 25))::numeric,
                            2
                        ) < 0.01 THEN 0
                        ELSE round(
                            (price_earnings_ttm / LEAST(GREATEST(cagr * 100, 0), 25))::numeric,
                            2
                        )::double precision
                    END AS peg
                FROM calculated_cagr
            )
            UPDATE asset
            SET net_income_5y_cagr = calculated_metrics.cagr,
                peg = calculated_metrics.peg
            FROM calculated_metrics
            WHERE asset.id = calculated_metrics.id;";

        await using var transaction = await conn.BeginTransactionAsync(cancellationToken);
        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            transaction: transaction,
            cancellationToken: cancellationToken));

        const string averageSql = @"
            INSERT INTO broad_index_peg_average (market, average_peg, calculated_at)
            SELECT
                markets.market,
                AVG(asset.peg),
                now()
            FROM (VALUES ('US'), ('MOEX')) AS markets(market)
            LEFT JOIN asset
                ON asset.is_broad_index_part = TRUE
               AND asset.peg IS NOT NULL
               AND (
                    (markets.market = 'US' AND upper(asset.exchange) IN ('NYSE', 'NASDAQ'))
                    OR (markets.market = 'MOEX' AND upper(asset.exchange) = 'MOEX')
               )
            GROUP BY markets.market
            ON CONFLICT (market)
            DO UPDATE SET
                average_peg = EXCLUDED.average_peg,
                calculated_at = EXCLUDED.calculated_at;";

        await conn.ExecuteAsync(new CommandDefinition(
            averageSql,
            transaction: transaction,
            cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
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

    public async Task UpsertFreeCashFlowAsync(int assetId, int year, double value)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = @"
            INSERT INTO free_cash_flow (asset_id, year, value)
            VALUES (@assetId, @year, @value)
            ON CONFLICT (asset_id, year)
            DO UPDATE SET value = EXCLUDED.value;";

        await conn.ExecuteAsync(sql, new { assetId, year, value });
    }

}
