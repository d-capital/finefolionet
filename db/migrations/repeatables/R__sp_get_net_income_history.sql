-- Drop the function first to allow signature updates in repeatable migrations
DROP FUNCTION IF EXISTS sp_get_net_income_history(int);

-- Stored procedure to get valuation by market and ticker
CREATE OR REPLACE FUNCTION sp_get_net_income_history(p_asset_id int)
RETURNS TABLE(
    id int,
    asset_id int, 
    year int, 
    value double precision
)
AS $$
    SELECT 
    id,
    asset_id, 
    year, 
    value
    FROM net_income WHERE asset_id = p_asset_id;
$$ LANGUAGE sql STABLE;