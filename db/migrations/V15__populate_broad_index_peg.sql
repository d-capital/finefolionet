WITH peg_inputs AS (
    SELECT
        id,
        price_earnings_ttm,
        LEAST(GREATEST(net_income_5y_cagr * 100, 0), 25) AS growth_rate
    FROM asset
    WHERE is_broad_index_part = TRUE
      AND net_income_5y_cagr IS NOT NULL
),
calculated_peg AS (
    SELECT
        id,
        CASE
            WHEN growth_rate = 0 THEN 0
            ELSE CASE
                WHEN round((price_earnings_ttm / growth_rate)::numeric, 2) < 0.01 THEN 0
                ELSE round((price_earnings_ttm / growth_rate)::numeric, 2)::double precision
            END
        END AS peg
    FROM peg_inputs
    WHERE growth_rate = 0
       OR price_earnings_ttm IS NOT NULL
)
UPDATE asset
SET peg = calculated_peg.peg
FROM calculated_peg
WHERE asset.id = calculated_peg.id;
