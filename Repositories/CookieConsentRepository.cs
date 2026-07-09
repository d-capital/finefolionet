using Dapper;
using Npgsql;

namespace Finefolio.ValuationApi.Repositories;

public class CookieConsentRepository : ICookieConsentRepository
{
    private readonly string _connectionString;

    public CookieConsentRepository(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    public async Task SaveCookieConsentAsync(string userId, string? userAgent)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        const string sql = "SELECT sp_save_cookie_consent(@p_user_id, @p_user_agent);";

        await conn.ExecuteAsync(
            sql,
            new { p_user_id = userId, p_user_agent = userAgent });
    }
}
