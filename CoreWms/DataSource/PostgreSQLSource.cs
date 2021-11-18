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
    string? table;
    string[]?extraColumns;

    string? geom;
    Type? geometryType;
    readonly IDictionary<string, NpgsqlDbType> extraColumnsSet = new Dictionary<string, NpgsqlDbType>();

    public PostgreSQLSource(ILogger<PostgreSQLSource> logger)
    {
        this.logger = logger;
    }

    public Envelope GetExtent()
    {
        var sql = $"select st_setsrid(st_extent({geom}), 0) from {table}";
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        var polygon = conn.QueryFirst<Polygon>(sql);
        if (polygon == null)
            throw new Exception("Unexpected failure determining layer extent");
        return polygon.EnvelopeInternal;
    }

    public int GetEPSGCode()
    {
        var sql = $"select st_srid({geom}) from {table} limit 1";
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        var srid = conn.QueryFirstOrDefault<int?>(sql);
        return srid ?? 6501;
    }

    IEnumerable<NpgsqlDbColumn> GetColumnsMeta()
    {
        var sql = $"select * from {table} limit 1";
        logger.LogTrace("SQL: {sql}", sql);
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();
        var columns = reader.GetColumnSchema();
        return columns;
    }

    public async IAsyncEnumerable<IFeature> FetchAsync(Envelope e, double tolerance = 0)
    {
        var columnsList = new List<string>();
        var geomColumn = tolerance > 0 ? $"st_snaptogrid({geom}, {tolerance}, {tolerance}) geom" : $"{geom} geom";
        columnsList.Add(geomColumn);
        if (extraColumns != null)
            columnsList.AddRange(extraColumns);
        var columns = string.Join(", ", columnsList);
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        var whereClauses = new List<string>()
            {
                $"st_intersects(st_setsrid({geom}, 0), st_makeenvelope({e.MinX}, {e.MinY}, {e.MaxX}, {e.MaxY}))"
            };
        if (typeof(IPolygonal).IsAssignableFrom(geometryType))
            whereClauses.Add($"st_area({geom}) > {tolerance}");
        else if (typeof(ILineal).IsAssignableFrom(geometryType))
            whereClauses.Add($"st_length({geom}) > {tolerance}");
        var where = string.Join(" and ", whereClauses);
        var select = $"select {columns} from {table} where {where}";
        var sql = $"copy ({select}) to stdout (format binary)";
        logger.LogTrace("SQL: {sql}", sql);
        using var reader = conn.BeginBinaryExport(sql);
        while (reader.StartRow() != -1)
            yield return await ReadFeature(reader);
    }

    private async Task<IFeature> ReadFeature(NpgsqlBinaryExporter reader)
    {
        var geometry = pgreader.Read(await reader.ReadAsync<byte[]>());
        if (extraColumnsSet.Count > 0)
            return new Feature(geometry, await ReadAttributes(reader));
        else
            return new Feature(geometry, null);
    }

    private async Task<AttributesTable> ReadAttributes(NpgsqlBinaryExporter reader)
    {
        var attributes = new AttributesTable();
        foreach (var column in extraColumnsSet)
            if (!reader.IsNull)
                attributes.Add(column.Key, await ReadAttribute(reader, column.Value));
        return attributes;
    }

    private static async Task<object> ReadAttribute(NpgsqlBinaryExporter reader, NpgsqlDbType npgsqlDbType)
    {
        return npgsqlDbType switch
        {
            NpgsqlDbType.Smallint => await reader.ReadAsync<short>(),
            NpgsqlDbType.Integer => await reader.ReadAsync<int>(),
            NpgsqlDbType.Bigint => await reader.ReadAsync<long>(),
            NpgsqlDbType.Text => await reader.ReadAsync<string>(),
            NpgsqlDbType.Varchar => await reader.ReadAsync<string>(),
            NpgsqlDbType.Date => await reader.ReadAsync<DateTimeOffset>(),
            NpgsqlDbType.Timestamp => await reader.ReadAsync<DateTimeOffset>(),
            NpgsqlDbType.TimestampTz => await reader.ReadAsync<DateTimeOffset>(),
            _ => throw new Exception("Unknown datatype {npgsqlDbType}"),
        };
    }

    public IDataSource Configure(Config.DataSource dataSource, Layer layer)
    {
        connectionString = dataSource.ConnectionString;
        table = dataSource.Schema + "." + layer.Name;
        if (layer.Rules != null)
            extraColumns = new HashSet<string>(layer.Rules.Select(r => r.Filters.FirstOrDefault().PropertyName).Where(pn => !string.IsNullOrEmpty(pn))).ToArray();
        else
            extraColumns = Array.Empty<string>();

        var columnsMeta = GetColumnsMeta();
        var geometryColumn = columnsMeta.FirstOrDefault(c => c.NpgsqlDbType == NpgsqlDbType.Geometry);
        if (geometryColumn == null)
            throw new Exception($"Cannot find a geometry column for table {table}");
        geom = geometryColumn.ColumnName;
        geometryType = layer.GeometryType;
        foreach (var column in extraColumns)
        {
            var columnMeta = columnsMeta.FirstOrDefault(c => c.ColumnName == column);
            if (columnMeta == null)
                throw new Exception($"Cannot find column {column} in {table}");
            if (columnMeta.NpgsqlDbType == null)
                throw new Exception($"Unknown column type for {column} in {table}");
            extraColumnsSet.Add(column, columnMeta.NpgsqlDbType.Value);
        }
        return this;
    }
}
