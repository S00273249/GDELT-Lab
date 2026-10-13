
using System.Text.Json;
using GDELT_Lab.Models;

namespace GDELT_Lab.Services;

/// <summary>
/// Sends search requests to the GDELT DOC API and deserialises
/// the returned article data.
/// </summary>
public class GdeltSearchService
{
    private readonly HttpClient _httpClient;

    public GdeltSearchService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        // Identify the application when making requests to GDELT.
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "GDELT-Lab/1.0 (student research prototype)");
    }

    /// <summary>
    /// Searches GDELT for articles matching the supplied query.
    /// </summary>
    /// <param name="query">Search terms or expression submitted to GDELT.</param>
    /// <param name="cancellationToken">Token used to cancel the HTTP request.</param>
    /// <returns>The deserialised search response containing matching articles.</returns>
    public async Task<GdeltSearchResponse> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        // Construct the DOC API request URL.
        // Escape the query so special characters are safe within the URL.
        var url =
            "https://api.gdeltproject.org/api/v2/doc/doc" +
            "?query=" + Uri.EscapeDataString(query) +
            "&mode=ArtList" +
            "&format=json" +
            "&maxrecords=10" +
            "&sort=DateDesc";

        // Send the request and ensure the response is disposed of afterwards.
        using var response = await _httpClient.GetAsync(
            url,
            cancellationToken);

        // Read the response body so it can be deserialised or included
        // in diagnostic information if the request fails.
        var content = await response.Content.ReadAsStringAsync(
            cancellationToken);

        // Report unsuccessful HTTP responses with useful diagnostic details.
        if (!response.IsSuccessStatusCode)
        {
            var retryAfter = response.Headers.RetryAfter?.ToString()
                ?? "Not provided";

            // Limit the error body to avoid producing excessively long messages.
            var errorBody = content.Length > 1000
                ? content[..1000]
                : content;

            throw new HttpRequestException(
                $"GDELT returned HTTP {(int)response.StatusCode}. " +
                $"Retry-After: {retryAfter}. Response: {errorBody}",
                null,
                response.StatusCode);
        }

        // Convert the JSON response into the application's response model.
        // Throw an exception if deserialisation produces no response object.
        return JsonSerializer.Deserialize<GdeltSearchResponse>(content)
            ?? throw new InvalidOperationException(
                "GDELT returned an empty or invalid JSON response.");
    }
}
