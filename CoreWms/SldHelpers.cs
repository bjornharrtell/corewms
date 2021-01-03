using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CoreWms.Ogc.Fes;
using CoreWms.Ogc.Sld;
using SkiaSharp;

namespace CoreWms {
    static class SldHelpers {
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
                "shape://slash" => CreateLines(45, width, size),
                "shape://backslash" => CreateLines(-45, width, size),
                _ => throw new System.Exception($"Unsupported symbol ${wellKnownName}"),
            };
        }

        static public StyledLayerDescriptor FromStream(Stream stream)  {
            var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
            if (serializer.Deserialize(stream) is not StyledLayerDescriptor sld)
                throw new System.Exception("Unexpected error deserializing SLD document");
            return sld;
        }

        static SKPaint? ToPaint(Ogc.Se.Fill fill)
        {
            if (fill.GraphicFill != null)
            {
                var graphic = fill.GraphicFill.Graphic;
                var size = graphic.Size;
                var mark = graphic.Mark.First();
                var strokeColor = mark.Stroke.SvgParameter.First(p => p.name == "stroke").Text;
                var strokeWidth = float.Parse(mark.Stroke.SvgParameter.First(p => p.name == "stroke-width").Text);
                return new SKPaint() {
                    Style = SKPaintStyle.Stroke,
                    PathEffect = CreatePathEffect(mark.WellKnownName, strokeWidth, size),
                    Color = SKColor.Parse(strokeColor),
                    StrokeWidth = strokeWidth,
                    IsAntialias = true
                };
            }
            return null;
        }

        static Symbolizer ToSymboliser(Ogc.Se.Symbolizer s)
        {
            var strokeColor = s.Stroke.SvgParameter.First(p => p.name == "stroke").Text;
            var strokeWidth = s.Stroke.SvgParameter.First(p => p.name == "stroke-width").Text;
            var stroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColor.Parse(strokeColor),
                StrokeWidth = float.Parse(strokeWidth),
                IsAntialias = true,
            };
            var fill = ToPaint(s.Fill);
            return new Symbolizer() {
                Stroke = stroke,
                Fill = fill
            };
        }

        static Rule ToCoreWmsRule(Ogc.Se.Rule seRule)
        {
            var symbolizers = seRule.Symbolizer.Select(s => ToSymboliser(s)).ToArray();

            // TODO: support other types of filters
            var seFilter = seRule.Filter.FirstOrDefault();
            EqualsTo? filter = null;
            if (seFilter != null)
            {
                var propertyIsEqualTo = seFilter.ComparisonOps.First() as PropertyIsEqualTo;
                var literalText = propertyIsEqualTo.Literal.Text;
                object literal;
                if (float.TryParse(literalText, out float literalFloat))
                    literal = (short) literalFloat;
                else
                    literal = literalText;
                filter = new EqualsTo()
                {
                    PropertyName = propertyIsEqualTo.PropertyName.Text,
                    Literal = literal
                };
            }

            var rule = new Rule() {
                Symbolizers = symbolizers,
                Filters = filter != null ? new EqualsTo[] { filter.Value } : new EqualsTo[] { }
            };

            return rule;
        }

        static public Rule[] ToCoreWmsRules(StyledLayerDescriptor sld) {
            var userStyle = sld.NamedLayer.First().UserStyle.First();
            var featureTypeStyle = userStyle.FeatureTypeStyle.First();
            var rules = featureTypeStyle.Rule.Select(ToCoreWmsRule);
            return rules.ToArray();
        }
    }
}