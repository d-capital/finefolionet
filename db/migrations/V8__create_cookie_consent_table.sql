-- Create cookie consent table
CREATE TABLE IF NOT EXISTS cookie_consent (
    id serial PRIMARY KEY,
    user_id varchar(255) NOT NULL UNIQUE,
    user_agent varchar(255),
    created_at date DEFAULT CURRENT_DATE
);