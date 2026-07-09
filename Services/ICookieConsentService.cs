using Finefolio.ValuationApi.Models;

namespace Finefolio.ValuationApi.Services;

public interface ICookieConsentService
{
    Task SaveCookieConsentAsync(CookieConsentDataDto request);
}
