using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace NewsAggregator.Controllers
{
    public class ChatController : Controller
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public ChatController(IConfiguration config, IHttpClientFactory factory)
        {
            _config = config;
            _http = factory.CreateClient();
        }

        // POST: /Chat/Ask
        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (string.IsNullOrEmpty(request.Message))
                return BadRequest(new { error = "Tin nhắn không được trống!" });

            try
            {
                var apiKey = _config["GroqApiKey"];
                var url = "https://api.groq.com/openai/v1/chat/completions";

                // Ghép context bài viết vào message nếu có
                var fullMessage = string.IsNullOrEmpty(request.Context)
                    ? request.Message
                    : $"Dựa trên bài viết sau:\n{request.Context}\n\nCâu hỏi: {request.Message}";

                var body = new
                {
                    model = "llama-3.3-70b-versatile",
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = """
                                Bạn là trợ lý AI thông minh của trang web tin tức NewsAggregator.
                                Nhiệm vụ của bạn là:
                                1. Trả lời câu hỏi về tin tức, thời sự
                                2. Tóm tắt nội dung bài viết khi được yêu cầu
                                3. Giải thích các sự kiện, khái niệm trong tin tức
                                Hãy trả lời ngắn gọn, dễ hiểu bằng tiếng Việt.
                                """
                        },
                        new
                        {
                            role = "user",
                            content = fullMessage
                        }
                    },
                    max_tokens = 1024,
                    temperature = 0.7
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Thêm Authorization header
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add(
                    "Authorization", $"Bearer {apiKey}");

                var response = await _http.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[ChatBot] Status: {response.StatusCode}");
                Console.WriteLine($"[ChatBot] Response: {responseJson[..Math.Min(200, responseJson.Length)]}");

                // Xử lý rate limit
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    return Ok(new { reply = "AI đang bận, vui lòng thử lại sau 1 phút! ⏳" });

                if (!response.IsSuccessStatusCode)
                    return Ok(new { reply = "AI tạm thời không khả dụng, vui lòng thử lại!" });

                // Parse kết quả Groq — format giống OpenAI
                using var doc = JsonDocument.Parse(responseJson);
                var text = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return Ok(new { reply = text });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ChatBot] Lỗi: {ex.Message}");
                return Ok(new { reply = "Xin lỗi, có lỗi xảy ra: " + ex.Message });
            }
        }
    }

    // Model nhận request từ frontend
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? Context { get; set; }
    }
}