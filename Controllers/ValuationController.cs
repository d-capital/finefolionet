using Microsoft.AspNetCore.Authorization;
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
        [FromQuery, BindRequired] decimal minDividend,
        [FromQuery] string? exchange,
        CancellationToken cancellationToken,
        [FromQuery] decimal minEps = 15)
    {
        var candidates = await _service.GetScreenerCandidatesAsync(minDividend, minEps, exchange, cancellationToken);
        return Ok(candidates);
    }

    /// <summary>
    /// Returns average PEGs for all sectors, optionally filtered by market.
    /// </summary>
    [HttpGet("sector-peg")]
    [ProducesResponseType(typeof(IReadOnlyList<SectorPegResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<SectorPegResponse>>> GetSectorPegs(
        [FromQuery] string? market,
        CancellationToken cancellationToken)
    {
        var marketFilter = string.IsNullOrWhiteSpace(market) ? null : market.Trim();
        if (marketFilter is not null &&
            !marketFilter.Equals("US", StringComparison.OrdinalIgnoreCase) &&
            !marketFilter.Equals("MOEX", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Market must be either 'US' or 'MOEX'.");
        }

        var sectorPegs = await _service.GetSectorPegsAsync(marketFilter, cancellationToken);
        return Ok(sectorPegs);
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
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    [HttpPost("{exchange}/{ticker}/free-cash-flow")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostFreeCashFlow(string exchange, string ticker, [FromBody] FreeCashFlowUpdateDto dto)
    {
        if (dto == null || dto.Year == null || dto.Value == null)
        {
            return BadRequest("Year and Value are required.");
        }

        var result = await _service.AddOrUpdateFreeCashFlowAsync(exchange, ticker, dto.Year.Value, dto.Value.Value);
        if (!result) return NotFound();
        return Ok();
    }

}
