using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Finefolio.ValuationApi.Services;
using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Controllers;

[ApiController]
[Route("valuation")]
public class ValuationController : ControllerBase
{
    private readonly IValuationService _service;

    public ValuationController(IValuationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns up to ten stocks matching the requested screening thresholds, ranked by EPS.
    /// </summary>
    [HttpGet("screener")]
    [ProducesResponseType(typeof(IReadOnlyList<StockScreenerCandidateResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockScreenerCandidateResponse>>> GetScreenerCandidates(
        [FromQuery, BindRequired] decimal maxPe,
        [FromQuery, BindRequired] decimal minDividend,
        [FromQuery] string? exchange,
        CancellationToken cancellationToken)
    {
        var candidates = await _service.GetScreenerCandidatesAsync(maxPe, minDividend, exchange, cancellationToken);
        return Ok(candidates);
    }

    [HttpGet("{lang}/{exchange}/{ticker}")]
    public async Task<IActionResult> Get(string lang, string exchange, string ticker)
    {
        Console.WriteLine($"Requested valuation for {lang},{exchange},{ticker}");
        var val = await _service.GetValuationAsync(exchange, ticker, lang);
        if (val == null) return NotFound();
        return Ok(val);
    }

    [HttpGet("dcf/{lang}/{exchange}/{ticker}")]
    public async Task<IActionResult> GetDcf(string lang, string exchange, string ticker)
    {
        Console.WriteLine($"Requested DCF valuation for {lang},{exchange},{ticker}");
        var val = await _service.GetDcfValuationAsync(exchange, ticker, lang);
        if (val == null) return NotFound();
        return Ok(val);
    }

    [HttpPost("{exchange}/{ticker}/net-income")]
    public async Task<IActionResult> PostNetIncome(string exchange, string ticker, [FromBody] NetIncomeUpdateDto dto)
    {
        if (dto == null || dto.Year == null || dto.Value == null)
        {
            return BadRequest("Year and Value are required.");
        }

        var result = await _service.AddOrUpdateNetIncomeAsync(exchange, ticker, dto.Year.Value, dto.Value.Value);
        if (!result) return NotFound();
        return Ok();
    }

}
