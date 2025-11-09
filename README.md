# ViewEngine Console Demo

This console application demonstrates how to use the ViewEngine REST API with an API key.

## What it does

1. **Discovers MCP Tools** - Calls `GET /v1/mcp/tools` to list available endpoints
2. **Submits Retrieve Request** - Calls `POST /v1/mcp/retrieve` to start a web page retrieval job
3. **Polls for Results** - Calls `GET /v1/mcp/retrieve/{requestId}` repeatedly until the job completes
4. **Downloads Content** - Optionally downloads the retrieved page data

## Prerequisites

1. **ViewEngine.API must be running** on `http://localhost:5072`
2. **You need an API key** from the web application:
   - Log in to the web app at http://localhost:5072
   - Go to Settings → API Keys
   - Create a new API key
   - Copy the key (it's only shown once!)

## How to Run

### Option 1: With command-line argument

```bash
cd ViewEngine.Console
dotnet run <your-api-key>
```

### Option 2: Interactive mode

```bash
cd ViewEngine.Console
dotnet run
```

You'll be prompted to enter your API key.

## Example Output

```
╔══════════════════════════════════════════════════════════╗
║          ViewEngine REST API Demo                       ║
║  Demonstrates using the MCP endpoints with an API key   ║
╚══════════════════════════════════════════════════════════╝

Enter your API key: ak_********************************

🔍 Step 1: Discovering available MCP tools...

✅ Found 2 available tools:
   • retrieve_url: Retrieve a web page and extract its content...
   • get_retrieve_status: Check the status of a retrieval job...

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Enter a URL to retrieve (or press Enter for example.com):

🌐 Step 2: Submitting retrieval request for https://example.com...

✅ Request submitted successfully!
   Request ID: 12345678-1234-1234-1234-123456789012
   Status: queued
   Estimated wait: 30s

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⏳ Step 3: Polling for results (this may take a while)...

   [1/60] Status: queued - Retrieval queued. Waiting for available feeder.
   [2/60] Status: processing - Retrieval in progress...
   [3/60] Status: complete - Retrieval completed successfully.

✅ Retrieval completed!
   Status: complete
   URL: https://example.com
   Completed at: 2025-01-15 10:30:45

📄 Content available:
   Page Data URL: https://storage.example.com/...
   Content Hash: abc123...
   Artifacts: screenshot, thumbnail
   Metrics: load_time_ms, dom_size

Download page content? (y/n): y

⬇️  Downloading page content...

📄 Page Content (first 500 chars):
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
{
  "url": "https://example.com",
  "title": "Example Domain",
  "text_content": "This domain is for use in illustrative examples...",
  "html": "<html>...</html>",
  ...
}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ Demo completed successfully!

Press any key to exit...
```

## API Endpoints Used

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/v1/mcp/tools` | GET | List available MCP tools |
| `/v1/mcp/retrieve` | POST | Submit a page retrieval request |
| `/v1/mcp/retrieve/{id}` | GET | Get retrieval status and results |

## Authentication

All requests include the API key in the `X-API-Key` header:

```http
X-API-Key: ak_your-api-key-here
```

## Troubleshooting

### "API key is required"
You forgot to provide an API key. Run with `dotnet run <your-api-key>` or enter it when prompted.

### "Error: Unauthorized"
Your API key is invalid or has been revoked. Create a new one in the web app.

### "API not responding"
Make sure ViewEngine.API is running on http://localhost:5072

### "Timeout: Maximum polling attempts reached"
The retrieval job took longer than expected. This usually means:
- No feeder clients are online to process the job
- The feeder client crashed or couldn't complete the job
- Check the API logs for more details

## Code Structure

The console app is organized into clear methods:

- `Main()` - Entry point, handles API key input
- `RunDemoAsync()` - Orchestrates the demo flow
- `GetMcpToolsAsync()` - Calls GET /v1/mcp/tools
- `SubmitRetrieveRequestAsync()` - Calls POST /v1/mcp/retrieve
- `PollForResultsAsync()` - Repeatedly calls GET /v1/mcp/retrieve/{id}
- `DownloadPageDataAsync()` - Downloads the retrieved content

## Next Steps

This demo shows the MCP endpoints, but ViewEngine also has:

- **Ingest API** (`/v1/ingest/*`) - Submit retrieval jobs programmatically
- **Feeder API** (`/v1/feeders/*`) - For feeder client applications
- **Billing API** (`/v1/billing/*`) - Check earnings and pricing

See the API documentation for more details.
