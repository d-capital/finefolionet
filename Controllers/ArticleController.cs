using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Repositories;
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

    /// <summary>Returns the full article information for the specified ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ArticleDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var article = await _repository.GetByIdAsync(id, cancellationToken);
        return article is null ? NotFound() : Ok(article);
    }

    /// <summary>Creates an article with a generated ID.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ArticleDto>> Create(
        [FromBody] ArticleWriteRequest request,
        CancellationToken cancellationToken)
    {
        var article = await _repository.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
    }

    /// <summary>Replaces the article with the specified ID.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ArticleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
