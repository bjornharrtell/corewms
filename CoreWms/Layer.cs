#nullable disable
using System;
using CoreWms.DataSource;
using SkiaSharp;

// TODO: make readonly in .NET 5

namespace CoreWms {

    public struct EqualsTo
    {
        public string PropertyName { get; init; }
        public object Literal { get; init; }
    }

    public struct Symbolizer
    {
        public Option<SKPaint> Fill { get; init; }
        public Option<SKPaint> Stroke { get; init; }
    }

    public struct Rule
    {
        public EqualsTo[] Filters { get; init; }
        public Symbolizer[] Symbolizers { get; init; }
    }

    public struct Layer
    {
        public string Name { get; init; }
        public string Title { get; init; }
        public string Schema { get; init; }
        public Type GeometryType { get; init; }
        public Rule[] Rules { get; init; }
        public IDataSource DataSource { get; set; }
    }
}