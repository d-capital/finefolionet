namespace Finefolio.ValuationApi.Models;

public class AssetLabelDto
{
    public int Id { get; set; }
    public int AssetId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}