using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FlatBuffers;
using FlatGeobuf;
using FlatGeobuf.NTS;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace CoreWms.DataSource
{
    public class FlatGeobufSource : IDataSource {
        readonly string filePath;

        public static byte[] MagicBytes = new byte[] {
            0x66, 0x67, 0x62, 0x03, 0x66, 0x67, 0x62, 0x00 };

        public FlatGeobufSource(ILogger logger, Config.DataSource dataSource, Layer layer)
        {
            filePath = Path.Join(dataSource.Path, layer.Name) + ".fgb";
        }

        public Envelope GetExtent()
        {
            using var stream = File.OpenRead(filePath);
            // TODO: would be nice if it was part of FlatGeobuf API
            var reader = new BinaryReader(stream);
            var magicBytes = reader.ReadBytes(8);
            if (!magicBytes.SequenceEqual(Constants.MagicBytes))
                throw new Exception("Not a FlatGeobuf file");
            var headerSize = reader.ReadInt32();
            var header = Header.GetRootAsHeader(new ByteBuffer(reader.ReadBytes(headerSize)));
            var e = new Envelope(header.Envelope(0), header.Envelope(2), header.Envelope(1), header.Envelope(3));
            return e;
        }

        public int GetEPSGCode()
        {
            using var stream = File.OpenRead(filePath);
            // TODO: would be nice if it was part of FlatGeobuf API
            var reader = new BinaryReader(stream);
            var magicBytes = reader.ReadBytes(8);
            if (!magicBytes.SequenceEqual(Constants.MagicBytes))
                throw new Exception("Not a FlatGeobuf file");
            var headerSize = reader.ReadInt32();
            var header = Header.GetRootAsHeader(new ByteBuffer(reader.ReadBytes(headerSize)));
            return header.Crs?.Code ?? 6501;
        }


        public async IAsyncEnumerable<IFeature> FetchAsync(Envelope e, double tolerance = 0)
        {
            using var stream = File.OpenRead(filePath);
            foreach (var f in FeatureCollectionConversions.Deserialize(stream, e))
                yield return await Task.FromResult(f);
        }

    }
}