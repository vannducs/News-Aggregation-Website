namespace NewsAggregator.Models.ViewModels
{
    public class PostDetailsViewModel
    {
        public Post Post { get; set; } = new();
        public List<Menu> Menus { get; set; } = new();
        public List<Post> RelatedPosts { get; set; } = new();
        public List<Comment> Comments { get; set; } = new();
        public CommentInputModel CommentForm { get; set; } = new();
    }
}
