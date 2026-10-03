#if NET10_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using NTComponents.Site.Components;
using NTComponents.Site.Documentation;

namespace NTComponents.Tests.Site;

public class DocumentationSandbox_Tests : BunitContext {
    private readonly DocumentationCatalog _catalog = new();

    public DocumentationSandbox_Tests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(_ => true).Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddNTServices();
    }

    [Fact]
    public void InvalidLabel_ShowsTheContractError_AndResetRestoresThePreview() {
        var cut = Render<DocumentationSandbox>(parameters => parameters.Add(component => component.Component, _catalog.GetComponent("ntbutton")!));
        cut.Find("#docs-sandbox-ntbutton-label").Change(string.Empty);

        cut.Find(".docs-generated-example [role=alert]").TextContent.Should().Contain("NTButton requires a non-empty Label");
        cut.Find(".docs-sandbox-controls").Should().NotBeNull();
        cut.Find(".docs-generated-example [role=alert] button").Click();

        cut.FindAll(".docs-generated-example [role=alert]").Should().BeEmpty();
        cut.Find(".docs-generated-example button").TextContent.Should().Contain("Save changes");
    }

    [Fact]
    public void EditedInput_RetainsItsBoundValue_WhenTheLabelChanges() {
        var cut = Render<DocumentationSandbox>(parameters => parameters.Add(component => component.Component, _catalog.GetComponent("ntinputtext")!));
        cut.Find(".docs-generated-example input:not([type=hidden])").Change("Grace Hopper");
        cut.Find("#docs-sandbox-ntinputtext-label").Change("Updated label");

        cut.Find("output[aria-label='Current value']").TextContent.Should().Be("Grace Hopper");
        cut.Find(".docs-generated-example input:not([type=hidden])").GetAttribute("value").Should().Be("Grace Hopper");
        cut.Find(".docs-generated-example").TextContent.Should().Contain("Updated label");
    }

    [Fact]
    public void ClearingOptionalColumnOverride_RemovesTheExplicitSpan() {
        var cut = Render<DocumentationSandbox>(parameters => parameters.Add(component => component.Component, _catalog.GetComponent("ntformfieldlayoutspan")!));
        cut.Find("#docs-sandbox-ntformfieldlayoutspan-smallcolumns").Change("4");
        cut.Find(".docs-generated-example .nt-form-field-layout-span").GetAttribute("style").Should().Contain("--nt-form-field-layout-span-small:4");

        cut.Find("#docs-sandbox-ntformfieldlayoutspan-smallcolumns").Change(string.Empty);

        cut.Find(".docs-generated-example .nt-form-field-layout-span").GetAttribute("style").Should().BeNullOrEmpty();
        cut.FindAll(".docs-generated-example [role=alert]").Should().BeEmpty();
    }

    [Fact]
    public void RequiredIconCleared_ShowsTheSpecificError_AndAllowsCorrection() {
        var cut = Render<DocumentationSandbox>(parameters => parameters.Add(component => component.Component, _catalog.GetComponent("nticonbutton")!));
        cut.Find("#docs-sandbox-nticonbutton-icon").Change("None");
        cut.Find(".docs-generated-example [role=alert]").TextContent.Should().Contain("requires a non-null Icon");

        cut.Find("#docs-sandbox-nticonbutton-icon").Change("MaterialIcon.Add");

        cut.FindAll(".docs-generated-example [role=alert]").Should().BeEmpty();
        cut.Find(".docs-generated-example button").TextContent.Should().Contain(MaterialIcon.Add.Icon);
    }
}
#endif
