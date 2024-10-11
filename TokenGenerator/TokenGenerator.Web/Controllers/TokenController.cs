using System;
using System.Net;
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

            if (_requestCount % 6 != 0)
            {
                await Task.Delay(10000); // simulate some data processing by delaying for 10 seconds
            }

            return Ok(15);
        }
    }


}
