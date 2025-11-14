using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace ABCRetailers.Functions.Functions;
public class QueueProcessorFunctions
{
    [Function("OrderNotifications_Processor")]
    public void OrderNotificationsProcessor(
        [QueueTrigger("%QUEUE_ORDER_NOTIFICATIONS%", Connection = "DefaultEndpointsProtocol=https;AccountName=abcretailerss;AccountKey=GNi356R5w/C76ArDAPE0QBligI0ivkuJzv+VkRUAEMAhKXZr/svB6iwFiBpLOQ727gzPUUKpbz6W+AStyocBLw==;EndpointSuffix=core.windows.net")] string message,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("OrderNotifications_Processor");
        log.LogInformation($"OrderNotifications message: {message}");
    }

    [Function("StockUpdates_Processor")]
    public void StockUpdatesProcessor(
        [QueueTrigger("%QUEUE_STOCK_UPDATES%", Connection = "STORAGE_CONNECTION")] string message,
        FunctionContext ctx)
    {
        var log = ctx.GetLogger("StockUpdates_Processor");
        log.LogInformation($"StockUpdates message: {message}");
    }
}
