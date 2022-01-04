using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Xml.Serialization;
using CoreWms.Ogc.Wms;
using System.IO;
using System.Threading;

namespace CoreWms;

    public class Function
    {
        readonly GetMap getMap;
        readonly GetCapabilities getCapabilities;

        public Function(GetMap getMap, GetCapabilities getCapabilities)
        {
            this.getCapabilities = getCapabilities;
            this.getMap = getMap;
        }

        [FunctionName("CoreWms")]
        public async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "wms")] HttpRequest req,
            ILogger logger, CancellationToken cancellationToken)
        {
            logger.LogTrace("Recieved GET request");
            var response = req.HttpContext.Response;

            try {
                var service = req.Query["service"].FirstOrDefault();
                var request = req.Query["request"].FirstOrDefault();
                var layers = req.Query["layers"].FirstOrDefault();
                var styles = req.Query["styles"].FirstOrDefault();
                var crs = req.Query["crs"].FirstOrDefault();
                var version = req.Query["version"].FirstOrDefault();
                var bbox = req.Query["bbox"].FirstOrDefault();
                var width = req.Query["width"].FirstOrDefault();
                var height = req.Query["height"].FirstOrDefault();
                var format = req.Query["format"].FirstOrDefault();
                var transparent = req.Query["transparent"].FirstOrDefault();

                if (request == "GetCapabilities")
                {
                    response.StatusCode = 200;
                    response.ContentType = "text/xml";
                    await getCapabilities.StreamResponseAsync(response.Body);
                    await response.Body.FlushAsync(cancellationToken);
                }
                else if (request == "GetMap")
                {
                    layers ??= "";
                    styles ??= "";
                    crs ??= "";
                    version ??= "1.3.0";
                    if (bbox == null)
                        throw new Exception("Query string parameter bbox is required");
                    if (width == null)
                        throw new Exception("Query string parameter width is required");
                    var intWidth = int.Parse(width);
                    if (height == null)
                        throw new Exception("Query string parameter height is required");
                    var intHeight = int.Parse(height);
                    format ??= "image/png";
                    transparent ??= "true";
                    var boolTransparent = bool.Parse(transparent);
                    response.StatusCode = 200;
                    response.ContentType = "image/png";
                    var parameters = getMap.ParseQueryStringParams(service, version, request, layers, styles, crs, bbox, intWidth, intHeight, format, boolTransparent);
                    await getMap.StreamResponseAsync(parameters, response.Body, cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
                else
                {
                    throw new Exception($"Unknown request {request}");
                }
            }
            catch (Exception e)
            {
                var serviceExceptionReport = new ServiceExceptionReport();
                var serviceException = new Ogc.Wms.ServiceException
                {
                    code = e.GetType().Name.Replace("Exception", ""),
                    Text = e.Message
                };
                serviceExceptionReport.ServiceException.Add(serviceException);
                var serializer = new XmlSerializer(typeof(ServiceExceptionReport));
                using var memoryStream = new MemoryStream();
                serializer.Serialize(memoryStream, serviceExceptionReport);
                memoryStream.Seek(0, SeekOrigin.Begin);
                response.StatusCode = 200;
                response.ContentType = "text/xml";
                await memoryStream.CopyToAsync(response.Body, cancellationToken);
                await memoryStream.FlushAsync(cancellationToken);
                await memoryStream.DisposeAsync();
            }
        }
    }

