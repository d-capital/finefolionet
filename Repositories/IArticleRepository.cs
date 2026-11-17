using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Repositories;

public interface IArticleRepository
{
    Task<ArticleDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ArticleDto> CreateAsync(ArticleWriteRequest request, CancellationToken cancellationToken);
    Task<ArticleDto?> UpdateByIdAsync(int id, ArticleWriteRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ArticleSummaryDto>> GetTopArticlesAsync(CancellationToken cancellationToken);
}
