
#nullable enable
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpWeatherServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Server initialization - Port changed to 8081
            var server = new McpServer("127.0.0.1", 8081);
            await server.Start();
        }
    }

    // Represents the core MCP server logic
    class McpServer
    {
        private readonly HttpListener _listener;
        private readonly string _url;

        public McpServer(string host, int port)
        {
            _url = $"http://{host}:{port}/";
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
        }

        // Starts the server and listens for incoming connections
        public async Task Start()
        {
            _listener.Start();
            Console.WriteLine($"Server listening on {_url}");

            while (true)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    // Process each request in a background thread
                    _ = Task.Run(() => ProcessRequest(context));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Listener error: {ex.Message}");
                }
            }
        }

        // Processes incoming HTTP requests
        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Ensure the request is a POST to the /mcp endpoint
                if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/mcp")
                {
                    HandleMcpRequest(request, response);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing request: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                response.OutputStream.Close();
            }
        }

        // Handles MCP-specific requests
        private void HandleMcpRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            string requestBody;
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
            {
                requestBody = reader.ReadToEnd();
            }

            var message = JsonSerializer.Deserialize<McpMessage>(requestBody);

            if (message == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            // Route message to appropriate handler based on message type
            switch (message.MessageType)
            {
                case "initialize":
                    HandleInitialize(response);
                    break;
                case "request_context":
                    if (message.Body != null)
                    {
                        HandleRequestContext(response, message.Body);
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    break;
                default:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
            }
        }

        // Handles the 'initialize' message
        private void HandleInitialize(HttpListenerResponse response)
        {
            var capabilities = new
            {
                name = "weather_provider",
                version = "1.0",
                display_name = "Local Weather Provider",
                description = "Provides weather information for a given location.",
                tools = new[]
                {
                    new
                    {
                        name = "get_weather",
                        description = "Get the current weather for a specific location",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                location = new { type = "string", description = "The city and state, e.g. San Francisco, CA" }
                            },
                            required = new[] { "location" }
                        }
                    }
                }
            };

            var responseMessage = new McpMessage
            {
                MessageType = "initialize_response",
                Body = new { capabilities }
            };

            SendMessage(response, responseMessage);
        }

        // Handles the 'request_context' message
        private void HandleRequestContext(HttpListenerResponse response, object body)
        {
            if (body is not JsonElement bodyElement)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            if (!bodyElement.TryGetProperty("tool_call", out var toolCallElement) ||
                !toolCallElement.TryGetProperty("name", out var toolNameElement) ||
                toolNameElement.GetString() != "get_weather" ||
                !toolCallElement.TryGetProperty("parameters", out var parametersElement) ||
                !parametersElement.TryGetProperty("location", out var locationElement) ||
                locationElement.GetString() is not string location)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                return;
            }

            var weatherData = GetWeatherData(location);
            var responseMessage = new McpMessage
            {
                MessageType = "context_response",
                Body = new { content = weatherData }
            };
            SendMessage(response, responseMessage);
        }

        // Simulates fetching weather data
        private string GetWeatherData(string location)
        {
            // In a real application, this would call a weather API
            var temperature = new Random().Next(-10, 35);
            return $"The weather in {location} is {temperature}°C.";
        }

        // Sends an MCP message to the client
        private void SendMessage(HttpListenerResponse response, McpMessage message)
        {
            var jsonResponse = JsonSerializer.Serialize(message, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
            var buffer = Encoding.UTF8.GetBytes(jsonResponse);

            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
        }
    }

    // Represents a generic MCP message
    class McpMessage
    {
        [JsonPropertyName("message_type")]
        public string? MessageType { get; set; }

        [JsonPropertyName("body")]
        public object? Body { get; set; }
    }
}
