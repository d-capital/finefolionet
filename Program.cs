using System.Data;
using System.Net;
using System.Threading.RateLimiting;
using Dapper;
using EvolveDb;
using Finefolio.ValuationApi.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Finefolio.ValuationApi.Repositories;
using Finefolio.ValuationApi.RateLimiting;
using Finefolio.ValuationApi.Services;
using Finefolio.ValuationApi.Services.PriceProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

var exemptNetworks = builder.Configuration
    .GetSection("RateLimiting:InternalNetworks")
    .Get<string[]>()?
    .Select(IPNetwork.Parse)
    .ToArray() ?? [];

if (string.IsNullOrWhiteSpace(builder.Configuration["ArticleApiToken"]))
{
    throw new InvalidOperationException(
        "ArticleApiToken must be configured through user secrets or an environment variable.");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddAuthentication(ArticleTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ArticleTokenAuthenticationHandler>(
        ArticleTokenAuthenticationHandler.SchemeName,
        _ => { });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("api-per-client", context =>
    {
        var partition = RateLimitClientAddress.Resolve(context, exemptNetworks);
        if (partition.IsExempt)
        {
            return RateLimitPartition.GetNoLimiter(partition.Key);
        }

        return RateLimitPartition.GetFixedWindowLimiter(
            partition.Key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = async (rejection, cancellationToken) =>
    {
        rejection.HttpContext.Response.Headers["Retry-After"] = "60";
        await rejection.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too Many Requests",
                Detail = "The request limit has been reached. Try again in one minute."
            },
            cancellationToken);
    };
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddSingleton<IValuationRepository, ValuationRepository>();
builder.Services.AddSingleton<IAssetFundamentalsRepository, AssetFundamentalsRepository>();
builder.Services.AddSingleton<ICookieConsentRepository, CookieConsentRepository>();
builder.Services.AddSingleton<IArticleRepository, ArticleRepository>();
builder.Services.AddSingleton<IValuationService, ValuationService>();
builder.Services.AddSingleton<IAssetFundamentalsService, AssetFundamentalsService>();
builder.Services.AddSingleton<ICookieConsentService, CookieConsentService>();

// Register price providers and HTTP clients
builder.Services.AddHttpClient("moex");
builder.Services.AddHttpClient("tradingview");
builder.Services.AddSingleton<IMoexPriceProvider, MoexPriceProvider>();
builder.Services.AddSingleton<ITradingViewPriceProvider, TradingViewPriceProvider>();
builder.Services.AddSingleton<PriceService>();
builder.Services.AddSingleton<IAssetUpdateService, AssetUpdateService>();
builder.Services.AddHostedService<AssetUpdateBackgroundService>();
builder.Services.AddHostedService<BroadIndexMetricsBackgroundService>();


var app = builder.Build();

// Enable Dapper snake_case mapping so database columns like
// `market_cap_basic` map to `MarketCapBasic` properties.
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Run Evolve migrations on startup
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

using (var conn = new NpgsqlConnection(connStr))
{
    conn.Open();
    var evolve = new Evolve(conn, msg => Console.WriteLine(msg))
    {
        Locations = new[] { "db/migrations" },
        IsEraseDisabled = true
    };
    evolve.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowAll");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("api-per-client");

app.Run();
