namespace Topstar.Web.Features.Admin.Menu;

public sealed class MenuFormInput
{
    public Guid Id { get; set; }
    public string ScopeCode { get; set; } = "ADMIN";
    public string Title { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? Icon { get; set; }
    public Guid? ParentId { get; set; }
    public int OrderNo { get; set; } = 1;
    public bool IsVisible { get; set; } = true;
}

public sealed record MenuParentVm(Guid Id, string DisplayText);
