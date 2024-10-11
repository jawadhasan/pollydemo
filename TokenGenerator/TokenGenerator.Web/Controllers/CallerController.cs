using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace TokenGenerator.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CallerController : ControllerBase
    {
        private readonly ILogger<CallerController> _logger;
        private readonly AsyncTimeoutPolicy _timeoutPolicy;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _httpRetryPolicy;

        public CallerController(ILogger<CallerController> logger)
        {
            _logger = logger;

            _httpRetryPolicy =
                Policy.HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                    .Or<TimeoutRejectedException>()
                    .RetryAsync(3, onRetry: OnRetry);

            _timeoutPolicy = Policy.TimeoutAsync(2); //policy will throw TimeoutRejectedException if operation exceeds 2 second   
        }
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var httpClient = GetHttpClient();
            var requestEndpoint = $"token";
            _logger.LogInformation("Making API Request....");

            //HttpResponseMessage response = await httpClient.GetAsync(requestEndpoint);
            //var response = await _httpRetryPolicy.ExecuteAsync(() => httpClient.GetAsync(requestEndpoint));

            var response = await _httpRetryPolicy.ExecuteAsync(() =>
                        _timeoutPolicy.ExecuteAsync(
                            async token => await httpClient.GetAsync(requestEndpoint, token), CancellationToken.None));

            if (response.IsSuccessStatusCode)
            {
                int apiResponse = JsonConvert.DeserializeObject<int>(await response.Content.ReadAsStringAsync());
                return Ok(apiResponse);
            }

            return StatusCode((int)response.StatusCode, response.Content.ReadAsStringAsync());
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
            _logger.LogWarning($"OnRetry --> retryCount = {retryCount}");

            if (delegateResult.Exception is HttpRequestException)
            {
                if (delegateResult.Exception.GetBaseException().Message == "The operation timed out.")
                {
                    //log
                    _logger.LogWarning($"The operation timed out --> retryCount = {retryCount}");
                }
            }
            else if (delegateResult.Exception is TimeoutRejectedException)
            {
                //log
                _logger.LogWarning($"TimeoutRejectedException --> retryCount = {retryCount}");
            }
        }
    }
}
