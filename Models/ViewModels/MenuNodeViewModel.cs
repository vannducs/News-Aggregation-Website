namespace NewsAggregator.Models.ViewModels;

public class MenuNodeViewModel
{
    public int MenuID { get; set; }
    public string MenuName { get; set; } = string.Empty;
    public string Url { get; set; } = "#";
    public int MenuOrder { get; set; }
    public IReadOnlyCollection<MenuNodeViewModel> Children { get; set; } = Array.Empty<MenuNodeViewModel>();
}
