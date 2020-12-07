using System;
using System.Collections.Generic;
using CoreWms.DataSource;
using SkiaSharp;

namespace CoreWms {

    public class EqualsTo
    {
        public string PropertyName { get; set; }
        public object Literal { get; set; }
    }

    public class Rule
    {
        public EqualsTo Filter { get; set; }
        public SKPaint Fill { get; set; }
        public SKPaint Stroke { get; set; }
    }

    public class Layer
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Schema { get; set; }
        public string FeatureType { get; set; }
        public Type GeometryType { get; set; }
        public IList<Rule> Rules { get; set; }
        public IDataSource DataSource { get; set; }
    }
}