#nullable disable

namespace CoreWms.Config;

public class Config : IConfig
{
    public IDictionary<string, DataSource> DataSources { get; set; }
    public IDictionary<string, Layer> Layers { get; set; }
    public string Host { get; set; }
    public string DataPath { get; set; }
}
