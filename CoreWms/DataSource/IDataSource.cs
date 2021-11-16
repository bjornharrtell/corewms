using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace CoreWms.DataSource;

public interface IDataSource
{
    Envelope GetExtent();
    int GetEPSGCode();
    IAsyncEnumerable<IFeature> FetchAsync(Envelope e, double tolerance = 0);
}
