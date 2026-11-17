using System.ComponentModel.DataAnnotations;

namespace Finefolio.ValuationApi.Models;

/// <summary>Article fields to create or replace.</summary>
public sealed record ArticleWriteRequest
{
    [Required]
    public required string UrlName { get; init; }

    [Required]
    public required string Title { get; init; }

    public string? Excerpt { get; init; }

    [MaxLength(100)]
    public string? MetaTitle { get; init; }

    [MaxLength(255)]
    public string? MetaDescription { get; init; }

    [Required]
    public required string Content { get; init; }

    public string? PublishedAt { get; init; }
    public bool? IsHidden { get; init; }
}
