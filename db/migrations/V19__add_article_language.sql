ALTER TABLE article
    ADD COLUMN language varchar(2) NOT NULL DEFAULT 'en'
        CONSTRAINT ck_article_language CHECK (language ~ '^[a-z]{2}$');
