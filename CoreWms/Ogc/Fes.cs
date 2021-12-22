using System.Xml.Serialization;
using NetTopologySuite.Features;

namespace CoreWms.Ogc.Fes;

public readonly struct PropertyName
{
    [XmlText]
    public string Text { get; init; }
}

public readonly struct Literal
{
    [XmlText]
    public string Text { get; init; }
}

public abstract class LogicOpsType : FilterPredicates { }

public class And : LogicOpsType {}

public class Or : LogicOpsType
{
    public override bool Evaluate(IFeature f) =>
        PredicateOps?.Any(o => o.Evaluate(f)) ?? false;
}

public abstract class PredicateOpsType
{
    public abstract bool Evaluate(IFeature f);
    public abstract string[] GetRequiredPropertyNames();
}

public abstract class ComparisonOpsType : PredicateOpsType
{
    public PropertyName PropertyName;
    public Literal Literal;

    public override string[] GetRequiredPropertyNames()
    {
        return new string[] { PropertyName.Text };
    }
}

public class PropertyIsEqualTo : ComparisonOpsType
{
    public override bool Evaluate(IFeature f)
    {
        var value = f.Attributes.GetOptionalValue(PropertyName.Text);
        if (value == null)
            return false;
        else
            return Literal.Text == Convert.ToString(value);
    }
}
public class PropertyIsNotEqualTo : ComparisonOpsType
{
    public override bool Evaluate(IFeature f)
    {
        var value = f.Attributes.GetOptionalValue(PropertyName.Text);
        if (value == null)
            return false;
        else
            return Literal.Text != Convert.ToString(value);
    }
}

public class FilterPredicates : PredicateOpsType
{
    [XmlElement("PropertyIsEqualTo", Type = typeof(PropertyIsEqualTo))]
    [XmlElement("PropertyIsNotEqualTo", Type = typeof(PropertyIsNotEqualTo))]
    [XmlElement("And", Type = typeof(And))]
    [XmlElement("Or", Type = typeof(Or))]
    public PredicateOpsType[]? PredicateOps;

    public override bool Evaluate(IFeature f) =>
        PredicateOps?.All(op => op.Evaluate(f)) ?? false;

    public override string[] GetRequiredPropertyNames()
    {
        return PredicateOps?.SelectMany(op => op.GetRequiredPropertyNames()).ToArray() ?? Array.Empty<string>();
    }
}

public class Filter : FilterPredicates
{
}