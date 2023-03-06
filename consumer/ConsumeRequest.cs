using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace GBelenky.Consumer
{
    public class ConsumeRequest
    {
        private readonly ILogger _logger;

        public ConsumeRequest(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ConsumeRequest>();
        }

        [Function("ConsumeRequest")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "processRequest/{requestId}")] HttpRequestData req, string requestId)
        {
            string result = $"processed request with id: {requestId}"; 
            _logger.LogInformation(result);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(result);

            return response;
        }
    }
}
