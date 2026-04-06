namespace Topstar.Infrastructure.Entities;

public sealed class ProvinceEntity
{
    public Guid Id { get; set; }
    public int ProvinceCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CodeName { get; set; }
    public int? GhnProvinceId { get; set; }
    public string? ExtensionNamesJson { get; set; }
    public string? Type { get; set; }
    public int? ParentId { get; set; }
    public int? GhnStatus { get; set; }
    public string? UpdatedIp { get; set; }
    public int? UpdatedEmployee { get; set; }
    public string? UpdatedSource { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? CreatedIp { get; set; }
    public int? CreatedEmployee { get; set; }
    public string? CreatedSource { get; set; }
    public DateTime? CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class WardEntity
{
    public Guid Id { get; set; }
    public string WardCode { get; set; } = string.Empty;
    public Guid ProvinceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CodeName { get; set; }
    public int? GhnWardId { get; set; }
    public string? ExtensionNamesJson { get; set; }
    public string? Type { get; set; }
    public int? GhnParentProvinceId { get; set; }
    public int? GhnStatus { get; set; }
    public string? UpdatedIp { get; set; }
    public int? UpdatedEmployee { get; set; }
    public string? UpdatedSource { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? CreatedIp { get; set; }
    public int? CreatedEmployee { get; set; }
    public string? CreatedSource { get; set; }
    public DateTime? CreatedDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
