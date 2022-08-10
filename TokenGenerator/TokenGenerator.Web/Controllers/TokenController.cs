using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace TokenGenerator.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TokenController : ControllerBase
    {
        static int _requestCount = 0;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _requestCount++;

            //cause delay on first 5 requests, 6th request without delay
            if (_requestCount % 6 != 0)
            {
                await Task.Delay(10000); //simulate work-10sec
            }

            return Ok(15);
        }
    }
}
