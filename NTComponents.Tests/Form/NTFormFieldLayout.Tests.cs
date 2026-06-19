using Microsoft.AspNetCore.Components;

namespace NTComponents.Tests.Form;

public class NTFormFieldLayout_Tests : BunitContext {

    [Fact]
    public void FieldGridView_Renders_Default_Classes_And_Gap_Variables() {
        var cut = Render<NTFormFieldGridView>(parameters => parameters
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        var grid = cut.Find("div.nt-form-field-grid-view");
        grid.ClassList.Should().Contain("nt-form-field-grid-view-max-3");
        grid.GetAttribute("style").Should().Contain("--nt-form-field-grid-view-column-gap:16px");
        grid.GetAttribute("style").Should().Contain("--nt-form-field-grid-view-row-gap:16px");
    }

    [Theory]
    [InlineData(-1, "nt-form-field-grid-view-max-1")]
    [InlineData(1, "nt-form-field-grid-view-max-1")]
    [InlineData(2, "nt-form-field-grid-view-max-2")]
    [InlineData(3, "nt-form-field-grid-view-max-3")]
    [InlineData(5, "nt-form-field-grid-view-max-4")]
    [InlineData(9, "nt-form-field-grid-view-max-6")]
    public void FieldGridView_Normalizes_MaxColumns(int maxColumns, string expectedClass) {
        var cut = Render<NTFormFieldGridView>(parameters => parameters
            .Add(p => p.MaxColumns, maxColumns)
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        cut.Find("div.nt-form-field-grid-view").ClassList.Should().Contain(expectedClass);
    }

    [Fact]
    public void LayoutSpan_Auto_Defers_To_Parent_Grid_Defaults() {
        var cut = Render<NTFormFieldLayoutSpan>(parameters => parameters
            .Add(p => p.Span, NTFormFieldSpan.Auto)
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        var item = cut.Find("div.nt-form-field-layout-span");
        item.ClassList.Should().Contain("nt-form-field-layout-span-auto");
        item.HasAttribute("style").Should().BeFalse();
    }

    [Fact]
    public void LayoutSpan_Auto_Explicit_Columns_Override_Only_Provided_Breakpoints() {
        var cut = Render<NTFormFieldLayoutSpan>(parameters => parameters
            .Add(p => p.Span, NTFormFieldSpan.Auto)
            .Add(p => p.LargeColumns, 99)
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        var style = cut.Find("div.nt-form-field-layout-span").GetAttribute("style");
        style.Should().NotContain("--nt-form-field-layout-span-small");
        style.Should().NotContain("--nt-form-field-layout-span-medium");
        style.Should().Contain("--nt-form-field-layout-span-large:12");
    }

    [Fact]
    public void LayoutSpan_Emits_Preset_Span_Variables() {
        var cut = Render<NTFormFieldLayoutSpan>(parameters => parameters
            .Add(p => p.Span, NTFormFieldSpan.Full)
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        var item = cut.Find("div.nt-form-field-layout-span");
        item.ClassList.Should().Contain("nt-form-field-layout-span-full");
        item.GetAttribute("style").Should().Contain("--nt-form-field-layout-span-small:12");
        item.GetAttribute("style").Should().Contain("--nt-form-field-layout-span-medium:12");
        item.GetAttribute("style").Should().Contain("--nt-form-field-layout-span-large:12");
    }

    [Fact]
    public void LayoutSpan_Explicit_Columns_Override_Preset_And_Clamp_To_Grid() {
        var cut = Render<NTFormFieldLayoutSpan>(parameters => parameters
            .Add(p => p.Span, NTFormFieldSpan.Full)
            .Add(p => p.SmallColumns, 0)
            .Add(p => p.MediumColumns, 5)
            .Add(p => p.LargeColumns, 99)
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        var style = cut.Find("div.nt-form-field-layout-span").GetAttribute("style");
        style.Should().Contain("--nt-form-field-layout-span-small:1");
        style.Should().Contain("--nt-form-field-layout-span-medium:5");
        style.Should().Contain("--nt-form-field-layout-span-large:12");
    }

    [Fact]
    public void SectionView_Renders_Section_With_Header_Description_And_Grid() {
        var cut = Render<NTFormSectionView>(parameters => parameters
            .Add(p => p.ElementId, "contact-section")
            .Add(p => p.Heading, "Contact")
            .Add(p => p.Description, "Primary contact information")
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        var section = cut.Find("section.nt-form-section-view");
        section.GetAttribute("aria-labelledby").Should().Be("contact-section-heading");
        section.GetAttribute("aria-describedby").Should().Be("contact-section-description");
        cut.Find("h2#contact-section-heading").TextContent.Should().Be("Contact");
        cut.Find("p#contact-section-description").TextContent.Should().Be("Primary contact information");
        cut.Find("div.nt-form-field-grid-view").Should().NotBeNull();
    }

    [Fact]
    public void SectionView_Renders_Fieldset_With_Legend() {
        var cut = Render<NTFormSectionView>(parameters => parameters
            .Add(p => p.ElementId, "preferences")
            .Add(p => p.Heading, "Preferences")
            .Add(p => p.Description, "Choose communication preferences")
            .Add(p => p.UseFieldset, true)
            .Add(p => p.ChildContent, builder => {
                builder.OpenElement(0, "input");
                builder.CloseElement();
            }));

        var fieldset = cut.Find("fieldset.nt-form-section-view");
        fieldset.GetAttribute("aria-describedby").Should().Be("preferences-description");
        cut.Find("legend#preferences-heading").TextContent.Should().Be("Preferences");
        cut.Find("p#preferences-description").TextContent.Should().Be("Choose communication preferences");
    }

}
