using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Shapes;

public sealed class NTShapeCatalog_Tests : BunitContext {

    public NTShapeCatalog_Tests() {
        SetRendererInfo(new RendererInfo("WebAssembly", true));
        var module = JSInterop.SetupModule("./_content/NTComponents/Shapes/NTShape.razor.js");
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
    }

    // Behavior source: NTShapeType documents the supported expressive catalog, and NTShape renders the selected shape in an objectBoundingBox clip path.
    [Fact]
    public void Every_Defined_Shape_Renders_A_Unique_Normalized_Closed_Path() {
        var paths = new Dictionary<NTShapeType, string>();

        foreach (var shape in Enum.GetValues<NTShapeType>()) {
            using var cut = Render<NTShape>(parameters => parameters.Add(component => component.Shape, shape));
            var root = cut.Find("nt-shape");
            var clipPath = cut.Find("clipPath");
            var pathData = cut.Find(".nt-shape-path").GetAttribute("d");

            root.GetAttribute("data-shape").Should().Be(((int)shape).ToString(CultureInfo.InvariantCulture));
            clipPath.GetAttribute("clipPathUnits").Should().Be("objectBoundingBox");
            pathData.Should().NotBeNullOrWhiteSpace();
            pathData.Should().StartWith("M").And.Contain(" L").And.EndWith(" Z");

            var coordinates = Regex.Matches(pathData!, @"-?\d+(?:\.\d+)?")
                .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture))
                .ToArray();
            coordinates.Should().NotBeEmpty().And.OnlyContain(coordinate => coordinate >= 0d && coordinate <= 1d);
            paths.Add(shape, pathData!);
        }

        paths.Values.Distinct(StringComparer.Ordinal).Should().HaveCount(paths.Count);
    }

    // Behavior source: NTShape.Shape selects catalog entries; returning to a prior value must restore the same deterministic clip path.
    [Fact]
    public void Returning_To_A_Previous_Shape_Restores_Its_Path() {
        var cut = Render<NTShape>(parameters => parameters.Add(component => component.Shape, NTShapeType.Circle));
        var circlePath = cut.Find(".nt-shape-path").GetAttribute("d");

        cut.Render(parameters => parameters.Add(component => component.Shape, NTShapeType.Heart));
        var heartPath = cut.Find(".nt-shape-path").GetAttribute("d");
        cut.Render(parameters => parameters.Add(component => component.Shape, NTShapeType.Circle));

        heartPath.Should().NotBe(circlePath);
        cut.Find(".nt-shape-path").GetAttribute("d").Should().Be(circlePath);
    }
}
