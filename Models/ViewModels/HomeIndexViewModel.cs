namespace NewsAggregator.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Menu> Menus { get; set; } = new();
        public List<Post> FeaturedPosts { get; set; } = new();
        public List<Post> LatestPosts { get; set; } = new();
        public List<Post> PopularPosts { get; set; } = new();
    }
}
