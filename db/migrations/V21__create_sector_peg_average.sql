CREATE TABLE sector_peg_average (
    market varchar(20) NOT NULL,
    language varchar(10) NOT NULL,
    sector varchar(255) NOT NULL,
    average_peg double precision,
    calculated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (market, language, sector),
    CONSTRAINT ck_sector_peg_average_market CHECK (market IN ('US', 'MOEX'))
);

WITH sector_labels AS (
    SELECT DISTINCT
        CASE
            WHEN upper(asset.exchange) IN ('NYSE', 'NASDAQ') THEN 'US'
            WHEN upper(asset.exchange) = 'MOEX' THEN 'MOEX'
        END AS market,
        asset.id AS asset_id,
        asset_label.language,
        asset_label.value AS sector
    FROM asset
    JOIN asset_label ON asset_label.asset_id = asset.id
    WHERE asset.is_broad_index_part = TRUE
      AND asset_label.label = 'sector'
      AND asset_label.value IS NOT NULL
      AND asset_label.value <> ''
)
INSERT INTO sector_peg_average (market, language, sector, average_peg)
SELECT
    sector_labels.market,
    sector_labels.language,
    sector_labels.sector,
    AVG(asset.peg)
FROM sector_labels
JOIN asset ON asset.id = sector_labels.asset_id
WHERE sector_labels.market IS NOT NULL
  AND asset.peg IS NOT NULL
GROUP BY sector_labels.market, sector_labels.language, sector_labels.sector;
