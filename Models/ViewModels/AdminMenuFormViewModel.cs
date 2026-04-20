using Microsoft.AspNetCore.Mvc.Rendering;

namespace NewsAggregator.Models.ViewModels;

public class AdminMenuFormViewModel
{
    public int MenuID { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public int Levels { get; set; } = 1;
    public int ParentID { get; set; }
    public string? Link { get; set; }
    public int MenuOrder { get; set; }
    public int Position { get; set; } = 1;
    public IReadOnlyCollection<SelectListItem> ParentOptions { get; set; } = Array.Empty<SelectListItem>();
    public IReadOnlyCollection<SelectListItem> PositionOptions { get; set; } =
    [
        new("Main Navigation", "1"),
        new("Footer / Secondary", "2")
    ];
}
