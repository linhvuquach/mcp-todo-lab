
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Net.Http.Json;

namespace QuickstartWeatherServer.Tools;

// This attribute marks the class as a container for MCP server tools.
[McpServerToolType]
public static class WeatherTools
{
    // This attribute identifies the method as an MCP server tool and provides a description for the client.
    [McpServerTool, Description("Get weather alerts for a US state.")]
    public static async Task<string> GetAlerts(
        HttpClient client,
        [Description("The US state to get alerts for.")] string state)
    {
        // Fetch active alerts for the specified state from the weather API.
        var alerts = await client.GetFromJsonAsync<JsonElement>($"/alerts/active?area={state}");
        // Return the alerts as a formatted JSON string.
        return JsonSerializer.Serialize(alerts, new JsonSerializerOptions { WriteIndented = true });
    }

    // This attribute identifies the method as an MCP server tool and provides a description for the client.
    [McpServerTool, Description("Get weather forecast for a location.")]
    public static async Task<string> GetForecast(
        HttpClient client,
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude)
    {
        // First, get the forecast grid data for the given latitude and longitude.
        var points = await client.GetFromJsonAsync<JsonElement>($"/points/{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}");
        // Extract the forecast URL from the grid data.
        var forecastUrl = points.GetProperty("properties").GetProperty("forecast").GetString();
        // Fetch the forecast data from the extracted URL.
        var forecast = await client.GetFromJsonAsync<JsonElement>(forecastUrl);
        // Return the forecast as a formatted JSON string.
        return JsonSerializer.Serialize(forecast, new JsonSerializerOptions { WriteIndented = true });
    }
}
