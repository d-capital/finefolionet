CREATE OR REPLACE FUNCTION sp_get_free_cash_flow_history(p_asset_id int)
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
    FROM free_cash_flow
    WHERE asset_id = p_asset_id;
$$ LANGUAGE sql STABLE;
