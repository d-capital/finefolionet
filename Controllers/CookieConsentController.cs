using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace Finefolio.ValuationApi.Controllers;

[ApiController]
[Route("cookie-consent")]
public class CookieConsentController : ControllerBase
{
    private readonly ICookieConsentService _service;

    public CookieConsentController(ICookieConsentService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] CookieConsentDataDto request)
    {
        if (request == null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest("userId is required.");
        }

        await _service.SaveCookieConsentAsync(request);

        return Ok(new { success = true });
    }
}
