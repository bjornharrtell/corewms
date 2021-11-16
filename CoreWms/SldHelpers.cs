using System.Xml.Serialization;
using CoreWms.Ogc.Fes;
using CoreWms.Ogc.Sld;
using SkiaSharp;

namespace CoreWms;

static class SldHelpers
{
    static SKMatrix Multiply(SKMatrix first, SKMatrix second)
    {
        SKMatrix target = SKMatrix.CreateIdentity();
        SKMatrix.Concat(ref target, first, second);
        return target;
    }

    static SKPathEffect CreateLines(float degrees, float width, float spacing)
    {
        return SKPathEffect.Create2DLine(width,
            Multiply(
                SKMatrix.CreateScale(spacing, spacing),
                SKMatrix.CreateRotationDegrees(degrees))
        );
    }

    static SKPathEffect? CreatePathEffect(string wellKnownName, float width, float size)
    {
        if (string.IsNullOrEmpty(wellKnownName))
            return null;
        return wellKnownName switch
        {
            "shape://vertline" => CreateLines(90, width, size),
            "shape://horline" => CreateLines(0, width, size),
            "shape://slash" => CreateLines(-45, width, size),
            "shape://backslash" => CreateLines(45, width, size),
            _ => throw new System.Exception($"Unsupported symbol ${wellKnownName}"),
        };
    }

    static public StyledLayerDescriptor FromStream(Stream stream)
    {
        var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
        if (serializer.Deserialize(stream) is not StyledLayerDescriptor sld)
            throw new System.Exception("Unexpected error deserializing SLD document");
        return sld;
    }

    static Option<SKPaint> ToPaint(Ogc.Se.Fill fill)
    {
        if (fill == null || fill.GraphicFill == null)
            return new Option<SKPaint>();
        var graphic = fill.GraphicFill.Graphic;
        var size = graphic.Size;
        var mark = graphic.Mark.First();
        var strokeColor = mark.Stroke.SvgParameter.First(p => p.name == "stroke").Text;
        var strokeWidth = float.Parse(mark.Stroke.SvgParameter.First(p => p.name == "stroke-width").Text);
        return new Option<SKPaint>(new SKPaint()
        {
            Style = SKPaintStyle.Stroke,
            PathEffect = CreatePathEffect(mark.WellKnownName, strokeWidth, size / 1.5f),
            Color = SKColor.Parse(strokeColor),
            StrokeWidth = strokeWidth * 1.5f,
            IsAntialias = true
        });
    }

    static Symbolizer ConvertSymbolizer(Ogc.Se.Symbolizer s)
    {
        var strokeColor = s.Stroke.SvgParameter.First(p => p.name == "stroke").Text;
        var strokeWidth = s.Stroke.SvgParameter.First(p => p.name == "stroke-width").Text;
        var stroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColor.Parse(strokeColor),
            StrokeWidth = float.Parse(strokeWidth) * 1.5f,
            IsAntialias = true,
        };
        return new Symbolizer()
        {
            Stroke = new Option<SKPaint>(stroke),
            Fill = ToPaint(s.Fill)
        };
    }

    static EqualsTo ConvertFilter(Ogc.Fes.Filter f)
    {
        // TODO: support other types of filters
        var propertyIsEqualTo = f.ComparisonOps.First() as PropertyIsEqualTo;
        var literalText = propertyIsEqualTo.Literal.Text;
        object literal;
        if (float.TryParse(literalText, out float literalFloat))
            literal = (short)literalFloat;
        else
            literal = literalText;
        return new EqualsTo()
        {
            PropertyName = propertyIsEqualTo.PropertyName.Text,
            Literal = literal
        };
    }

    static Rule ConvertRule(Ogc.Se.Rule seRule)
    {
        var symbolizers = seRule.Symbolizer.Select(ConvertSymbolizer).ToArray();
        var filters = seRule.Filter.Select(ConvertFilter).ToArray();

        return new Rule()
        {
            Symbolizers = symbolizers,
            Filters = filters
        };
    }

    static public Rule[] ToCoreWmsRules(StyledLayerDescriptor sld)
    {
        var userStyle = sld.NamedLayer.First().UserStyle.First();
        var featureTypeStyle = userStyle.FeatureTypeStyle.First();
        var rules = featureTypeStyle.Rule.Select(ConvertRule);
        return rules.ToArray();
    }
}
