using Topstar.Application.Common;
namespace Topstar.Web.Features.Common.Paging;
public sealed record ListWindowViewModel(string Id, ListWindow Window, string Part = "All", bool ShowReset = true);
