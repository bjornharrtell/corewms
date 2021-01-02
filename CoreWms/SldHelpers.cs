using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using CoreWms.Ogc.Fes;
using CoreWms.Ogc.Sld;
using SkiaSharp;

namespace CoreWms {
    static class SldHelpers {

        static SKPathEffect horzLinesPath = SKPathEffect.Create2DLine(3, SKMatrix.CreateScale(6, 6));

        static SKPathEffect vertLinesPath = SKPathEffect.Create2DLine(6,
            Multiply(SKMatrix.CreateRotationDegrees(90), SKMatrix.CreateScale(24, 24)));

        static SKPathEffect diagLinesPath = SKPathEffect.Create2DLine(0.5f,
            Multiply(SKMatrix.CreateScale(4, 4), SKMatrix.CreateRotationDegrees(45)));

        static SKMatrix Multiply(SKMatrix first, SKMatrix second)
        {
            SKMatrix target = SKMatrix.CreateIdentity();
            SKMatrix.Concat(ref target, first, second);
            return target;
        }

        static SKPathEffect CreateDiagLines(float width, float spacing)
        {
            return SKPathEffect.Create2DLine(width, Multiply(SKMatrix.CreateScale(spacing, spacing), SKMatrix.CreateRotationDegrees(45)));
        }

        static public StyledLayerDescriptor FromStream(Stream stream)  {
            var serializer = new XmlSerializer(typeof(StyledLayerDescriptor));
            if (!(serializer.Deserialize(stream) is StyledLayerDescriptor sld))
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
                SKPathEffect? pathEffect = null;
                if (mark.WellKnownName == "shape://backslash")
                    pathEffect = CreateDiagLines(strokeWidth, size);
                return new SKPaint() {
                    Style = SKPaintStyle.Stroke,
                    PathEffect = pathEffect,
                    Color = SKColor.Parse(strokeColor),
                    StrokeWidth = strokeWidth,
                    IsAntialias = true
                };
            }
            return null;
        }

        static Rule ToCoreWmsRule(Ogc.Se.Rule seRule)
        {
            var ps = seRule.Symbolizer.First();
            var rule = new Rule();
            var strokeColor = ps.Stroke.SvgParameter.First(p => p.name == "stroke").Text;
            var strokeWidth = ps.Stroke.SvgParameter.First(p => p.name == "stroke-width").Text;
            rule.Stroke = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColor.Parse(strokeColor),
                StrokeWidth = float.Parse(strokeWidth),
                IsAntialias = true,
            };
            rule.Fill = ToPaint(ps.Fill);
            rule.Filter = new EqualsTo
            {
                PropertyName = (seRule.Filter.First().ComparisonOps.First() as PropertyIsEqualTo).PropertyName.Text
            };
            var literal = (seRule.Filter.First().ComparisonOps.First() as PropertyIsEqualTo).Literal.Text;
            if (float.TryParse(literal, out float literalFloat))
                rule.Filter.Literal = (short) literalFloat;
            else
                rule.Filter.Literal = literal;
            return rule;
        }

        static public IList<Rule> ToCoreWmsRules(StyledLayerDescriptor sld) {
            var userStyle = sld.NamedLayer.First().UserStyle.First();
            var featureTypeStyle = userStyle.FeatureTypeStyle.First();
            var rules = featureTypeStyle.Rule.Select(ToCoreWmsRule);
            return rules.ToList();
        }
    }
}