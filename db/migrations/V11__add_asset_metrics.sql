ALTER TABLE asset
    ADD COLUMN peg double precision,
    ADD COLUMN net_income_5y_cagr double precision,
    ADD COLUMN is_broad_index_part boolean NOT NULL DEFAULT false;
