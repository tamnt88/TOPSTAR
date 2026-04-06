namespace Topstar.Core.Menus;

public static class MenuScopeOptions
{
    public const string All = "ALL";
    public const string Admin = "ADMIN";
    public const string Koc = "KOC";
    public const string Brand = "BRAND";

    public static readonly IReadOnlyList<string> ScopeValues = [All, Admin, Koc, Brand];

    public static readonly IReadOnlyList<MenuScopeItem> UiOptions =
    [
        new(All, "Tất cả"),
        new(Admin, "Admin"),
        new(Koc, "KOC"),
        new(Brand, "Brand")
    ];

    public static string Normalize(string? scope, bool allowAll = true)
    {
        var s = (scope ?? string.Empty).Trim().ToUpperInvariant();
        if (s == Admin || s == Koc || s == Brand)
        {
            return s;
        }

        return allowAll ? All : Admin;
    }
}

public sealed record MenuScopeItem(string Value, string Label);

public static class MenuCodeFormatter
{
    public static string BuildCode(string? scope, string suffix)
    {
        var normalizedScope = MenuScopeOptions.Normalize(scope, allowAll: false);
        var codeSuffix = (suffix ?? string.Empty).Trim().Replace(" ", "_").Replace("-", "_").ToUpperInvariant();
        return $"MENU_{normalizedScope}_{codeSuffix}";
    }

    public static (string scope, string suffix) ParseCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (MenuScopeOptions.Admin, string.Empty);
        }

        var parts = code.Split('_', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[0] == "MENU")
        {
            return (MenuScopeOptions.Normalize(parts[1], allowAll: false), string.Join('_', parts.Skip(2)));
        }

        return (MenuScopeOptions.Admin, code);
    }

    public static string ResolveScopeTag(string code)
    {
        if (code.StartsWith("MENU_ADMIN_")) return MenuScopeOptions.Admin;
        if (code.StartsWith("MENU_BRAND_")) return MenuScopeOptions.Brand;
        if (code.StartsWith("MENU_KOC_")) return MenuScopeOptions.Koc;
        return "KHÁC";
    }
}
