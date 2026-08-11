using System;
using System.Linq;
using System.Net;
using System.Reflection;

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

    [Theory]
    [InlineData(0, TestElements.None, true)]
    [InlineData(1, TestElements.Element1)]
    [InlineData(2, TestElements.Element2)]
    [InlineData(3, TestElements.Element3)]
    public void Generated_ImplicitUnderlyingEnumTypeToClassConversionTests(
        int value,
        TestElements expectedEnumValue,
        bool expectedIsEmpty = false)
    {
        TestElementClass sut = value;
        sut.Value.Should().Be(expectedEnumValue);
        sut.IsEmpty.Should().Be(expectedIsEmpty);
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
    [InlineData(TestElements.Element7, "First line of multi summary.\nSecond line of multi summary.")]
    public void Generated_WithDescription_ShouldHaveCorrectDescription(TestElements element, string expectedDescription)
    {
        TestElementRecord sut = element;
        sut.Description.Should().Be(expectedDescription);
    }


    [Fact]
    public void Generated_WhenTestElementClass_ShouldContainInstanceStaticFields()
    {
        TestElementClass.Element1Instance.Value.Should().Be(TestElements.Element1);
        TestElementClass.Element2Instance.Value.Should().Be(TestElements.Element2);
        TestElementClass.Element3Instance.Value.Should().Be(TestElements.Element3);
        TestElementClass.Element4Instance.Value.Should().Be(TestElements.Element4);
        TestElementClass.Element5Instance.Value.Should().Be(TestElements.Element5);
        TestElementClass.Element6Instance.Value.Should().Be(TestElements.Element6);
        TestElementClass.Element7Instance.Value.Should().Be(TestElements.Element7);
    }

    [Fact]
    public void Empty_WhenDefaultIsNamed_ShouldBeSameInstanceAsDefaultMember()
    {
        ReferenceEquals(TestElementClass.Empty, TestElementClass.NoneInstance).Should().BeTrue();
        ReferenceEquals(TestElementClass.Empty, TestElementClass.GetByName("missing")).Should().BeTrue();
        TestElementClass.Empty.IsEmpty.Should().BeTrue();
        TestElementClass.Empty.Value.Should().Be(default(TestElements));
    }

    [Fact]
    public void Empty_WhenFirstMemberIsNotDefault_ShouldUseDefaultValueNotFirstField()
    {
        NonZeroFirstElementClass.Empty.Value.Should().Be(NonZeroFirstElements.Zero);
        NonZeroFirstElementClass.Empty.IsEmpty.Should().BeTrue();
        NonZeroFirstElementClass.AlphaInstance.IsEmpty.Should().BeFalse();
        ReferenceEquals(NonZeroFirstElementClass.Empty, NonZeroFirstElementClass.ZeroInstance).Should().BeTrue();
        ReferenceEquals(NonZeroFirstElementClass.GetByName("nope"), NonZeroFirstElementClass.Empty).Should().BeTrue();
    }

    [Fact]
    public void Description_WhenContainsQuotesAndNewlines_ShouldRoundTrip()
    {
        SpecialDescriptionElementClass.QuotedInstance.Description.Should().Be("He said \"hi\"\nand left");
        SpecialDescriptionElementClass.PlainInstance.Description.Should().Be("plain");
    }

    [Fact]
    public void Flyweight_WhenConvertedViaDifferentPaths_ShouldReturnSameInstance()
    {
        TestElementClass fromEnum = TestElements.Element2;
        TestElementClass fromName = TestElementClass.GetByName(nameof(TestElements.Element2));
        TestElementClass fromInstance = TestElementClass.Element2Instance;
        TestElementClass fromConstString = TestElementClass.Element2;

        ReferenceEquals(fromEnum, fromName).Should().BeTrue();
        ReferenceEquals(fromEnum, fromInstance).Should().BeTrue();
        ReferenceEquals(fromEnum, fromConstString).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperators_WhenSameValue_ShouldBeEqual()
    {
        var a = TestElementClass.Element1Instance;
        var b = TestElementClass.GetByName(nameof(TestElements.Element1));
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void EqualityOperators_WhenDifferentValue_ShouldNotBeEqual()
    {
        (TestElementClass.Element1Instance == TestElementClass.Element2Instance).Should().BeFalse();
        (TestElementClass.Element1Instance != TestElementClass.Element2Instance).Should().BeTrue();
    }

    [Fact]
    public void TryGetByName_WhenExists_ShouldReturnTrueAndInstance()
    {
        var ok = TestElementClass.TryGetByName(nameof(TestElements.Element3), out var value);
        ok.Should().BeTrue();
        value.Value.Should().Be(TestElements.Element3);
        ReferenceEquals(value, TestElementClass.Element3Instance).Should().BeTrue();
    }

    [Fact]
    public void TryGetByName_WhenMissing_ShouldReturnFalseAndEmpty()
    {
        var ok = TestElementClass.TryGetByName("does-not-exist", out var value);
        ok.Should().BeFalse();
        ReferenceEquals(value, TestElementClass.Empty).Should().BeTrue();
    }

    [Fact]
    public void UnderlyingByte_WhenConverted_ShouldRoundTrip()
    {
        ByteBackedElementClass sut = ByteBackedElements.Two;
        byte raw = sut;
        raw.Should().Be(2);
        ByteBackedElementClass back = raw;
        back.Value.Should().Be(ByteBackedElements.Two);
        ReferenceEquals(back, ByteBackedElementClass.TwoInstance).Should().BeTrue();
    }

    [Fact]
    public void GetByName_IsCaseSensitive()
    {
        TestElementClass.GetByName("element1").IsEmpty.Should().BeTrue();
        TestElementClass.GetByName("Element1").Value.Should().Be(TestElements.Element1);
    }

    [Fact]
    public void AttributeProperties_WhenBothAttributes_ShouldExposeInstances()
    {
        var sut = MetadataElementClass.BothInstance;
        sut.Metadata1.Should().NotBeNull();
        sut.Metadata1!.Label.Should().Be("alpha");
        sut.Meta2.Should().NotBeNull();
    }

    [Fact]
    public void AttributeProperties_WhenOnlySecond_ShouldLeaveFirstNull()
    {
        var sut = MetadataElementClass.OnlySecondInstance;
        sut.Metadata1.Should().BeNull();
        sut.Meta2.Should().NotBeNull();
    }

    [Fact]
    public void AttributeProperties_WhenNone_ShouldBeNull()
    {
        var sut = MetadataElementClass.BareInstance;
        sut.Metadata1.Should().BeNull();
        sut.Meta2.Should().BeNull();
    }

    [Fact]
    public void AttributeProperties_DefaultName_StripsAttributeSuffix()
    {
        typeof(MetadataElementClass).GetProperty(nameof(MetadataElementClass.Metadata1)).Should().NotBeNull();
        typeof(MetadataElementClass).GetProperty("Metadata1Attribute").Should().BeNull();
        typeof(MetadataElementClass).GetProperty(nameof(MetadataElementClass.Meta2)).Should().NotBeNull();
    }

    [Fact]
    public void AttributeProperties_InternalAttribute_PropertyIsInternal()
    {
        var prop = typeof(InternalMetaElementClass).GetProperty(
            "InternalMeta",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        prop.Should().NotBeNull();
        prop!.GetMethod.Should().NotBeNull();
        prop.GetMethod!.IsAssembly.Should().BeTrue();
        InternalMetaElementClass.AInstance.InternalMeta.Should().NotBeNull();
    }

    [Fact]
    public void AttributeProperties_AsArray_WhenSeveral_ShouldReturnAll()
    {
        var sut = RoleTypeClass.EditorInstance;
        sut.Permissions.Should().HaveCount(2);
        sut.Permissions.Select(p => p.Name).Should().BeEquivalentTo("read", "write");
    }

    [Fact]
    public void AttributeProperties_AsArray_WhenOne_ShouldReturnSingleElementArray()
    {
        var sut = RoleTypeClass.ViewerInstance;
        sut.Permissions.Should().ContainSingle(p => p.Name == "read");
    }

    [Fact]
    public void AttributeProperties_AsArray_WhenNone_ShouldReturnEmptyArray()
    {
        var sut = RoleTypeClass.GuestInstance;
        sut.Permissions.Should().NotBeNull().And.BeEmpty();
    }
}
