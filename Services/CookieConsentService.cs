using Finefolio.ValuationApi.Models;
using Finefolio.ValuationApi.Repositories;

namespace Finefolio.ValuationApi.Services;

public class CookieConsentService : ICookieConsentService
{
    private readonly ICookieConsentRepository _repository;

    public CookieConsentService(ICookieConsentRepository repository)
    {
        _repository = repository;
    }

    public async Task SaveCookieConsentAsync(CookieConsentDataDto request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        await _repository.SaveCookieConsentAsync(request.UserId, request.UserAgent);
    }
}
