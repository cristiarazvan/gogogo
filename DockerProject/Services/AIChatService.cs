using System.Text;
using System.Text.Json;
using DockerProject.Models;

namespace DockerProject.Services;

public class AIChatService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _faqData;
    private readonly ILogger<AIChatService> _logger;

    public AIChatService(IConfiguration configuration, IWebHostEnvironment environment, ILogger<AIChatService> logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;

        // Load API key from config file
        var configPath = Path.Combine(environment.ContentRootPath, "google-ai-config.json");
        _logger.LogInformation("Looking for Google AI config at: {Path}", configPath);

        if (File.Exists(configPath))
        {
            var configJson = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<JsonElement>(configJson);
            _apiKey = config.GetProperty("GoogleAI").GetProperty("ApiKey").GetString() ?? "";
            _logger.LogInformation("Google AI API key loaded: {KeyPreview}...",
                _apiKey.Length > 10 ? _apiKey.Substring(0, 10) : "TOO_SHORT");
        }
        else
        {
            _apiKey = "";
            _logger.LogError("Google AI config file NOT FOUND at {Path}", configPath);
        }

        // Load FAQ data
        var faqPath = Path.Combine(environment.ContentRootPath, "faq-data.json");
        if (File.Exists(faqPath))
        {
            _faqData = File.ReadAllText(faqPath);
        }
        else
        {
            _faqData = "{}";
            _logger.LogWarning("FAQ data file not found at {Path}", faqPath);
        }
    }

    public async Task<AIChatResponse> GetResponseAsync(string userMessage, ProductContext productContext)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return new AIChatResponse
            {
                Success = false,
                Message = "AI service is not configured. Please contact support."
            };
        }

        // Build the system prompt
        var systemPrompt = BuildSystemPrompt(productContext);

        // Build the request for Gemini API
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = $"{systemPrompt}\n\nUser question: {userMessage}" }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                topK = 40,
                topP = 0.95,
                maxOutputTokens = 1024
            },
            safetySettings = new[]
            {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_MEDIUM_AND_ABOVE" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_MEDIUM_AND_ABOVE" }
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";

            _logger.LogInformation("Calling Gemini API at: {Url}", url.Replace(_apiKey, "***API_KEY***"));

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gemini API response status: {StatusCode}", response.StatusCode);
            _logger.LogInformation("Gemini API response: {Content}", responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return new AIChatResponse
                {
                    Success = false,
                    Message = $"API Error: {response.StatusCode}. Please check the console for details."
                };
            }

            var responseData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            // Extract the text from the response
            var text = responseData
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return new AIChatResponse
            {
                Success = true,
                Message = text ?? "I couldn't generate a response. Please try again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return new AIChatResponse
            {
                Success = false,
                Message = "An error occurred while processing your request. Please try again."
            };
        }
    }

    private string BuildSystemPrompt(ProductContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are a helpful customer support assistant for GoGoGo, a food delivery platform.");
        sb.AppendLine("You MUST follow these rules strictly:");
        sb.AppendLine();
        sb.AppendLine("1. ONLY answer questions using the information provided below (FAQ data and product information).");
        sb.AppendLine("2. If the user asks something not covered by the provided information, politely say: \"I don't have information about that at the moment. Please contact our support team for further assistance.\"");
        sb.AppendLine("3. NEVER make up information or provide answers not based on the provided data.");
        sb.AppendLine("4. REFUSE to answer questions that are:");
        sb.AppendLine("   - Unrelated to food delivery, the platform, or the product");
        sb.AppendLine("   - Inappropriate, offensive, or harmful");
        sb.AppendLine("   - Asking for personal opinions or advice unrelated to the service");
        sb.AppendLine("   For such questions, respond: \"I'm here to help with questions about our food delivery service and products. Is there anything else I can help you with?\"");
        sb.AppendLine("5. Be friendly, concise, and helpful.");
        sb.AppendLine("6. If asked about this specific product, use the product details provided.");
        sb.AppendLine("7. Answer in the same language the user writes in (English or Romanian).");
        sb.AppendLine();
        sb.AppendLine("=== PRODUCT INFORMATION ===");
        sb.AppendLine($"Product Name: {context.ProductName}");
        sb.AppendLine($"Description: {context.ProductDescription}");
        sb.AppendLine($"Price: {context.Price:C}");
        sb.AppendLine($"Category: {context.CategoryName}");
        sb.AppendLine($"Restaurant: {context.RestaurantName}");
        sb.AppendLine($"In Stock: {(context.InStock ? "Yes" : "No")} ({context.StockCount} available)");
        sb.AppendLine();

        if (context.Reviews != null && context.Reviews.Any())
        {
            sb.AppendLine("=== CUSTOMER REVIEWS ===");
            foreach (var review in context.Reviews.Take(10)) // Limit to 10 reviews
            {
                sb.AppendLine($"- \"{review}\"");
            }
            sb.AppendLine();
        }

        sb.AppendLine("=== FAQ DATA ===");
        sb.AppendLine(_faqData);

        return sb.ToString();
    }
}

public class ProductContext
{
    public string ProductName { get; set; } = "";
    public string ProductDescription { get; set; } = "";
    public double Price { get; set; }
    public string CategoryName { get; set; } = "";
    public string RestaurantName { get; set; } = "";
    public bool InStock { get; set; }
    public int StockCount { get; set; }
    public List<string>? Reviews { get; set; }
}

public class AIChatResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public class AIChatRequest
{
    public int ProductId { get; set; }
    public string Message { get; set; } = "";
}
