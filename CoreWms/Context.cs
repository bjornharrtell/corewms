using System.Collections.ObjectModel;
using CoreWms.Config;
using CoreWms.DataSource;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.DependencyInjection;

namespace CoreWms;

public class Context : IContext
{
    readonly ILogger<Context> logger;
    readonly IConfig config;
    readonly IServiceProvider serviceProvider;

    public IReadOnlyDictionary<string, Layer> Layers { get; }

    public Context(ILogger<Context> logger, IConfig config, IServiceProvider serviceProvider)
    {
        this.logger = logger;
        this.config = config;
        this.serviceProvider = serviceProvider;
        logger.LogInformation("Initializing CoreWms");
        Layers = new ReadOnlyDictionary<string, Layer>(CreateLayers());
    }

    public IDictionary<string, Layer> CreateLayers()
    {
        return config.Layers
            .Select(kv => CreateLayer(kv.Key, kv.Value))
            .ToDictionary(l => l.Name);
    }

    Rule[]? CreateRules(string name)
    {
        var filePath = Path.Join("sld", name) + ".sld";
        try
        {
            var stream = new FileStream(filePath, FileMode.Open);
            var sld = SldHelpers.FromStream(stream);
            var rules = SldHelpers.ToCoreWmsRules(sld);
            return rules;
        }
        catch (Exception e) when (e is FileNotFoundException || e is DirectoryNotFoundException)
        {
            logger.LogWarning("Style for layer {name} not found falling back to default", name);
            return Array.Empty<Rule>();
        }
    }

    IDataSource CreateDataSource(Config.Layer configLayer, Layer layer)
    {
        var dataSourceName = configLayer.DataSource ?? "Default";
        if (!config.DataSources.TryGetValue(dataSourceName, out var dataSource))
            throw new Exception($"Could not find {dataSourceName} data source");

        if (dataSource.Type == "FlatGeobuf")
            return serviceProvider.GetRequiredService<FlatGeobufSource>()
                .Configure(dataSource, layer);
        else if (dataSource.Type == "PostgreSQL")
            return serviceProvider.GetRequiredService<PostgreSQLSource>()
                .Configure(dataSource, layer);
        else
            throw new Exception($"Unknown datasource type {dataSource.Type}");
    }

    Layer CreateLayer(string name, Config.Layer configLayer)
    {
        var geometryType = typeof(Geometry);
        if (configLayer.GeometryType == "Point")
            geometryType = typeof(LineString);
        else if (configLayer.GeometryType == "LineString")
            geometryType = typeof(LineString);
        else if (configLayer.GeometryType == "Polygon")
            geometryType = typeof(Polygon);
        else if (configLayer.GeometryType == "MultiPoint")
            geometryType = typeof(MultiPoint);
        else if (configLayer.GeometryType == "MultiLineString")
            geometryType = typeof(MultiLineString);
        else if (configLayer.GeometryType == "MultiPolygon")
            geometryType = typeof(MultiPolygon);

        var layer = new Layer()
        {
            Name = name,
            Title = configLayer.Title,
            Table = configLayer.Table,
            Extent = configLayer.Extent,
            Rules = CreateRules(name),
            GeometryType = geometryType
        };
        layer.DataSource = CreateDataSource(configLayer, layer);
        return layer;
    }
}
