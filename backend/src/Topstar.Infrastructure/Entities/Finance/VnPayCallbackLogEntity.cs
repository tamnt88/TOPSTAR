namespace Topstar.Infrastructure.Entities;

public sealed class VnPayCallbackLogEntity
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";
    public string? TargetReference { get; set; }
    public string? TransactionNo { get; set; }
    public string RawQuery { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public string? RemoteIp { get; set; }
    public string? UserAgent { get; set; }
    public bool? SignatureValid { get; set; }
    public bool? Success { get; set; }
    public string? ResponseCode { get; set; }
    public string? ResultMessage { get; set; }
    public string? ErrorDetail { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
