using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.RegularExpressions;

namespace NewsAggregator.Controllers
{
    public class TtsController(IConfiguration config, IHttpClientFactory factory) : Controller
    {
        private readonly IConfiguration _config = config;
        private readonly HttpClient _http = factory.CreateClient();

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

                var cleanText = CleanTextForTts(request.Text);
                if (string.IsNullOrWhiteSpace(cleanText))
                    return BadRequest(new { error = "Không có nội dung văn bản để đọc!" });

                cleanText = cleanText.Length > 500 ? cleanText[..500] : cleanText;

                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("api-key", apiKey);
                _http.DefaultRequestHeaders.Add("speed", "0");
                _http.DefaultRequestHeaders.Add("voice", request.Voice ?? "banmai");

                var content = new StringContent(cleanText, Encoding.UTF8, "text/plain");
                var response = await _http.PostAsync(url, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[TTS] Status: {response.StatusCode}");
                Console.WriteLine($"[TTS] Response: {responseJson[..Math.Min(200, responseJson.Length)]}");

                if (!response.IsSuccessStatusCode)
                    return StatusCode(500, new { error = "Lỗi TTS: " + responseJson });

                using var doc = System.Text.Json.JsonDocument.Parse(responseJson);
                var audioUrl = doc.RootElement.GetProperty("async").GetString();

                return Ok(new { audioUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Lỗi: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private static readonly Regex UrlRegex        = new(@"https?://\S+",  RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex   = new(@"[\r\n\t]+",    RegexOptions.Compiled);
        private static readonly Regex MultiSpaceRegex   = new(@"\s{2,}",       RegexOptions.Compiled);

        private static string CleanTextForTts(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var doc = new HtmlDocument();
            doc.LoadHtml(input);

            var removeNodes = doc.DocumentNode
                .SelectNodes("//img|//figure|//figcaption|//script|//style|//iframe|//video|//audio|//table")
                ?? Enumerable.Empty<HtmlNode>();

            foreach (var node in removeNodes.ToList())
                node.Remove();

            var text = doc.DocumentNode.InnerText;
            text = UrlRegex.Replace(text, " ");
            text = WhitespaceRegex.Replace(text, " ");
            text = MultiSpaceRegex.Replace(text, " ");
            text = System.Net.WebUtility.HtmlDecode(text);

            return text.Trim();
        }
    }

    public class TtsRequest
    {
        public string Text { get; set; } = string.Empty;
        public string? Voice { get; set; } = "banmai";
    }
}