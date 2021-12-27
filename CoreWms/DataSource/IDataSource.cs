using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace CoreWms.DataSource;

public interface IDataSource
{
    IDataSource Configure(IContext context, Config.DataSource dataSource, Layer layer);
    Envelope GetExtent();
    int GetEPSGCode();
    IAsyncEnumerable<IFeature> FetchAsync(Envelope e, double tolerance = 0);
}
