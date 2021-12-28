using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Npgsql;
using Npgsql.Schema;
using NpgsqlTypes;
using Dapper;

namespace CoreWms.DataSource;

public class PostgreSQLSource : IDataSource
{
    readonly ILogger logger;
    readonly PostGisReader pgreader = new();

    string? connectionString;
    string? schema;
    string? table;
    Envelope? envelope;

    string[]?extraColumns;

    string? geom;
    int? srid;
    Type? geometryType;
    readonly IDictionary<string, NpgsqlDbType> extraColumnsSet = new Dictionary<string, NpgsqlDbType>();

    public PostgreSQLSource(ILogger<PostgreSQLSource> logger)
    {
        this.logger = logger;
    }

    public Envelope GetExtent()
    {
        logger.LogTrace("GetExtent called");
        if (envelope != null)
        {
            logger.LogTrace("Returning cached envelope {}", this.envelope);
            return envelope;
        }
        var srid = GetEPSGCode();
        var sql = $"select st_setsrid(st_estimatedextent('{schema}', '{table}', '{geom}'), {srid})";
        logger.LogTrace("SQL: {sql}", sql);
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        var polygon = conn.ExecuteScalar<Polygon>(sql);
        if (polygon == null)
            throw new Exception("Unexpected failure determining layer extent");
        logger.LogTrace("polygon.EnvelopeInternal: {EnvelopeInternal}", polygon.EnvelopeInternal);
        return polygon.EnvelopeInternal;
    }

    public int GetEPSGCode()
    {
        if (this.srid.HasValue)
        {
            logger.LogTrace("Returning cached srid {}", this.srid);
            return this.srid.Value;
        }
        var sql = $"select st_srid({geom}) from {schema}.{table} limit 1";
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        var srid = conn.QueryFirstOrDefault<int?>(sql);
        this.srid = srid ?? 0;
        return this.srid.Value;
    }

    string GetGeometryName()
    {
        var sql = $"select * from {schema}.{table} limit 1";
        logger.LogTrace("SQL: {sql}", sql);
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var columnsSchema = reader.GetColumnSchema();
        var geometryColumn = columnsSchema.FirstOrDefault(c => c.NpgsqlDbType == NpgsqlDbType.Geometry);
        if (geometryColumn == null)
            throw new Exception($"Cannot find a geometry column for table {schema}.{table}");
        return geometryColumn.ColumnName;
    }

    IEnumerable<NpgsqlDbColumn> GetColumnsMeta()
    {
        var columns = GenerateSelect(geom ?? "");
        var sql = $"select {columns} from {schema}.{table} limit 1";
        logger.LogTrace("SQL: {sql}", sql);
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var columnsSchema = reader.GetColumnSchema();
        return columnsSchema;
    }

    private string GenerateSelect(string geomColumn) {
        var columnsList = new List<string>() { geomColumn };
        if (extraColumns != null)
            columnsList.AddRange(extraColumns);
        var columns = string.Join(", ", columnsList);
        return columns;
    }

    public async IAsyncEnumerable<IFeature> FetchAsync(Envelope e, double tolerance = 0)
    {
        var srid = GetEPSGCode();
        var geomColumn = tolerance > 0 ? $"st_snaptogrid(st_force2d({geom}), {tolerance}, {tolerance}) geom" : $"st_force2d({geom}) geom";
        var columns = GenerateSelect(geomColumn);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        var whereGeomFilter = $"st_intersects({geom}, st_makeenvelope({e.MinX}, {e.MinY}, {e.MaxX}, {e.MaxY}, {srid}))";
        var whereClauses = new List<string>() { whereGeomFilter };
        if (typeof(IPolygonal).IsAssignableFrom(geometryType))
            whereClauses.Add($"st_area({geom}) > {tolerance}");
        else if (typeof(ILineal).IsAssignableFrom(geometryType))
            whereClauses.Add($"st_length({geom}) > {tolerance}");
        var where = string.Join(" and ", whereClauses);
        var select = $"select {columns} from {schema}.{table} where {where}";
        var sql = $"copy ({select}) to stdout (format binary)";
        logger.LogTrace("SQL: {sql}", sql);
        using var reader = conn.BeginBinaryExport(sql);
        while (reader.StartRow() != -1)
            yield return ReadFeature(reader);
    }

    private IFeature ReadFeature(NpgsqlBinaryExporter reader)
    {
        var geometry = pgreader.Read(reader.Read<byte[]>());
        if (extraColumnsSet.Count > 0)
            return new Feature(geometry, ReadAttributes(reader));
        else
            return new Feature(geometry, null);
    }

    private AttributesTable ReadAttributes(NpgsqlBinaryExporter reader)
    {
        var attributes = new AttributesTable();
        foreach (var column in extraColumnsSet)
            if (!reader.IsNull)
                attributes.Add(column.Key, ReadAttribute(reader, column.Value));
        return attributes;
    }

    private static object ReadAttribute(NpgsqlBinaryExporter reader, NpgsqlDbType npgsqlDbType)
    {
        return npgsqlDbType switch
        {
            NpgsqlDbType.Smallint => reader.Read<short>(),
            NpgsqlDbType.Integer => reader.Read<int>(),
            NpgsqlDbType.Bigint => reader.Read<long>(),
            NpgsqlDbType.Text => reader.Read<string>(),
            NpgsqlDbType.Varchar => reader.Read<string>(),
            NpgsqlDbType.Date => reader.Read<DateTimeOffset>(),
            NpgsqlDbType.Timestamp => reader.Read<DateTimeOffset>(),
            NpgsqlDbType.TimestampTz => reader.Read<DateTimeOffset>(),
            _ => throw new Exception($"Unknown datatype {npgsqlDbType}"),
        };
    }

    public IDataSource Configure(IContext context, Config.DataSource dataSource, Layer layer)
    {
        logger.LogTrace("Configuring {Name}", layer.Name);
        connectionString = dataSource.ConnectionString;
        schema = dataSource.Schema;
        table = layer.Table ?? layer.Name;
        if (layer.Extent != null)
            envelope = new Envelope(layer.Extent[0], layer.Extent[2], layer.Extent[1], layer.Extent[3]);
        if (layer.Rules != null)
            extraColumns = new HashSet<string>(layer.Rules.SelectMany(r => r.Filter?.GetRequiredPropertyNames() ?? Array.Empty<string>())).ToArray();
        else
            extraColumns = Array.Empty<string>();

        logger.LogTrace("Getting column metadata for {Name}", layer.Name);
        geom = GetGeometryName();
        var columnsMeta = GetColumnsMeta();
        geometryType = layer.GeometryType;
        logger.LogTrace("Found geometry column with name {geom} and type {geometryType}", geom, geometryType);
        foreach (var column in extraColumns)
        {
            var columnMeta = columnsMeta.FirstOrDefault(c => c.ColumnName == column);
            if (columnMeta == null)
                throw new Exception($"Cannot find column {column} in {table}");
            if (columnMeta.NpgsqlDbType == null)
                throw new Exception($"Unknown column type for {column} in {table}");
            extraColumnsSet.Add(column, columnMeta.NpgsqlDbType.Value);
        }
        logger.LogTrace("Found {Length} other columns", extraColumns.Length);
        return this;
    }
}
