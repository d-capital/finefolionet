using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Finefolio.ValuationApi.Controllers;

[ApiController]
[Route("asset-fundamentals")]
public class AssetFundamentalsController : ControllerBase
{
    private readonly IAssetFundamentalsService _service;

    public AssetFundamentalsController(IAssetFundamentalsService service)
    {
        _service = service;
    }

    [HttpPatch("{exchange}/{ticker}")]
    public async Task<IActionResult> Patch(string exchange, string ticker, [FromBody] AssetFundamentalsUpdateDto request)
    {
        if (request == null || !request.HasAnyValue)
        {
            return BadRequest("At least one field must be provided.");
        }

        var updated = await _service.UpdateFundamentalsAsync(exchange, ticker, request);
        return updated ? Ok(new { success = true }) : NotFound();
    }
}
