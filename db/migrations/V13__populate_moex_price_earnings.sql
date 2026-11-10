UPDATE asset
SET price_earnings_ttm = close / earnings_per_share_basic_ttm
WHERE exchange = 'MOEX'
  AND price_earnings_ttm IS NULL
  AND close IS NOT NULL
  AND earnings_per_share_basic_ttm IS NOT NULL
  AND earnings_per_share_basic_ttm <> 0;
