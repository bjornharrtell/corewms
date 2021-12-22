#nullable disable
using System.Xml.Serialization;

namespace CoreWms.Ogc.Fes;

public class PropertyName
{
    [XmlText]
    public string Text;
}

public class Literal
{
    private string _text;
    private object _parsedValue;

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
            if (Int32.TryParse(_text, out int parsedInt))
                _parsedValue = parsedInt;
            else
                _parsedValue = _text;
        }
    }

    public object ParsedValue { get { return _parsedValue; } }
}

public abstract class LogicOpsType : FilterPredicates { }

public class And : LogicOpsType {}

public class Or : LogicOpsType
{
    public new bool Evaluate(object value)
    {
        return
            PropertyIsEqualTo.All(p => p.Evaluate(value)) ||
            PropertyIsNotEqualTo.All(p => p.Evaluate(value)) ||
            And.All(p => p.Evaluate(value)) ||
            Or.All(p => p.Evaluate(value));
    }
}

public abstract class ComparisonOpsType
{
    public PropertyName PropertyName;
    public Literal Literal;

    public abstract bool Evaluate(object value);
}

public class PropertyIsEqualTo : ComparisonOpsType
{
    public override bool Evaluate(object value) => Literal.ParsedValue.Equals(value);
}
public class PropertyIsNotEqualTo : ComparisonOpsType
{
    public override bool Evaluate(object value) => !Literal.ParsedValue.Equals(value);
}

public class FilterPredicates
{
    [XmlElement("PropertyIsEqualTo")]
    public PropertyIsEqualTo[] PropertyIsEqualTo;
    [XmlElement("PropertyIsNotEqualTo")]
    public PropertyIsNotEqualTo[] PropertyIsNotEqualTo;
    [XmlElement("And")]
    public And[] And;
    [XmlElement("Or")]
    public Or[] Or;

    public bool Evaluate(object value)
    {
        return
            (PropertyIsEqualTo?.All(p => p.Evaluate(value)) ?? true) &&
            (PropertyIsNotEqualTo?.All(p => p.Evaluate(value)) ?? true) &&
            (And?.All(p => p.Evaluate(value)) ?? true) &&
            (Or?.All(p => p.Evaluate(value)) ?? true);
    }
}

public class Filter : FilterPredicates
{
}