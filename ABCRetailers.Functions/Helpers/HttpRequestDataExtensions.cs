using Microsoft.Azure.Functions.Worker.Http;
using System.Text;

namespace ABCRetailers.Functions.Helpers;

public static class HttpRequestDataExtensions
{
    public static async Task<string> ReadAsStringAsync(this HttpRequestData request)
    {
        request.Body.Position = 0;
        using var reader = new StreamReader(request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}