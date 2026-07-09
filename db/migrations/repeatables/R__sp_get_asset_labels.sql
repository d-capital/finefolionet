-- Drop the function first to allow signature updates in repeatable migrations
DROP FUNCTION IF EXISTS sp_get_asset_labels(int);

-- Stored procedure to get valuation by market and ticker
CREATE OR REPLACE FUNCTION sp_get_asset_labels(p_asset_id int)
RETURNS TABLE(
    id int,
    asset_id int, 
    label varchar, 
    language varchar,
    value varchar
)
AS $$
    SELECT 
    id,
    asset_id, 
    label, 
    language,
    value
    FROM asset_label WHERE asset_id = p_asset_id;
$$ LANGUAGE sql STABLE;