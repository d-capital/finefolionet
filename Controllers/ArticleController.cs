using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finefolio.ValuationApi.Controllers;

[ApiController]
[Route("article")]
public sealed class ArticleController : ControllerBase
{
    private readonly IArticleRepository _repository;

    public ArticleController(IArticleRepository repository)
    {
        _repository = repository;
    }

    /// <summary>Returns the full article information for the specified URL name.</summary>
    [HttpGet("{urlName}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDto>> GetByUrlName(string urlName, CancellationToken cancellationToken)
    {
        var article = await _repository.GetByUrlNameAsync(urlName, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Creates an article with a generated ID.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ArticleDto>> Create(
        [FromBody] ArticleWriteRequest request,
        CancellationToken cancellationToken)
    {
        var article = await _repository.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByUrlName), new { urlName = article.UrlName }, article);
    }

    /// <summary>Replaces the article with the specified ID.</summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ArticleDto>> UpdateById(
        int id,
        [FromBody] ArticleWriteRequest request,
        CancellationToken cancellationToken)
    {
        var article = await _repository.UpdateByIdAsync(id, request, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Returns the ten newest visible articles, ordered by ID descending.</summary>
    [HttpGet("top")]
    [ProducesResponseType(typeof(IReadOnlyList<ArticleSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ArticleSummaryDto>>> GetTop(CancellationToken cancellationToken)
    {
        var articles = await _repository.GetTopArticlesAsync(cancellationToken);
        return Ok(articles);
    }
}
