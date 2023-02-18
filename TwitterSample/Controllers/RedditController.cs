using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TwitterSample.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    public class RedditController : ControllerBase
    {
        private readonly ILogger<RedditController> _logger;
        private readonly ISampleService<RedditService> _redditService;

        public RedditController(ISampleService<RedditService> redditService, ILogger<RedditController> logger)
        {
            _redditService = redditService;
            _logger = logger;
        }

        /// <summary>
        /// Get a sample of Reddit posts
        /// </summary>
        /// <returns>A JSON array of posts</returns>
        [HttpGet("Sample")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<ActionResult> GetSample()
        {
            var samples = await _redditService.GetSampleAsync();
            if (samples?.Count() < 1)
                return base.NoContent();

            return Content(JsonSerializer.Serialize(samples));
        }

        /// <summary>
        /// Get a URL to a WebSocket stream of samples
        /// </summary>
        /// <returns>A WebSocket URL in plaintext</returns>
        [HttpGet("SampleStream")]
        [ProducesResponseType(typeof(string), 200)]
        public async Task<ActionResult> GetSampleStream()
        {
            return Content(_redditService.GetSampleSocketURL(new Uri(HttpContext.Request.GetEncodedUrl())));
        }


    }
}