CREATE TABLE broad_index_peg_average (
    market varchar(20) PRIMARY KEY,
    average_peg double precision,
    calculated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_broad_index_peg_average_market CHECK (market IN ('US', 'MOEX'))
);

INSERT INTO broad_index_peg_average (market, average_peg)
SELECT
    markets.market,
    AVG(asset.peg)
FROM (VALUES ('US'), ('MOEX')) AS markets(market)
LEFT JOIN asset
    ON asset.is_broad_index_part = TRUE
   AND asset.peg IS NOT NULL
   AND (
        (markets.market = 'US' AND upper(asset.exchange) IN ('NYSE', 'NASDAQ'))
        OR (markets.market = 'MOEX' AND upper(asset.exchange) = 'MOEX')
   )
GROUP BY markets.market;
