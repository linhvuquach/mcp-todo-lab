using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using System.Net.Http.Headers;

// Create a new host application builder. This is the standard way to configure and create a .NET host.
var builder = Host.CreateEmptyApplicationBuilder(settings: null);

// Add the MCP server to the services collection. This extension method is the entry point for the MCP SDK.
builder.Services.AddMcpServer()
    // Use standard I/O for server transport. This means the server will communicate over stdin and stdout,
    // which is a common way to connect to a host/client in a local development environment.
    .WithStdioServerTransport()
    // Discover and register all tool methods in the current assembly. The SDK will scan for classes and methods
    // decorated with [McpServerToolType] and [McpServerTool] attributes.
    .WithToolsFromAssembly();

// Register a singleton HttpClient. This ensures that a single instance of HttpClient is used for all HTTP requests,
// which is the recommended practice for performance and resource management.
builder.Services.AddSingleton(_ =>
{
    // Initialize the HttpClient with the base address of the weather API.
    var client = new HttpClient()
    {
        BaseAddress = new Uri("https://api.weather.gov")
    };
    // Set a User-Agent header. This is a good practice and sometimes required by APIs.
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
    return client;
});

// Build the application host.
var app = builder.Build();
// Run the application asynchronously. This will start the MCP server and block until the application is shut down.
await app.RunAsync();