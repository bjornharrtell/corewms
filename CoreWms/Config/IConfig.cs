using System.Collections.Generic;

namespace CoreWms.Config;

public interface IConfig
{
    IDictionary<string, DataSource> DataSources { get; set; }
    IDictionary<string, Layer> Layers { get; set; }
    string Host { get; set; }
}
