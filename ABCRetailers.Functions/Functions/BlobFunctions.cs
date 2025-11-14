using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace ABCRetailers.Functions.Functions;
public class BlobFunctions
{
    [Function("OnProductImageUploaded")]
    public void OnProductImageUploaded(
        [BlobTrigger("%BLOB_PRODUCT_IMAGES%/{name}", Connection = "DefaultEndpointsProtocol=https;AccountName=abcretailerss;AccountKey=GNi356R5w/C76ArDAPE0QBligI0ivkuJzv+VkRUAEMAhKXZr/svB6iwFiBpLOQ727gzPUUKpbz6W+AStyocBLw==;EndpointSuffix=core.windows.net")] Stream blob,
        string name,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("OnProductImageUploaded");
        log.LogInformation($"Product image uploaded: {name}, size={blob.Length} bytes");
    }
}
