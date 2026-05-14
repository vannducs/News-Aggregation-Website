using NewsAggregator.Models;

namespace NewsAggregator.Models.ViewModels;

public class AdminUserIndexViewModel
{
    public string? Keyword { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
    public bool IncludeDeleted { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public int TotalRecords { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int DeletedUsers { get; set; }
    public IReadOnlyCollection<AppUser> Users { get; set; } = Array.Empty<AppUser>();
}
