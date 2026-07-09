-- Drop the function first to allow signature updates in repeatable migrations
DROP FUNCTION IF EXISTS sp_get_asset_data(varchar, varchar);

-- Stored procedure to get valuation by market and ticker
CREATE OR REPLACE FUNCTION sp_get_asset_data(p_exchange varchar, p_ticker varchar)
RETURNS TABLE(
    id int ,
    ticker varchar(50),
    exchange varchar(20),
    description varchar(255),
    country varchar(255),
    close double precision,
    close_last_updated timestamptz,
    interest_expense double precision,
    issue bigint,
    market_cap_basic double precision,
    earnings_per_share_basic_ttm double precision,
    price_earnings_ttm double precision,
    dividends_yield double precision,
    free_cash_flow_fy double precision,
    equity double precision,
    debt double precision,
    net_debt double precision,
    debt_to_equity double precision,
    interest_rate_on_debt double precision
)
AS $$
    SELECT 
    id,
    ticker,
    exchange,
    description,
    country,
    close,
    close_last_updated,
    interest_expense,
    issue,
    market_cap_basic,
    earnings_per_share_basic_ttm,
    price_earnings_ttm,
    dividends_yield,
    free_cash_flow_fy,
    equity,
    debt,
    net_debt,
    debt_to_equity,
    interest_rate_on_debt
    FROM asset WHERE exchange = p_exchange AND ticker = p_ticker;
$$ LANGUAGE sql STABLE;
