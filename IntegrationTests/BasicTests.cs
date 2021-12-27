using System.Reflection;
using Codeuctivity;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CoreWms.IntegrationTests;

public class BasicTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    private readonly CustomWebApplicationFactory _factory;

    public BasicTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Test1()
    {
        var client = _factory.CreateClient();
        var urlPath = "/wms?service=WMS&request=GetMap&layers=countries&styles=&crs=&bbox=-180,-90,180,90&width=500&height=500&format=image/png";
        var response = await client.GetAsync(urlPath);
        response.EnsureSuccessStatusCode();
        var actual = await Image.LoadAsync<Rgba32>(await response.Content.ReadAsStreamAsync());
        var expected = await Image.LoadAsync<Rgba32>(path + "/../../../Expected/countries.png");
        Assert.True(ImageSharpCompare.ImageAreEqual(actual, expected));
    }
}
