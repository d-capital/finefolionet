using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Finefolio.ValuationApi.Authentication;

public sealed class ArticleTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ArticleToken";

    private readonly byte[] _configuredTokenHash;

    public ArticleTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        var token = configuration["ArticleApiToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "ArticleApiToken must be configured through user secrets or an environment variable.");
        }

        _configuredTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authorization)
            || !System.Net.Http.Headers.AuthenticationHeaderValue.TryParse(
                authorization.ToString(),
                out var header)
            || !string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(header.Parameter))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var suppliedTokenHash = SHA256.HashData(Encoding.UTF8.GetBytes(header.Parameter));
        if (!CryptographicOperations.FixedTimeEquals(_configuredTokenHash, suppliedTokenHash))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid article API token."));
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "article-api-client")],
            SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers["WWW-Authenticate"] = "Bearer";
        return Task.CompletedTask;
    }
}
