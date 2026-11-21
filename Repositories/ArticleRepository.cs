using Dapper;
using Finefolio.ValuationApi.Models;
using Npgsql;

namespace Finefolio.ValuationApi.Repositories;

public sealed class ArticleRepository : IArticleRepository
{
    private readonly string _connectionString;

    public ArticleRepository(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task<ArticleDto?> GetByUrlNameAsync(string urlName, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT id, url_name, language, title, excerpt, meta_title, meta_description, content,
                   published_at, is_hidden, created_at, last_updated_at
            FROM article
            WHERE url_name = @urlName;
            """;

        return await conn.QuerySingleOrDefaultAsync<ArticleDto>(
            new CommandDefinition(sql, new { urlName }, cancellationToken: cancellationToken));
    }

    public async Task<ArticleDto> CreateAsync(
        ArticleWriteRequest request,
        CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            INSERT INTO article (
                url_name, language, title, excerpt, meta_title, meta_description, content, published_at, is_hidden
            )
            VALUES (
                @UrlName, @Language, @Title, @Excerpt, @MetaTitle, @MetaDescription, @Content, @PublishedAt, @IsHidden
            )
            RETURNING id, url_name, language, title, excerpt, meta_title, meta_description, content,
                      published_at, is_hidden, created_at, last_updated_at;
            """;

        return await conn.QuerySingleAsync<ArticleDto>(
            new CommandDefinition(
                sql,
                request,
                cancellationToken: cancellationToken));
    }

    public async Task<ArticleDto?> UpdateByIdAsync(
        int id,
        ArticleWriteRequest request,
        CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            UPDATE article
            SET url_name = @UrlName,
                language = @Language,
                title = @Title,
                excerpt = @Excerpt,
                meta_title = @MetaTitle,
                meta_description = @MetaDescription,
                content = @Content,
                published_at = @PublishedAt,
                is_hidden = @IsHidden,
                last_updated_at = now()
            WHERE id = @id
            RETURNING id, url_name, language, title, excerpt, meta_title, meta_description, content,
                      published_at, is_hidden, created_at, last_updated_at;
            """;

        return await conn.QuerySingleOrDefaultAsync<ArticleDto>(
            new CommandDefinition(sql, new { id, request.UrlName, request.Language, request.Title, request.Excerpt,
                request.MetaTitle, request.MetaDescription, request.Content, request.PublishedAt,
                request.IsHidden }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<ArticleSummaryDto>> GetTopArticlesAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        const string sql = """
            SELECT id, url_name AS "UrlName", language AS "Language", title, excerpt
            FROM article
            WHERE is_hidden IS FALSE
            ORDER BY id DESC
            LIMIT 10;
            """;

        var articles = await conn.QueryAsync<ArticleSummaryDto>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return articles.AsList();
    }
}
