using System.Data;
using Dapper;
using EvolveDb;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Cors;
using Npgsql;
using Finefolio.ValuationApi.Repositories;
using Finefolio.ValuationApi.Services;
using Finefolio.ValuationApi.Services.PriceProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

app.UseCors("AllowAll");
app.MapControllers();

app.Run();
