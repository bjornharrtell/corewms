using System.Xml.Serialization;
using CoreWms.Ogc.Fes;
using NetTopologySuite.Features;
using Xunit;

namespace CoreWms.UnitTests;

public class FesTests
{
    [Fact]
    public void PropertyIsEqualToTest()
    {
        var xml = @"
<Filter>
    <PropertyIsEqualTo>
        <PropertyName>NAME</PropertyName>
        <Literal>New York</Literal>
    </PropertyIsEqualTo>
</Filter>
";
        var serializer = new XmlSerializer(typeof(Filter));
        var reader = new StringReader(xml);
        if (serializer.Deserialize(reader) is not Filter filter)
            throw new Exception("Content not deserialized to Filter");
        Assert.Collection(filter.PredicateOps, e => Assert.IsType<PropertyIsEqualTo>(e));

        Assert.True(filter.Evaluate(new Feature(null, new AttributesTable { { "NAME", "New York" } } )));
        Assert.False(filter.Evaluate(new Feature(null, new AttributesTable { { "NOME", "New York" } } )));
        Assert.False(filter.Evaluate(new Feature(null, new AttributesTable { { "NAME", "Naw York" } } )));
    }

    [Fact]
    public void PropertyMultipleIsEqualToTest()
    {
        var xml = @"
<Filter>
    <PropertyIsEqualTo>
        <PropertyName>NAME</PropertyName>
        <Literal>New York</Literal>
    </PropertyIsEqualTo>
    <PropertyIsEqualTo>
        <PropertyName>CAT</PropertyName>
        <Literal>City</Literal>
    </PropertyIsEqualTo>
</Filter>
";
        var serializer = new XmlSerializer(typeof(Filter));
        var reader = new StringReader(xml);
        if (serializer.Deserialize(reader) is not Filter filter)
            throw new Exception("Content not deserialized to Filter");
        Assert.Collection(filter.PredicateOps,
                e => Assert.IsType<PropertyIsEqualTo>(e),
                e => Assert.IsType<PropertyIsEqualTo>(e)
        );
        Assert.True(filter.Evaluate(new Feature(null, new AttributesTable {
            { "NAME", "New York" },
            { "CAT", "City" },
        } )));
        Assert.False(filter.Evaluate(new Feature(null, new AttributesTable {
            { "NAME", "New York" }
        } )));
        Assert.False(filter.Evaluate(new Feature(null, new AttributesTable {
            { "NAME", "New York" },
            { "CAT", "City1" },
        } )));
    }

    [Fact]
    public void PropertyAndTest()
    {
        var xml = @"
<Filter>
    <And>
        <PropertyIsEqualTo>
            <PropertyName>NAME</PropertyName>
            <Literal>New York</Literal>
        </PropertyIsEqualTo>
        <PropertyIsEqualTo>
            <PropertyName>CAT</PropertyName>
            <Literal>City</Literal>
        </PropertyIsEqualTo>
    </And>
</Filter>
";
        var serializer = new XmlSerializer(typeof(Filter));
        var reader = new StringReader(xml);
        if (serializer.Deserialize(reader) is not Filter filter)
            throw new Exception("Content not deserialized to Filter");
        Assert.Collection(filter.PredicateOps, e => Assert.IsType<And>(e));

        Assert.True(filter.Evaluate(new Feature(null, new AttributesTable {
            { "NAME", "New York" },
            { "CAT", "City" },
        } )));
    }
}
