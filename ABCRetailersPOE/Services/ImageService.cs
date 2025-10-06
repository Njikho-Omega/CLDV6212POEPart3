using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;

namespace ABCRetailersPOE.Services;

public class ImageService
{
    private readonly string _connectionString;
    private readonly string _containerName;

    public ImageService(IConfiguration configuration)
    {
        _connectionString = configuration["AzureStorage:ConnectionString"] ??
                           "UseDevelopmentStorage=true";
        _containerName = "product-images";
    }

    public async Task<string> UploadProductImageAsync(IFormFile imageFile, string productId)
    {
        try
        {
            var container = new BlobContainerClient(_connectionString, _containerName);
            await container.CreateIfNotExistsAsync();

            var blobName = $"{productId}/{Guid.NewGuid():N}{Path.GetExtension(imageFile.FileName)}";
            var blob = container.GetBlobClient(blobName);

            await using var stream = imageFile.OpenReadStream();
            await blob.UploadAsync(stream, overwrite: true);

            return blob.Uri.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ImageService upload error: {ex}");
            throw;
        }
    }
}