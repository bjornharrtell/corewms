using CoreWms.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests
{
    public class CustomWebApplicationFactory
        : WebApplicationFactory<WmsController>
    {
        public CustomWebApplicationFactory()
        {
            Server.AllowSynchronousIO = true;
        }
    }
}