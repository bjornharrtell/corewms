using FlatGeobuf;
using FlatGeobuf.NTS;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace CoreWms.DataSource;

public class FlatGeobufSource : IDataSource
{
    private readonly ILogger logger;
    string filePath = "";

    public FlatGeobufSource(ILogger<FlatGeobufSource> logger)
    {
        this.logger = logger;
    }

    public Envelope GetExtent()
    {
        using var stream = File.OpenRead(filePath);
        var header = Helpers.ReadHeader(stream);
        var e = Helpers.GetEnvelope(header);
        return e;
    }

    public int GetEPSGCode()
    {
        using var stream = File.OpenRead(filePath);
        var header = Helpers.ReadHeader(stream);
        var code = Helpers.GetCrsCode(header);
        return code == 0 ? 6501 : code;
    }

    public async IAsyncEnumerable<IFeature> FetchAsync(Envelope e, double tolerance = 0)
    {
        var i = 0;
        using var stream = File.OpenRead(filePath);
        foreach (var f in FeatureCollectionConversions.Deserialize(stream, e)) {
            yield return await Task.FromResult(f);
            i++;
        }
        logger.LogTrace("Fetched {i} features", i);
    }

    public IDataSource Configure(IContext context, Config.DataSource dataSource, Layer layer)
    {
        filePath = Path.Join(context.Config.DataPath ?? "", dataSource.Path, layer.Name) + ".fgb";
        return this;
    }
}
