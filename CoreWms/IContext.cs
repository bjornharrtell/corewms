using System.Collections.Generic;

namespace CoreWms;

public interface IContext
{
    IReadOnlyDictionary<string, Layer> Layers { get; }
}
