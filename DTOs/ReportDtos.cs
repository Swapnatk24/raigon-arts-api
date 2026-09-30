using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class SettlementStatsDto
{
    [JsonPropertyName("paidCount")]
    public int PaidCount { get; set; }

    [JsonPropertyName("paidPercentage")]
    public int PaidPercentage { get; set; }

    [JsonPropertyName("partialCount")]
    public int PartialCount { get; set; }

    [JsonPropertyName("partialPercentage")]
    public int PartialPercentage { get; set; }

    [JsonPropertyName("unpaidCount")]
    public int UnpaidCount { get; set; }

    [JsonPropertyName("unpaidPercentage")]
    public int UnpaidPercentage { get; set; }
}

public class ReportCardsDto
{
    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("advanceCollected")]
    public decimal AdvanceCollected { get; set; }

    [JsonPropertyName("outstandingBalance")]
    public decimal OutstandingBalance { get; set; }

    [JsonPropertyName("highestOrderOfDay")]
    public decimal HighestOrderOfDay { get; set; }

    [JsonPropertyName("hod")]
    public decimal Hod { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }
}

public class FinancialReportDto
{
    [JsonPropertyName("totalBilled")]
    public decimal TotalBilled { get; set; }

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("totalCollected")]
    public decimal TotalCollected { get; set; }

    [JsonPropertyName("advanceCollected")]
    public decimal AdvanceCollected { get; set; }

    [JsonPropertyName("totalOutstanding")]
    public decimal TotalOutstanding { get; set; }

    [JsonPropertyName("outstandingBalance")]
    public decimal OutstandingBalance { get; set; }

    [JsonPropertyName("highestOrderOfDay")]
    public decimal HighestOrderOfDay { get; set; }

    [JsonPropertyName("hod")]
    public decimal Hod { get; set; }

    [JsonPropertyName("totalOrders")]
    public int TotalOrders { get; set; }

    [JsonPropertyName("settlementStats")]
    public SettlementStatsDto SettlementStats { get; set; } = new();
}

public class ProductionBreakdownItemDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }
}

public class FrameSizeSalesBreakdownDto
{
    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }
}

public class MaterialSalesBreakdownDto
{
    [JsonPropertyName("material")]
    public string Material { get; set; } = string.Empty;

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("percentage")]
    public int Percentage { get; set; }
}

public class ProductionAnalyticsDto
{
    [JsonPropertyName("topFrameSizes")]
    public List<FrameSizeSalesBreakdownDto> TopFrameSizes { get; set; } = new();

    [JsonPropertyName("popularMaterials")]
    public List<MaterialSalesBreakdownDto> PopularMaterials { get; set; } = new();

    [JsonPropertyName("frameTypes")]
    public List<ProductionBreakdownItemDto> FrameTypes { get; set; } = new();

    [JsonPropertyName("totalVolume")]
    public int TotalVolume { get; set; }
}
