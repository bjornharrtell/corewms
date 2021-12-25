using System.Collections.Generic;
using CoreWms.Config;

namespace CoreWms;

public interface IContext
{
    IReadOnlyDictionary<string, Layer> Layers { get; }
    IConfig Config { get; }
}
