#nullable disable
namespace CoreWms.Config;

public class Layer
{
    public string Title { get; set; }
    public string DataSource { get; set; }
    public string Table { get; set; }
    public string GeometryType { get; set; }
    public double[] Extent { get; set; }
}
