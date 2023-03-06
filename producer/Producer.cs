using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace GBelenky.Producer
{

    public static class Producer
    {
        private static HttpClient httpClient = new HttpClient();

        [Function(nameof(ProducerOrchestrator))]
        public static async Task ProducerOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger logger = context.CreateReplaySafeLogger(nameof(Producer));
            
            int rps = Int32.Parse(Environment.GetEnvironmentVariable("RPS") ?? "1");
            int delay = 1000 / rps;

            logger.LogInformation($"Orchestrator will broadcast {rps} requests per second woth the delay of {delay} millicesonds.");

            var tasks = new Task[rps];

            for (int i = 0; i < rps; i++)
            {
                tasks[i] = context.CallActivityAsync("BroadcastRequest", i.ToString());
                DateTime nextStart = context.CurrentUtcDateTime.Add(TimeSpan.FromMilliseconds(delay));
                await context.CreateTimer(nextStart, CancellationToken.None);
            }
            await Task.WhenAll(tasks);
            return;
        }

        [Function(nameof(BroadcastRequest))]
        public static async Task<string> BroadcastRequest([ActivityTrigger] string requestId, FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("BroadcastRequest");
            logger.LogInformation($"Broadcasting request: {requestId}.");

            string consumerUrl = Environment.GetEnvironmentVariable("CONSUMER_URL");
            string urlString = $"{consumerUrl}/{requestId}";
            string consumerResultStr = await httpClient.GetStringAsync(urlString);
            return consumerResultStr;
        }

        [Function("ProducerClient")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("ProducerClient");

            // Function input comes from the request content.
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(ProducerOrchestrator));

            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            // Returns an HTTP 202 response with an instance management payload.
            // See https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-http-api#start-orchestration
            return client.CreateCheckStatusResponse(req, instanceId);
        }
    }
}
