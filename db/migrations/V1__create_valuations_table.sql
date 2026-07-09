-- Create asset table
CREATE TABLE IF NOT EXISTS asset (
    id serial PRIMARY KEY,
    ticker varchar(50),
    exchange varchar(20),
    description varchar(255),
    country varchar(255),
    close double precision,
    close_last_updated timestamptz default now(),
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
);

-- Create net_income table
CREATE TABLE IF NOT EXISTS net_income (
    id serial PRIMARY KEY,
    asset_id int REFERENCES asset(id) ON DELETE CASCADE, 
    year int, 
    value double precision
);

-- Create asset_label table
CREATE TABLE IF NOT EXISTS asset_label (
    id serial PRIMARY KEY,
    asset_id int REFERENCES asset(id) ON DELETE CASCADE, 
    label varchar(255),
    language varchar(10),
    value varchar(255)
);
