WITH latest_net_income AS (
    SELECT DISTINCT ON (asset_id)
        asset_id,
        year,
        value
    FROM net_income
    ORDER BY asset_id, year DESC, id DESC
),
net_income_endpoints AS (
    SELECT
        latest.asset_id,
        CASE
            WHEN five_years_ago.value = 0 THEN 0.1::double precision
            ELSE five_years_ago.value
        END AS beginning_value,
        latest.value AS ending_value
    FROM latest_net_income latest
    JOIN LATERAL (
        SELECT value
        FROM net_income
        WHERE asset_id = latest.asset_id
          AND year = latest.year - 5
        ORDER BY id DESC
        LIMIT 1
    ) five_years_ago ON TRUE
),
net_income_cagr AS (
    SELECT
        asset_id,
        CASE
            WHEN ending_value < 0 AND beginning_value < 0 THEN
                CASE
                    WHEN abs(ending_value) < abs(beginning_value) THEN
                        (power(abs(ending_value / beginning_value), 1.0::double precision / 5.0) - 1) * -1
                    ELSE power(abs(ending_value / beginning_value), 1.0::double precision / 5.0) - 1
                END
            ELSE
                CASE
                    WHEN ratio < 0 THEN (power(abs(ratio), 1.0::double precision / 5.0) - 1) * -1
                    ELSE power(ratio, 1.0::double precision / 5.0) - 1
                END
        END AS cagr
    FROM (
        SELECT
            asset_id,
            beginning_value,
            ending_value,
            (ending_value - beginning_value + abs(beginning_value)) / abs(beginning_value) AS ratio
        FROM net_income_endpoints
    ) endpoints
)
UPDATE asset
SET net_income_5y_cagr = net_income_cagr.cagr
FROM net_income_cagr
WHERE asset.id = net_income_cagr.asset_id
  AND asset.net_income_5y_cagr IS NULL;
