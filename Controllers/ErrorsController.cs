using CoreWms.Ogc.Wms;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CoreWms.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorsController : ControllerBase
    {
        [Route("error")]
        [Produces("text/xml")]
        public ServiceExceptionReport Error()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = context.Error;
            var code = 200;

            Response.StatusCode = code;

            var serviceExceptionReport = new ServiceExceptionReport();
            var serviceException = new CoreWms.Ogc.Wms.ServiceException();
            serviceException.code = exception.GetType().Name.Replace("Exception", "");
            serviceException.Text = exception.Message;
            serviceExceptionReport.ServiceException.Add(serviceException);
            return serviceExceptionReport;
        }
    }

}