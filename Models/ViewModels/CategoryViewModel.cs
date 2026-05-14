namespace NewsAggregator.Models.ViewModels
{
    public class CategoryViewModel
    {
        public Menu? CurrentMenu { get; set; }
        public List<Menu> Menus { get; set; } = new();
        public List<Post> Posts { get; set; } = new();
    }
}
