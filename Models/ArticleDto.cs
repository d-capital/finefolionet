namespace Finefolio.ValuationApi.Models;

/// <summary>Full article information returned by the API.</summary>
public sealed record ArticleDto
{
    public int Id { get; init; }
    public string UrlName { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Excerpt { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? PublishedAt { get; init; }
    public bool? IsHidden { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastUpdatedAt { get; init; }
}
