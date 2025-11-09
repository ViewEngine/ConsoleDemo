using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ViewEngine.Console;

/// <summary>
/// Demo console application showing how to use ViewEngine's REST API with an API key
/// </summary>
class Program
{
    private static readonly HttpClient _httpClient = new();
    //private const string API_BASE_URL = "http://localhost:5072";
	private const string API_BASE_URL = "https://www.viewengine.io";

	// You'll need to provide your API key here
	private static string? _apiKey;

    static async Task Main(string[] args)
    {
        System.Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        System.Console.WriteLine("║          ViewEngine REST API Demo                       ║");
        System.Console.WriteLine("║  Demonstrates using the MCP endpoints with an API key   ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
        System.Console.WriteLine();

        // Get API key from command line or prompt
        if (args.Length > 0)
        {
            _apiKey = args[0];
        }
        else
        {
            System.Console.Write("Enter your API key: ");
            _apiKey = System.Console.ReadLine();
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            System.Console.WriteLine("❌ Error: API key is required");
            System.Console.WriteLine();
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("  dotnet run <api-key>");
            System.Console.WriteLine("  OR");
            System.Console.WriteLine("  dotnet run   (you'll be prompted for the API key)");
            return;
        }

        try
        {
            await RunDemoAsync();
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            System.Console.WriteLine();
            System.Console.WriteLine("Stack trace:");
            System.Console.WriteLine(ex.StackTrace);
        }

        System.Console.WriteLine();
        System.Console.WriteLine("Press any key to exit...");
        System.Console.ReadKey();
    }

    static async Task RunDemoAsync()
    {
        System.Console.WriteLine("🔍 Step 1: Discovering available MCP tools...");
        System.Console.WriteLine();

        var tools = await GetMcpToolsAsync();
        if (tools != null && tools.Count > 0)
        {
            System.Console.WriteLine($"✅ Found {tools.Count} available tools:");
            foreach (var tool in tools)
            {
                System.Console.WriteLine($"   • {tool.Name}: {tool.Description}");
            }
        }
        else
        {
            System.Console.WriteLine("⚠️  No tools found or API not responding");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        System.Console.WriteLine();

        // Get URL to retrieve
        System.Console.Write("Enter a URL to retrieve (or press Enter for example.com): ");
        var url = System.Console.ReadLine();
        if (string.IsNullOrWhiteSpace(url))
        {
            url = "https://example.com";
        }

        System.Console.WriteLine();
        System.Console.Write("Force fresh retrieval? (y/n, default: n - use cache if available): ");
        var forceRefreshInput = System.Console.ReadLine()?.ToLower();
        var forceRefresh = forceRefreshInput == "y";

        System.Console.WriteLine();
        System.Console.Write("Processing mode (private/community, default: private): ");
        var modeInput = System.Console.ReadLine()?.ToLower();
        var mode = modeInput == "community" ? "community" : "private";

        System.Console.WriteLine();
        System.Console.WriteLine($"🌐 Step 2: Submitting retrieval request for {url}...");
        if (forceRefresh)
        {
            System.Console.WriteLine("   (Forcing fresh retrieval, bypassing cache)");
        }
        else
        {
            System.Console.WriteLine("   (Will use cached results if available)");
        }
        System.Console.WriteLine($"   Mode: {mode}");
        System.Console.WriteLine();

        var retrieveResponse = await SubmitRetrieveRequestAsync(url, forceRefresh, mode);
        if (retrieveResponse == null)
        {
            System.Console.WriteLine("❌ Failed to submit retrieval request");
            return;
        }

        System.Console.WriteLine($"✅ Request submitted successfully!");
        System.Console.WriteLine($"   Request ID: {retrieveResponse.RequestId}");
        System.Console.WriteLine($"   Status: {retrieveResponse.Status}");
        System.Console.WriteLine($"   Estimated wait: {retrieveResponse.EstimatedWaitTimeSeconds}s");

        System.Console.WriteLine();
        System.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        System.Console.WriteLine();
        System.Console.WriteLine("⏳ Step 3: Polling for results (this may take a while)...");
        System.Console.WriteLine();

        var result = await PollForResultsAsync(retrieveResponse.RequestId);
        if (result == null)
        {
            System.Console.WriteLine("❌ Failed to get results");
            return;
        }

        System.Console.WriteLine($"✅ Retrieval completed!");
        System.Console.WriteLine($"   Status: {result.Status}");
        System.Console.WriteLine($"   URL: {result.Url}");
        System.Console.WriteLine($"   Completed at: {result.CompletedAt}");

        if (result.Content != null)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("📄 Content available:");
            System.Console.WriteLine($"   Page Data URL: {result.Content.PageDataUrl}");
            System.Console.WriteLine($"   Content Hash: {result.Content.ContentHash}");

            if (result.Content.Artifacts != null && result.Content.Artifacts.Count > 0)
            {
                System.Console.WriteLine($"   Artifacts: {string.Join(", ", result.Content.Artifacts.Keys)}");
            }

            if (result.Content.Metrics != null && result.Content.Metrics.Count > 0)
            {
                System.Console.WriteLine($"   Metrics: {string.Join(", ", result.Content.Metrics.Keys)}");
            }

            // Optionally download the page data
            System.Console.WriteLine();
            System.Console.Write("Download page content? (y/n): ");
            var download = System.Console.ReadLine()?.ToLower();
            if (download == "y")
            {
                await DownloadPageDataAsync(result.Content.PageDataUrl);
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        System.Console.WriteLine();
        System.Console.WriteLine("✅ Demo completed successfully!");
    }

    static async Task<List<McpTool>?> GetMcpToolsAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE_URL}/v1/mcp/tools");
            request.Headers.Add("X-API-Key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<McpToolsResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result?.Tools;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error getting tools: {ex.Message}");
            return null;
        }
    }

