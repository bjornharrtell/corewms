using CoreWms.Ogc.Wms;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CoreWms.WebApp.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorsController : ControllerBase
{
    [Route("error")]
    [Produces("text/xml")]
    public ServiceExceptionReport Error()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = context?.Error;
        if (exception == null)
            exception = new System.Exception("Could not resolve IExceptionHandlerFeature");
        var code = 200;

        Response.StatusCode = code;

        var serviceExceptionReport = new ServiceExceptionReport();
        var serviceException = new Ogc.Wms.ServiceException
        {
            code = exception.GetType().Name.Replace("Exception", ""),
            Text = exception.Message
        };
        serviceExceptionReport.ServiceException.Add(serviceException);
        return serviceExceptionReport;
    }
}
