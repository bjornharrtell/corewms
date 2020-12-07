using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace CoreWms.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WmsController : ControllerBase
    {
        private readonly ILogger<WmsController> logger;
        private readonly GetMap getMap;
        private readonly GetCapabilities getCapabilities;

        public WmsController(ILogger<WmsController> logger, GetMap getMap, GetCapabilities getCapabilities)
        {
            this.logger = logger;
            this.getMap = getMap;
            this.getCapabilities = getCapabilities;
        }

        [HttpGet]
        public async Task Get(string service, string version, string request, string layers, string styles, string crs, string bbox, int width, int height, string format, bool? transparent)
        {
            logger.LogTrace("Recieved GET request");
            if (request == "GetCapabilities")
            {
                Response.StatusCode = 200;
                Response.Headers.Add(HeaderNames.ContentType, "text/xml");
                await getCapabilities.StreamResponseAsync(Response.Body);
                await Response.Body.FlushAsync();
            }
            else if (request == "GetMap")
            {
                layers ??= "";
                styles ??= "";
                crs ??= "";
                Response.StatusCode = 200;
                Response.Headers.Add(HeaderNames.ContentType, "image/png");
                getMap.Parse(service, version, request, layers, styles, crs, bbox, width, height, format, transparent.GetValueOrDefault());
                await getMap.StreamResponseAsync(Response.Body);
                await Response.Body.FlushAsync();
            }
            else
            {
                throw new Exception($"Unknown request {request}");
            }
        }
    }
}
