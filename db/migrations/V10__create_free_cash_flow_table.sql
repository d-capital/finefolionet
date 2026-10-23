CREATE TABLE IF NOT EXISTS free_cash_flow (
    id serial PRIMARY KEY,
    asset_id int NOT NULL REFERENCES asset(id) ON DELETE CASCADE,
    year int NOT NULL,
    value double precision NOT NULL,
    UNIQUE (asset_id, year)
);
