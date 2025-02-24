using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddLogging(); // Add logging services

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Updated `/get-ip` endpoint
app.MapGet("/get-ip", async (HttpContext context, HttpClient httpClient, ILogger<Program> logger) =>
{
    try
    {
        // Extract real client IP from X-Forwarded-For header
        string ipAddress = context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0];

        if (string.IsNullOrEmpty(ipAddress))
        {
            logger.LogWarning("Failed to extract client IP from X-Forwarded-For. Using fallback.");
            ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";
        }

        logger.LogInformation($"Retrieved Client IP address: {ipAddress}");

        // Get Zscaler Data Center info
        var zscalerHostname = await GetZscalerHostname(httpClient, logger);
        var location = GetLocationFromZscalerHostname(zscalerHostname);

        return Results.Ok(new { ipAddress, zscalerHostname, location });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while processing the request.");
        return Results.Problem("Internal server error.");
    }
});

app.Run();

/// <summary>
/// Fetches the Zscaler Data Center hostname by scraping ip.zscaler.com.
/// </summary>
static async Task<string> GetZscalerHostname(HttpClient httpClient, ILogger logger)
{
    try
    {
        // Fetch the Zscaler IP lookup page
        var pageContent = await httpClient.GetStringAsync("https://ip.zscaler.com/");

        // Load the HTML document
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(pageContent);

        // Extract the Zscaler hostname using a regular expression
        string pattern = @"The Zscaler hostname for this proxy appears to be <span class=""detailOutput"">([^<]+)</span>";
        Match match = Regex.Match(pageContent, pattern);
        if (match.Success)
        {
            string dataCenter = match.Groups[1].Value.Trim();
            logger.LogInformation($"Zscaler Data Center found: {dataCenter}");
            return dataCenter;
        }

        logger.LogWarning("Could not determine the Zscaler Data Center.");
        return "Unknown";
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while fetching the Zscaler Data Center.");
        return "Error";
    }
}

/// <summary>
/// Maps the Zscaler hostname to a location.
/// </summary>
static string GetLocationFromZscalerHostname(string zscalerHostname)
{
    // Define a dictionary to map Zscaler hostnames to locations
    Dictionary<string, string> locationMap = new Dictionary<string, string>
    {
        { "dxb", "Dubai, UAE" },
        { "nyc", "New York, USA" },
        { "lon", "London, UK" },
    };

    // Extract the second part of the hostname and ignore any numbers
    string[] parts = zscalerHostname.Split('-');
    if (parts.Length > 1)
    {
        string key = Regex.Replace(parts[1], @"\d", ""); // Remove any digits
        if (locationMap.ContainsKey(key))
        {
            return locationMap[key];
        }
    }
    return "Unknown location";
}