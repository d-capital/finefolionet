namespace Finefolio.ValuationApi.Repositories;

public interface ICookieConsentRepository
{
    Task SaveCookieConsentAsync(string userId, string? userAgent);
}
