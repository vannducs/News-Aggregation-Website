using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace NewsAggregator.Controllers
{
    public class TtsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public TtsController(IConfiguration config, IHttpClientFactory factory)
        {
            _config = config;
            _http = factory.CreateClient();
        }

        // POST: /Tts/Speak
        [HttpPost]
        public async Task<IActionResult> Speak([FromBody] TtsRequest request)
        {
            if (string.IsNullOrEmpty(request.Text))
                return BadRequest(new { error = "Text không được trống!" });

            try
            {
                var apiKey = _config["FptAiKey"];
                var url = "https://api.fpt.ai/hmi/tts/v5";

                // Giới hạn 500 ký tự mỗi request (FPT AI giới hạn)
                var text = request.Text.Length > 500
                    ? request.Text.Substring(0, 500)
                    : request.Text;

                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", apiKey);
                _http.DefaultRequestHeaders.Add("speed", "0");
                _http.DefaultRequestHeaders.Add("voice", request.Voice ?? "banmai");

                var content = new StringContent(text, Encoding.UTF8, "text/plain");
                var response = await _http.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[TTS] Status: {response.StatusCode}");
                Console.WriteLine($"[TTS] Response: {responseJson[..Math.Min(200, responseJson.Length)]}");

                if (!response.IsSuccessStatusCode)
                    return StatusCode(500, new { error = "Lỗi TTS: " + responseJson });

                // FPT AI trả về URL file audio
                using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
                var audioUrl = doc.RootElement
                    .GetProperty("async")
                    .GetString();

                return Ok(new { audioUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Lỗi: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class TtsRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? Voice { get; set; } = "banmai";
    }
}