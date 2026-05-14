namespace NewsAggregator.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalPosts { get; set; }
        public int TotalMenus { get; set; }
        public int TotalUsers { get; set; }
        public int TotalComments { get; set; }
        public List<Post> RecentPosts { get; set; } = new();
        public List<Comment> RecentComments { get; set; } = new();
    }
}
