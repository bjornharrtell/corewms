using CoreWms.WebApp.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CoreWms.IntegrationTests;

public class CustomWebApplicationFactory
    : WebApplicationFactory<WmsController>
{
    public CustomWebApplicationFactory()
    {
        Server.AllowSynchronousIO = true;
    }
}
