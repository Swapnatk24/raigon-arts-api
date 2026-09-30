using System.Text.Json.Serialization;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.DTOs;

public class BackupExportDto
{
    [JsonPropertyName("backupVersion")]
    public string BackupVersion { get; set; } = "1.0";

    [JsonPropertyName("exportedAt")]
    public string ExportedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

    [JsonPropertyName("workshop")]
    public string Workshop { get; set; } = "Raigon Arts";

    [JsonPropertyName("customers")]
    public List<Customer> Customers { get; set; } = new();

    [JsonPropertyName("orders")]
    public List<Order> Orders { get; set; } = new();

    [JsonPropertyName("frames")]
    public List<FrameSize> Frames { get; set; } = new();

    [JsonPropertyName("settings")]
    public WorkshopSetting? Settings { get; set; }
}

public class BackupRestoreRequest
{
    [JsonPropertyName("backupVersion")]
    public string BackupVersion { get; set; } = "1.0";

    [JsonPropertyName("customers")]
    public List<Customer>? Customers { get; set; }

    [JsonPropertyName("orders")]
    public List<Order>? Orders { get; set; }

    [JsonPropertyName("frames")]
    public List<FrameSize>? Frames { get; set; }

    [JsonPropertyName("settings")]
    public WorkshopSetting? Settings { get; set; }
}

public class BackupRestoreResponseData
{
    [JsonPropertyName("restoredCustomers")]
    public int RestoredCustomers { get; set; }

    [JsonPropertyName("restoredOrders")]
    public int RestoredOrders { get; set; }

    [JsonPropertyName("restoredFrames")]
    public int RestoredFrames { get; set; }
}
