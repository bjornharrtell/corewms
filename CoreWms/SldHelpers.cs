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
        if (fill == null)
            return null;
        var graphic = fill.GraphicFill?.Graphic;
        SKColor color = SKColor.Parse("#000000");
        SKPaintStyle paintStyle = SKPaintStyle.Fill;

        SKPathEffect? pathEffect = null;
        float strokeWidth = 0;
        if (graphic != null && graphic.Mark != null)
        {
            var size = graphic.Size;
            var mark = graphic.Mark.First();
            color = SKColor.Parse(mark.Stroke?.CssParameter?.First(p => p.name == "stroke").Text);
            strokeWidth = float.Parse(mark.Stroke?.CssParameter?.First(p => p.name == "stroke-width").Text ?? "1");
            pathEffect = CreatePathEffect(mark.WellKnownName, strokeWidth, size / 1.5f);
            paintStyle = SKPaintStyle.Stroke;
        }
        var fillParam = fill.CssParameter?.FirstOrDefault(p => p.name == "fill");
        if (fillParam != null)
            color = SKColor.Parse(fillParam.Text);
        var fillOpacity = fill.CssParameter?.FirstOrDefault(p => p.name == "fill-opacity");
        byte alpha = (byte) (float.Parse(fillOpacity?.Text ?? "1") * 255);

        return new SKPaint()
        {
            Style = paintStyle,
            PathEffect = pathEffect,
            Color = color.WithAlpha(alpha),
            StrokeWidth = strokeWidth * 1.5f,
            IsAntialias = true
        };
    }

    static Symbolizer ConvertSymbolizer(Ogc.Se.Symbolizer s)
    {
        SKPaint? stroke = null;
        if (s.Stroke != null)
        {
            var strokeColor = s.Stroke.CssParameter?.First(p => p.name == "stroke").Text ?? "#000000";
            var strokeWidth = s.Stroke.CssParameter?.First(p => p.name == "stroke-width").Text ?? "1";
            byte alpha = (byte) (float.Parse(s.Stroke.CssParameter?.FirstOrDefault(p => p.name == "stroke-opacity")?.Text ?? "1") * 255);
            stroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColor.Parse(strokeColor).WithAlpha(alpha),
                StrokeWidth = float.Parse(strokeWidth) * 1.5f,
                IsAntialias = true,
            };
        }
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
        double? minResolution = null;
        double? maxResolution = null;
        if (seRule.MinScaleDenominator != null)
            minResolution = 0.0254 / 90 * int.Parse(seRule.MinScaleDenominator);
        if (seRule.MaxScaleDenominator != null)
            maxResolution = 0.0254 / 90 * int.Parse(seRule.MaxScaleDenominator);

        return new Rule()
        {
            Symbolizers = symbolizers,
            Filter = filter,
            MinResolution = minResolution,
            MaxResolution = maxResolution
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
