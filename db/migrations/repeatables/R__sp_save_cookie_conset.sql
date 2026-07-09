DROP PROCEDURE IF EXISTS sp_save_cookie_consent(varchar, varchar);
DROP FUNCTION IF EXISTS sp_save_cookie_consent(varchar, varchar);

CREATE OR REPLACE FUNCTION sp_save_cookie_consent(
    p_user_id VARCHAR(255),
    p_user_agent VARCHAR(255)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO cookie_consent (user_id, user_agent, created_at)
    VALUES (p_user_id, p_user_agent, CURRENT_DATE)
    ON CONFLICT (user_id)
    DO UPDATE SET
        user_agent = EXCLUDED.user_agent,
        created_at = CURRENT_DATE;
END;
$$;