namespace NewsAggregator.Models.ViewModels
{
    /// <summary>
    /// ViewModel cho trang tìm kiếm bài viết.
    /// Chứa kết quả, bộ lọc, phân trang và dữ liệu phụ trợ (comment count).
    /// </summary>
    public class SearchViewModel
    {
        // ── Tham số tìm kiếm ──────────────────────────────────────────────────
        public string  Keyword    { get; set; } = string.Empty;
        public int?    CategoryID { get; set; }
        public string  SortBy     { get; set; } = "newest";

        /// <summary>Khoảng thời gian lọc: "" | "today" | "week" | "month" | "custom"</summary>
        public string  DateRange  { get; set; } = string.Empty;

        /// <summary>Ngày bắt đầu cho custom date range (ISO string, ví dụ "2025-01-01")</summary>
        public string  DateFrom   { get; set; } = string.Empty;

        /// <summary>Ngày kết thúc cho custom date range (ISO string)</summary>
        public string  DateTo     { get; set; } = string.Empty;

        // ── Dữ liệu kết quả ───────────────────────────────────────────────────
        public List<Post> Results    { get; set; } = new();
        public List<Menu> Categories { get; set; } = new();

        /// <summary>Số bình luận đã duyệt cho mỗi bài viết trong trang hiện tại.</summary>
        public Dictionary<int, int> CommentCounts { get; set; } = new();

        // ── Phân trang ────────────────────────────────────────────────────────
        public int CurrentPage  { get; set; } = 1;
        public int TotalPages   { get; set; } = 1;
        public int TotalResults { get; set; } = 0;
        public int PageSize     { get; set; } = 8;

        // ── Computed helpers ──────────────────────────────────────────────────
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage     => CurrentPage < TotalPages;

        /// <summary>Trả về số comment của bài viết, mặc định 0 nếu không có dữ liệu.</summary>
        public int GetCommentCount(int postId)
            => CommentCounts.TryGetValue(postId, out var c) ? c : 0;
    }
}
