using System.Xml.Serialization;
using CoreWms.Ogc.Fes;
using Xunit;

namespace CoreWms.UnitTests;

public class FesTests
{
    [Fact]
    public async Task PropertyIsEqualToTest()
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
        Assert.Collection(filter.PropertyIsEqualTo, e => Assert.Equal("New York", e.Literal.Text));

        Assert.True(filter.Evaluate("New York"));
        Assert.True(!filter.Evaluate("Naw York"));
    }

    [Fact]
    public async Task PropertyMultipleIsEqualToTest()
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
        Assert.Collection(filter.PropertyIsEqualTo,
                e => Assert.Equal("New York", e.Literal.Text),
                e => Assert.Equal("City", e.Literal.Text)
        );
    }

    [Fact]
    public async Task PropertyAndTest()
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
        Assert.Collection(filter.And,
            e => Assert.Collection(e.PropertyIsEqualTo,
                e => Assert.Equal("New York", e.Literal.Text),
                e => Assert.Equal("City", e.Literal.Text))
        );
    }
}
