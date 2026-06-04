using System.Collections.Generic;
using Bunit;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;
using NTComponents.Core;

namespace NTComponents.Tests.Core;

/// <summary>
///     Unit tests for <see cref="NTComponentBase" />.
/// </summary>
public class NTComponentBaseTests : BunitContext {

    [Fact]
    public void Abstract_properties_are_required() {
        // Act
        var cut = Render<TestComponent>();

        // Assert
        cut.Instance.ElementClass.Should().Be("test-class");
        cut.Instance.ElementStyle.Should().Be("color:red;");
    }

    [Fact]
    public void Does_not_reallocate_AdditionalAttributes_if_tntid_is_correct() {
        // Arrange
        var cut = Render<TestComponent>();
        var attrs = cut.Instance.AdditionalAttributes;

        // Act
        cut = Render<TestComponent>(parameters => parameters
            .Add(p => p.AdditionalAttributes, attrs));

        // Assert
        cut.Instance.AdditionalAttributes.Should().NotBeNull();
        cut.Instance.AdditionalAttributes.Should().NotBeEquivalentTo(attrs);
    }

    [Fact]
    public void Parameters_are_settable() {
        // Act
        var cut = Render<TestComponent>(parameters => parameters
            .Add(p => p.ElementId, "myid")
            .Add(p => p.ElementLang, "en")
            .Add(p => p.ElementTitle, "title")
            .Add(p => p.AutoFocus, true));

        // Assert
        cut.Instance.ElementId.Should().Be("myid");
        cut.Instance.ElementLang.Should().Be("en");
        cut.Instance.ElementTitle.Should().Be("title");
        cut.Instance.AutoFocus.Should().BeTrue();
    }

    [Fact]
    public void Preserves_existing_AdditionalAttributes_and_adds_tntid() {
        // Arrange
        var attrs = new Dictionary<string, object> { { "data-test", "foo" } };

        // Act
        var cut = Render<TestComponent>(parameters => parameters
            .Add(p => p.AdditionalAttributes, attrs));

        // Assert
        cut.Instance.AdditionalAttributes!.Should().ContainKey("data-test");
        cut.Instance.AdditionalAttributes["data-test"].Should().Be("foo");
        cut.Instance.AdditionalAttributes.Should().ContainKey("tntid");
    }

    [Fact]
    public void Sets_tntid_in_AdditionalAttributes() {
        // Arrange & Act
        var cut = Render<TestComponent>();
        var tntid = cut.Instance.ComponentIdentifier;

        // Assert
        cut.Instance.AdditionalAttributes!.Should().ContainKey("tntid");
        cut.Instance.AdditionalAttributes["tntid"].Should().Be(tntid);
    }

    /// <summary>
    ///     Minimal concrete implementation for testing <see cref="NTComponentBase" />.
    /// </summary>
    private class TestComponent : TnTComponentBase {
        public override string? ElementClass => "test-class";
        public override string? ElementStyle => "color:red;";

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "id", ElementId);
            builder.AddAttribute(3, "lang", ElementLang);
            builder.AddAttribute(4, "title", ElementTitle);
            builder.AddAttribute(5, "class", ElementClass);
            builder.AddAttribute(6, "style", ElementStyle);
            builder.CloseElement();
        }
    }
}