WITH latest_net_income AS (
    SELECT DISTINCT ON (asset_id)
        asset_id,
        value
    FROM net_income
    ORDER BY asset_id, year DESC, id DESC
),
net_income_endpoints AS (
    SELECT
        latest.asset_id,
        CASE
            WHEN five_observations_ago.value = 0 THEN 0.1::double precision
            ELSE five_observations_ago.value
        END AS beginning_value,
        latest.value AS ending_value
    FROM latest_net_income latest
    LEFT JOIN LATERAL (
        SELECT value
        FROM net_income
        WHERE asset_id = latest.asset_id
        ORDER BY year DESC, id DESC
        OFFSET 4
        LIMIT 1
    ) five_observations_ago ON TRUE
),
cagr_ratios AS (
    SELECT
        asset_id,
        beginning_value,
        ending_value,
        (ending_value - beginning_value + abs(beginning_value)) / abs(beginning_value) AS ratio
    FROM net_income_endpoints
),
net_income_cagr AS (
    SELECT
        asset_id,
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
)
UPDATE asset
SET net_income_5y_cagr = net_income_cagr.cagr
FROM net_income_cagr
WHERE asset.id = net_income_cagr.asset_id;
