using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FlatGeobuf;
using FlatGeobuf.NTS;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace CoreWms.DataSource
{
    public class FlatGeobufSource : IDataSource {
        readonly string filePath;

        public FlatGeobufSource(ILogger logger, Config.DataSource dataSource, Layer layer)
        {
            filePath = Path.Join(dataSource.Path, layer.Name) + ".fgb";
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
            using var stream = File.OpenRead(filePath);
            foreach (var f in FeatureCollectionConversions.Deserialize(stream, e))
                yield return await Task.FromResult(f);
        }

    }
}