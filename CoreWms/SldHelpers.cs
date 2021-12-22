using System.Xml.Serialization;
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

    static SKPathEffect? CreatePathEffect(string? wellKnownName, float width, float size)
    {
        if (string.IsNullOrEmpty(wellKnownName))
            return null;
        return wellKnownName switch
        {
            "shape://vertline" => CreateLines(90, width, size),
            "shape://horline" => CreateLines(0, width, size),
            "shape://slash" => CreateLines(-45, width, size),
            "shape://backslash" => CreateLines(45, width, size),
            _ => throw new Exception($"Unsupported symbol ${wellKnownName}"),
        };
    }

    static public StyledLayerDescriptor FromStream(Stream stream)
    {
        var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
        if (serializer.Deserialize(stream) is not StyledLayerDescriptor sld)
            throw new Exception("Unexpected error deserializing SLD document");
        return sld;
    }

    static SKPaint? ToPaint(Ogc.Se.Fill? fill)
    {
        if (fill == null || fill.GraphicFill == null)
            return null;
        var graphic = fill.GraphicFill.Value.Graphic;
        if (graphic == null)
            return null;
        var size = graphic.Size;
        if (graphic.Mark == null)
            return null;
        var mark = graphic.Mark.First();
        if (mark.Stroke == null || mark.Stroke.SvgParameter == null)
            return null;
        var strokeColor = mark.Stroke.SvgParameter.First(p => p.name == "stroke").Text;
        var strokeWidth = float.Parse(mark.Stroke.SvgParameter.First(p => p.name == "stroke-width").Text ?? "1");
        return new SKPaint()
        {
            Style = SKPaintStyle.Stroke,
            PathEffect = CreatePathEffect(mark.WellKnownName, strokeWidth, size / 1.5f),
            Color = SKColor.Parse(strokeColor),
            StrokeWidth = strokeWidth * 1.5f,
            IsAntialias = true
        };
    }

    static Symbolizer ConvertSymbolizer(Ogc.Se.Symbolizer s)
    {
        var strokeColor = s.Stroke?.SvgParameter?.First(p => p.name == "stroke").Text ?? "#000000";
        var strokeWidth = s.Stroke?.SvgParameter?.First(p => p.name == "stroke-width").Text ?? "1";
        var stroke = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = SKColor.Parse(strokeColor),
            StrokeWidth = float.Parse(strokeWidth) * 1.5f,
            IsAntialias = true,
        };
        return new Symbolizer()
        {
            Stroke = stroke,
            Fill = ToPaint(s.Fill)
        };
    }

    static Rule ConvertRule(Ogc.Se.Rule seRule)
    {
        var symbolizers = seRule.Symbolizer?.Select(ConvertSymbolizer).ToArray();
        var filter = seRule.Filter;

        return new Rule()
        {
            Symbolizers = symbolizers,
            Filter = filter
        };
    }

    static public Rule[]? ToCoreWmsRules(StyledLayerDescriptor sld)
    {
        var userStyle = sld.NamedLayer?.First()?.UserStyle?.First();
        var featureTypeStyle = userStyle?.FeatureTypeStyle?.First();
        var rules = featureTypeStyle?.Rule?.Select(ConvertRule);
        return rules?.ToArray();
    }
}
