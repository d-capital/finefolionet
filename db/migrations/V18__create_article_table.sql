CREATE TABLE article (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    url_name text NOT NULL UNIQUE,
    title text NOT NULL,
    excerpt text,
    meta_title varchar(100),
    meta_description varchar(255),
    content text NOT NULL,
    published_at text,
    is_hidden boolean DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    last_updated_at timestamptz NOT NULL DEFAULT now()
);
