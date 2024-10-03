using System;
using System.Linq;
using System.Net;

namespace EnumToClass.Tests.Integration;

public sealed class EnumToClassTests
{
    [Theory]
    [InlineData(TestElements.None, true)]
    [InlineData(TestElements.Element1, false)]
    [InlineData(TestElements.Element2, false)]
    [InlineData(TestElements.Element3, false)]
    public void Generated_ImplicitEnumToClassConversionTests(TestElements element, bool expectedIsEmpty)
    {
        TestElementClass sut = element;
        sut.Value.Should().Be(element);
        sut.IsEmpty.Should().Be(expectedIsEmpty);
    }

    [Theory]
    [InlineData(TestElements.None, true)]
    [InlineData(TestElements.Element1, false)]
    [InlineData(TestElements.Element2, false)]
    [InlineData(TestElements.Element3, false)]
    public void Generated_ImplicitConversionByCodeTests(TestElements element, bool expectedIsEmpty)
    {
        TestElementClass sut = element.ToString();
        sut.Value.Should().Be(element);
        sut.IsEmpty.Should().Be(expectedIsEmpty);
    }


    [Theory]
    [InlineData(TestElements.None, TestElementClass.None)]
    [InlineData(TestElements.Element1, TestElementClass.Element1)]
    [InlineData(TestElements.Element2, TestElementClass.Element2)]
    [InlineData(TestElements.Element3, TestElementClass.Element3)]
    public void Generated_WithTestElementsEnum_ShouldGenerateConstantValues(TestElements element, string expectedStringValue)
    {
        TestElementClass sut = element;
        sut.Name.Should().Be(expectedStringValue);
        sut.Value.Should().Be(element);
    }

    [Theory]
    [InlineData("Something not existing", TestElements.None)]
    [InlineData(TestElementClass.None, TestElements.None)]
    [InlineData(TestElementClass.Element1, TestElements.Element1)]
    [InlineData(TestElementClass.Element2, TestElements.Element2)]
    [InlineData(TestElementClass.Element3, TestElements.Element3)]
    public void Generated_WhenGetByName_ShouldResolveCorrectValue(string name, TestElements expectedValue)
    {
        var sut = TestElementClass.GetByName(name);
        sut.Value.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData(TestElements.None)]
    [InlineData(TestElements.Element1)]
    [InlineData(TestElements.Element2)]
    [InlineData(TestElements.Element3)]
    public void Generated_ImplicitConversionToUnderlyingEnumType_ShouldSucceed(TestElements value)
    {
        var expectedValue = (int)value;
        TestElementClass sut = value;

        int underyingTypeValue = sut;

        underyingTypeValue.Should().Be(expectedValue);
    }


    [Fact]
    public void Equals_WhenSameElementClasses_ShouldBeTrue()
    {
        TestElementClass a = TestElementClass.Element1;
        TestElementClass b = TestElementClass.Element1;
        a.Should().BeEquivalentTo(b);
    }

    [Fact]
    public void GetAll_WhenTestElementClass_ShouldReturnAllValues()
    {
        var allValues = TestElementClass.GetAll();
        var expectedValues = Enum.GetValues<TestElements>();
        allValues.Should().HaveCount(expectedValues.Length);
        expectedValues.All(e => allValues.Any(v => v.Value == e)).Should().BeTrue();
    }

    [Fact]
    public void Equals_WhenSameElementRecords_ShouldBeTrue()
    {
        TestElementRecord a = TestElementRecord.Element1;
        TestElementRecord b = TestElementRecord.Element1;
        a.Should().BeEquivalentTo(b);
    }

    [Fact]
    public void Generated_WhenExternalEnumIsUsedInParameter_ShouldContainAllValuesFromSource()
    {
        var a = Enum.GetValues<HttpResponseHeader>();
        HttpResponseHeaderClass.GetAll().Should().HaveCount(a.Length);
    }

    [Theory]
    [InlineData(typeof(TestElementRecord), true)]
    [InlineData(typeof(TestElementClass), false)]
    public void Generated_WithDescriptionTests(Type classType, bool shouldHaveDescription)
    {
        var propertyInfo = classType.GetProperties().FirstOrDefault(p => p.Name == "Description");
        (propertyInfo is not null).Should().Be(shouldHaveDescription);
    }

    [Theory]
    [InlineData(TestElements.None, "Empty element")]
    [InlineData(TestElements.Element1, "First element of enum")]
    [InlineData(TestElements.Element3, "This is element 3 of the test enum")]
    [InlineData(TestElements.Element4, "This test element calculates the factorial of a given non-negative integer.")]
    [InlineData(TestElements.Element5, "The fifth element")]
    [InlineData(TestElements.Element6, nameof(TestElements.Element6))]
    public void Generated_WithDescription_ShouldHaveCorrectDescription(TestElements element, string expectedDescription)
    {
        TestElementRecord sut = element;
        sut.Description.Should().Be(expectedDescription);
    }
}
