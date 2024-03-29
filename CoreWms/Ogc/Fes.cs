using System.Xml.Serialization;
using NetTopologySuite.Features;

namespace CoreWms.Ogc.Fes;

public readonly struct PropertyName
{
    [XmlText]
    public string Text { get; init; }
}

public struct Literal
{
    string _text;
    object _object;

    [XmlText]
    public string Text
    {
        get
        {
            return _text;
        }
        set
        {
            _text = value;
            if (int.TryParse(value, out int intValue))
                _object = intValue;
            else
                _object = value;
        }
    }

    public object Object { get { return _object; }}
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


public abstract class UnaryOpsType : PredicateOpsType
{
    public PropertyName PropertyName;

    public override string[] GetRequiredPropertyNames()
    {
        return new string[] { PropertyName.Text };
    }
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
        return value?.Equals(Literal.Object) ?? false;
    }
}

public class PropertyIsNotEqualTo : ComparisonOpsType
{
    public override bool Evaluate(IFeature f)
    {
        var value = f.Attributes.GetOptionalValue(PropertyName.Text);
        return !(value?.Equals(Literal.Object) ?? true);
    }
}

public class PropertyIsNull : UnaryOpsType
{
    public override bool Evaluate(IFeature f)
    {
        var value = f.Attributes.GetOptionalValue(PropertyName.Text);
        return value == null;
    }
}

public class FilterPredicates : PredicateOpsType
{
    [XmlElement("PropertyIsEqualTo", Type = typeof(PropertyIsEqualTo))]
    [XmlElement("PropertyIsNotEqualTo", Type = typeof(PropertyIsNotEqualTo))]
    [XmlElement("PropertyIsNull", Type = typeof(PropertyIsNull))]
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