    static async Task<RetrieveResponse?> SubmitRetrieveRequestAsync(string url, bool forceRefresh = false, string mode = "private")
    {
        try
        {
            var requestBody = new
            {
                url = url,
                timeoutSeconds = 60,
                forceRefresh = forceRefresh,
                mode = mode
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{API_BASE_URL}/v1/mcp/retrieve");
            request.Headers.Add("X-API-Key", _apiKey);
            request.Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"API Error ({response.StatusCode}): {errorContent}");
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<RetrieveResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error submitting retrieval request: {ex.Message}");
            return null;
        }
    }

    static async Task<RetrieveStatusResponse?> PollForResultsAsync(Guid requestId, int maxAttempts = 60)
    {
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{API_BASE_URL}/v1/mcp/retrieve/{requestId}");
                request.Headers.Add("X-API-Key", _apiKey);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RetrieveStatusResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                {
                    System.Console.WriteLine($"   [{attempt}/{maxAttempts}] Failed to deserialize response");
                    await Task.Delay(2000);
                    continue;
                }

                System.Console.WriteLine($"   [{attempt}/{maxAttempts}] Status: {result.Status} - {result.Message}");

                if (result.Status == "complete")
                {
                    return result;
                }

                if (result.Status == "failed" || result.Status == "canceled")
                {
                    System.Console.WriteLine($"   Error: {result.Error}");
                    return result;
                }

                await Task.Delay(2000); // Wait 2 seconds before next poll
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"   [{attempt}/{maxAttempts}] Error: {ex.Message}");
                await Task.Delay(2000);
            }
        }

        System.Console.WriteLine("⚠️  Timeout: Maximum polling attempts reached");
        return null;
    }

    static async Task DownloadPageDataAsync(string pageDataUrl)
    {
        try
        {
            System.Console.WriteLine();
            System.Console.WriteLine("⬇️  Downloading page content...");

            var request = new HttpRequestMessage(HttpMethod.Get, pageDataUrl);
            request.Headers.Add("X-API-Key", _apiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var pageData = JsonSerializer.Deserialize<JsonElement>(content);

            System.Console.WriteLine();
            System.Console.WriteLine("📄 Page Content (first 500 chars):");
            System.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            var prettyJson = JsonSerializer.Serialize(pageData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            if (prettyJson.Length > 500)
            {
                System.Console.WriteLine(prettyJson.Substring(0, 500) + "...");
            }
            else
            {
                System.Console.WriteLine(prettyJson);
            }

            System.Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error downloading page data: {ex.Message}");
        }
    }
}

// DTOs
record McpToolsResponse(List<McpTool> Tools);
record McpTool(string Name, string Description);
record RetrieveResponse(Guid RequestId, string Status, string Message, int EstimatedWaitTimeSeconds);
record RetrieveStatusResponse(
    Guid RequestId,
    string Url,
    string Status,
    string? Message,
    string? Error,
    ContentInfo? Content,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
record ContentInfo(
    string PageDataUrl,
    string? ContentHash,
    Dictionary<string, string>? Artifacts,
    Dictionary<string, object>? Metrics
);
