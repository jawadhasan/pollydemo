using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace HttpClientWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly AsyncTimeoutPolicy _timeoutPolicy;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;
        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            _httpRetryPolicy =
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .Or<TimeoutRejectedException>()
                    .RetryAsync(3, onRetry: OnRetry);

            _timeoutPolicy = Policy.TimeoutAsync(2); //policy will throw TimeoutRejectedException if operation exceeds 2 second

            #region othercode
            //_httpRetryPolicy =
            //    Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            //        .Or<HttpRequestException>()
            //        .Or<TimeoutRejectedException>()
            //        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(1));
            //

            //_httpRetryPolicy =
            //    Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            //        .Or<HttpRequestException>()
            //        .Or<TimeoutRejectedException>()
            //        .RetryAsync(3, onRetry: OnRetry);


            #endregion


        }




        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            await Task.Delay(1000, stoppingToken);//delay for a bit in start
            _logger.LogInformation("Worker ready at: {time}", DateTimeOffset.Now);

            var httpClient = GetHttpClient();
            var requestEndpoint = $"token";
            _logger.LogInformation("Making API Request....");

            //HTTP Request
            var response = await _httpRetryPolicy.ExecuteAsync(() => httpClient.GetAsync(requestEndpoint));

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync());
                Console.WriteLine($"response received from API {apiResponse}");
            }
            else
            {
                Console.WriteLine($"Not success: {response.StatusCode}");
            }


        }


        private HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(@"http://localhost:16601/api/");
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return httpClient;
        }


        private void OnRetry(DelegateResult<HttpResponseMessage> delegateResult, int retryCount)
        {
            Console.WriteLine($"OnRetry --> retryCount = {retryCount}");

            if (delegateResult.Exception is HttpRequestException)
            {
                if (delegateResult.Exception.GetBaseException().Message == "The operation timed out.")
                {
                    //log
                    Console.WriteLine($"The operation timed out --> retryCount = {retryCount}");
                }
            }
            else if (delegateResult.Exception is TimeoutRejectedException)
            {
                //log
                Console.WriteLine($"TimeoutRejectedException --> retryCount = {retryCount}");
            }
        }
    }
}
