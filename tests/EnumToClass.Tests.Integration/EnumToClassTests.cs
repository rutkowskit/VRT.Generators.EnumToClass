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

    [Fact]
    public void Equals_WhenSameElementClasses_ShouldBeTrue()
    {
        TestElementClass a = TestElementClass.Element1;
        TestElementClass b = TestElementClass.Element1;

        a.Should().BeEquivalentTo(b);
    }

    [Fact]
    public void Equals_WhenSameElementRecords_ShouldBeTrue()
    {
        TestElementRecord a = TestElementRecord.Element1;
        TestElementRecord b = TestElementRecord.Element1;
        a.Should().BeEquivalentTo(b);
    }
}
