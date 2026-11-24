namespace Finefolio.ValuationApi.Models;

/// <summary>Average PEG for a sector and market.</summary>
public sealed record SectorPegResponse(
    string Market,
    string Language,
    string Sector,
    double? AveragePeg);
