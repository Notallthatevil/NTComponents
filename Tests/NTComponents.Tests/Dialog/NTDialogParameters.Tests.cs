namespace NTComponents.Tests.Dialog;

public class NTDialogParameters_Tests {
    [Fact]
    public void Get_Returns_Typed_Parameter() {
        var parameters = new NTDialogParameters(new Dictionary<string, object?> {
            ["RecordId"] = 42
        });

        parameters.Get<int>("RecordId").Should().Be(42);
    }

    [Fact]
    public void Indexer_Returns_Parameter() {
        var parameters = new NTDialogParameters(new Dictionary<string, object?> {
            ["RecordId"] = 42
        });

        parameters["RecordId"].Should().Be(42);
    }

    [Fact]
    public void Get_Throws_When_Parameter_Is_Missing() {
        var parameters = new NTDialogParameters(new Dictionary<string, object?>());

        var action = () => parameters.Get<int>("RecordId");

        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("Dialog parameter 'RecordId' was not found.");
    }

    [Fact]
    public void Get_Throws_When_Parameter_Cannot_Be_Cast() {
        var parameters = new NTDialogParameters(new Dictionary<string, object?> {
            ["RecordId"] = "42"
        });

        var action = () => parameters.Get<int>("RecordId");

        action.Should().Throw<InvalidCastException>()
            .WithMessage("Dialog parameter 'RecordId' cannot be cast to Int32.");
    }

    [Fact]
    public void TryGet_Returns_True_For_Null_Reference_Value() {
        var parameters = new NTDialogParameters(new Dictionary<string, object?> {
            ["Name"] = null
        });

        var found = parameters.TryGet<string>("Name", out var value);

        found.Should().BeTrue();
        value.Should().BeNull();
    }

    [Fact]
    public void TryGet_Returns_False_When_Parameter_Cannot_Be_Cast() {
        var parameters = new NTDialogParameters(new Dictionary<string, object?> {
            ["RecordId"] = "42"
        });

        var found = parameters.TryGet<int>("RecordId", out var value);

        found.Should().BeFalse();
        value.Should().Be(default);
    }

    [Fact]
    public void Constructor_Copies_Source_Dictionary() {
        var source = new Dictionary<string, object?> {
            ["RecordId"] = 42
        };
        var parameters = new NTDialogParameters(source);

        source["RecordId"] = 84;

        parameters.Get<int>("RecordId").Should().Be(42);
    }

    [Fact]
    public void Constructor_Accepts_Read_Only_Dictionary() {
        IReadOnlyDictionary<string, object?> source = new Dictionary<string, object?> {
            ["RecordId"] = 42
        };

        var parameters = new NTDialogParameters(source);

        parameters.Get<int>("RecordId").Should().Be(42);
    }

    [Fact]
    public void Constructor_Accepts_Key_Value_Pairs() {
        var source = new[] {
            new KeyValuePair<string, object?>("RecordId", 42)
        };

        var parameters = new NTDialogParameters(source);

        parameters.Get<int>("RecordId").Should().Be(42);
    }

    [Fact]
    public void Constructor_Treats_Null_Source_As_Empty() {
        var parameters = new NTDialogParameters(null);

        parameters.Should().BeEmpty();
    }

    [Fact]
    public void Collection_Initializer_Adds_Parameters() {
        var value = new object();
        var parameters = new NTDialogParameters {
            { "MyKey", 10 },
            { "MyOtherKey", value }
        };

        parameters.Get<int>("MyKey").Should().Be(10);
        parameters.Get<object>("MyOtherKey").Should().BeSameAs(value);
    }

    [Fact]
    public void Dictionary_Implicitly_Converts_To_Dialog_Parameters() {
        NTDialogParameters? parameters = new Dictionary<string, object?> {
            ["RecordId"] = 42
        };

        parameters.Should().NotBeNull();
        parameters!.Get<int>("RecordId").Should().Be(42);
    }

    [Fact]
    public void Null_Dictionary_Implicitly_Converts_To_Empty_Dialog_Parameters() {
        Dictionary<string, object?>? dictionary = null;

        NTDialogParameters parameters = dictionary;

        parameters.Should().BeEmpty();
    }

    [Fact]
    public void Dialog_Parameters_Implicitly_Converts_To_Dictionary() {
        NTDialogParameters parameters = new Dictionary<string, object?> {
            ["RecordId"] = 42
        };

        Dictionary<string, object?> dictionary = parameters;

        dictionary.Should().ContainKey("RecordId").WhoseValue.Should().Be(42);
    }

    [Fact]
    public void Dictionary_Conversion_Returns_Copy() {
        NTDialogParameters parameters = new Dictionary<string, object?> {
            ["RecordId"] = 42
        };

        Dictionary<string, object?> dictionary = parameters;
        dictionary["RecordId"] = 84;

        parameters.Get<int>("RecordId").Should().Be(42);
    }

    [Fact]
    public void Null_Dialog_Parameters_Implicitly_Converts_To_Empty_Dictionary() {
        NTDialogParameters parameters = null!;

        Dictionary<string, object?> dictionary = parameters;

        dictionary.Should().BeEmpty();
    }
}
