namespace Finefolio.ValuationApi.Models;

/// <summary>Brief article information for the article list.</summary>
public sealed record ArticleSummaryDto(
    int Id,
    string UrlName,
    string Title,
    string? Excerpt);
