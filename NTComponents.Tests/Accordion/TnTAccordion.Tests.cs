using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Reflection;
using RippleTestingUtility = NTComponents.Tests.TestingUtility.TestingUtility;

namespace NTComponents.Tests.Accordion;

public class TnTAccordion_Tests : BunitContext {
    private const string JsModulePath = "./_content/NTComponents/Accordion/TnTAccordion.razor.js";

    public TnTAccordion_Tests() {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        var module = JSInterop.SetupModule(JsModulePath);
        module.SetupVoid("onLoad", _ => true).SetVoidResult();
        module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        module.SetupVoid("onDispose", _ => true).SetVoidResult();
        RippleTestingUtility.SetupRippleEffectModule(this);
    }

    [Fact]
    public void Adds_LimitOneExpanded_Class_When_Enabled() {
        // Arrange Act
        var cut = Render<TnTAccordion>(p => p.Add(c => c.LimitToOneExpanded, true).AddChildContent(Children(Child(b => b.AddAttribute(1, "Label", "A")))));
        // Assert
        cut.Markup.Should().Contain("tnt-limit-one-expanded");
    }

    [Fact]
    public void Child_ContentStyle_Includes_Custom_Color_Variable() {
        // Arrange / Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, "ContentBodyColor", TnTColor.Primary); }))));
        // Assert
        cut.Find(".tnt-accordion-child").GetAttribute("style")!.Should().Contain("--tnt-accordion-child-content-bg-color");
    }

    [Fact]
    public async Task Child_Dispose_Removes_From_Dom() {
        // Arrange
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => b.AddAttribute(1, "Label", "First")),
            Child(b => b.AddAttribute(1, "Label", "Second"))
        )));
        var second = cut.FindComponents<TnTAccordionChild>().Last();
        // Act
        await cut.InvokeAsync(() => second.Instance.Dispose());
        cut.Render();
        // Assert
        cut.Markup.Should().NotContain("Second");
    }

    [Fact]
    public void Child_EnableRipple_False_Renders_No_Ripple_Component() {
        // Arrange / Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, "EnableRipple", false); }))));
        // Assert
        cut.Markup.Should().NotContain("tnt-ripple-effect");
    }

    [Fact]
    public void Initially_Closed_Child_Does_Not_Render_Collapsed_Class() {
        // Arrange / Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => b.AddAttribute(1, "Label", "A")))));

        // Assert
        var button = cut.Find("button[data-accordion-header='true']");
        var content = cut.Find("[data-accordion-content='true']");
        button.GetAttribute("aria-expanded").Should().Be("false");
        content.ClassList.Should().NotContain("tnt-collapsed");
        content.ClassList.Should().NotContain("tnt-expanded");
        content.GetAttribute("aria-hidden").Should().Be("true");
    }

    [Fact]
    public void Initially_Closed_RemoveContentOnClose_Keeps_Content_In_Dom() {
        // Arrange / Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => {
                b.AddAttribute(1, "Label", "A");
                b.AddAttribute(2, nameof(TnTAccordionChild.RemoveContentOnClose), true);
                b.AddAttribute(3, nameof(TnTAccordionChild.ChildContent), (RenderFragment)(contentBuilder => contentBuilder.AddContent(0, "Body")));
            }))));

        // Assert
        cut.Find("[data-accordion-content='true']").TextContent.Should().Contain("Body");
    }

    [Fact]
    public async Task LimitToOneExpanded_Opening_Second_Child_Closes_First_And_Invokes_Callbacks() {
        // Arrange
        var opened = 0;
        var closed = 0;
        var cut = Render<TnTAccordion>(p => p.Add(c => c.LimitToOneExpanded, true)
            .AddChildContent(Children(
                Child(b => {
                    b.AddAttribute(1, "Label", "A");
                    b.AddAttribute(2, "OpenByDefault", true);
                    b.AddAttribute(3, nameof(TnTAccordionChild.OnCloseCallback), EventCallback.Factory.Create(this, () => closed++));
                }),
                Child(b => {
                    b.AddAttribute(1, "Label", "B");
                    b.AddAttribute(2, nameof(TnTAccordionChild.OnOpenCallback), EventCallback.Factory.Create(this, () => opened++));
                })
            )));
        var second = cut.FindComponents<TnTAccordionChild>().Last().Instance;

        // Act
        await cut.Instance.SetAsOpened(GetChildElementId(second));
        cut.Render();

        // Assert
        cut.FindAll("[data-accordion-content='true'].tnt-expanded").Should().HaveCount(1);
        cut.Markup.Should().Contain("aria-expanded=\"false\"");
        cut.Markup.Should().Contain("aria-expanded=\"true\"");
        opened.Should().Be(1);
        closed.Should().Be(1);
    }

    [Fact]
    public void Multiple_OpenByDefault_When_Limited_Only_First_Expanded() {
        // Arrange Act
        var cut = Render<TnTAccordion>(p => p.Add(c => c.LimitToOneExpanded, true)
            .AddChildContent(Children(
                Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, "OpenByDefault", true); }),
                Child(b => { b.AddAttribute(1, "Label", "B"); b.AddAttribute(2, "OpenByDefault", true); })
            )));
        // Assert
        cut.FindAll(".tnt-expanded").Count.Should().Be(1);
    }

    [Fact]
    public void Multiple_OpenByDefault_When_Not_Limited_All_Expanded() {
        // Arrange Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, "OpenByDefault", true); }),
            Child(b => { b.AddAttribute(1, "Label", "B"); b.AddAttribute(2, "OpenByDefault", true); })
        )));
        // Assert
        cut.FindAll(".tnt-expanded").Count.Should().Be(2);
    }

    [Fact]
    public void Orders_Children_By_Order_Property() {
        // Arrange Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "Second"); b.AddAttribute(2, "Order", 20); }),
            Child(b => { b.AddAttribute(1, "Label", "First"); b.AddAttribute(2, "Order", 10); })
        )));
        // Assert
        var firstIndex = cut.Markup.IndexOf("First", StringComparison.Ordinal);
        var secondIndex = cut.Markup.IndexOf("Second", StringComparison.Ordinal);
        (firstIndex < secondIndex).Should().BeTrue();
    }

    [Fact]
    public void Renders_Accordion_Base_Class() {
        // Arrange Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(Child(b => b.AddAttribute(1, "Label", "A")))));
        // Assert
        cut.Markup.Should().Contain("tnt-accordion");
    }

    [Fact]
    public void Renders_Child_Labels() {
        // Arrange Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => b.AddAttribute(1, "Label", "First")),
            Child(b => b.AddAttribute(1, "Label", "Second")))));
        // Assert
        cut.Markup.Should().Contain("First");
    }

    [Fact]
    public void Renders_Second_Child_Label() {
        // Arrange Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => b.AddAttribute(1, "Label", "First")),
            Child(b => b.AddAttribute(1, "Label", "Second")))));
        // Assert
        cut.Markup.Should().Contain("Second");
    }

    [Fact]
    public void Renders_Button_Header_With_Aria_Wiring() {
        // Arrange / Act
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, "OpenByDefault", true); }))));

        // Assert
        var button = cut.Find("button[data-accordion-header='true']");
        var content = cut.Find("[data-accordion-content='true']");
        button.GetAttribute("aria-controls").Should().Be(content.Id);
        button.GetAttribute("aria-expanded").Should().Be("true");
        content.GetAttribute("aria-labelledby").Should().Be(button.Id);
        content.GetAttribute("aria-hidden").Should().Be("false");
    }

    [Fact]
    public async Task SetAsClosed_Does_Not_Invoke_OnClose_When_Already_Closed() {
        // Arrange
        var closed = 0;
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, nameof(TnTAccordionChild.OnCloseCallback), EventCallback.Factory.Create(this, () => closed++)); }))));
        var child = cut.FindComponents<TnTAccordionChild>().Single().Instance;
        // Act
        await cut.Instance.SetAsClosed(GetChildElementId(child));
        cut.Render();
        // Assert
        closed.Should().Be(0);
    }

    [Fact]
    public async Task SetAsClosed_Invokes_OnCloseCallback_When_Open() {
        // Arrange
        var closed = 0;
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, "OpenByDefault", true); b.AddAttribute(3, nameof(TnTAccordionChild.OnCloseCallback), EventCallback.Factory.Create(this, () => closed++)); }))));
        var child = cut.FindComponents<TnTAccordionChild>().Single().Instance;
        // Act
        await cut.Instance.SetAsClosed(GetChildElementId(child));
        cut.Render();
        // Assert
        closed.Should().Be(1);
    }

    [Fact]
    public async Task SetAsOpened_Does_Not_Invoke_OnOpen_When_Already_Open() {
        // Arrange
        var opened = 0;
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, "OpenByDefault", true); b.AddAttribute(3, nameof(TnTAccordionChild.OnOpenCallback), EventCallback.Factory.Create(this, () => opened++)); }))));
        var child = cut.FindComponents<TnTAccordionChild>().Single().Instance;
        // Act
        await cut.Instance.SetAsOpened(GetChildElementId(child));
        cut.Render();
        // Assert
        opened.Should().Be(0);
    }

    [Fact]
    public async Task SetAsOpened_Invokes_OnOpenCallback_When_Closed() {
        // Arrange
        var opened = 0;
        var cut = Render<TnTAccordion>(p => p.AddChildContent(Children(
            Child(b => { b.AddAttribute(1, "Label", "A"); b.AddAttribute(2, nameof(TnTAccordionChild.OnOpenCallback), EventCallback.Factory.Create(this, () => opened++)); }))));
        var child = cut.FindComponents<TnTAccordionChild>().Single().Instance;
        // Act
        await cut.Instance.SetAsOpened(GetChildElementId(child));
        cut.Render();
        // Assert
        opened.Should().Be(1);
    }

    private static int GetChildElementId(TnTAccordionChild child) =>
        (int)typeof(TnTAccordionChild).GetField("_elementId", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(child)!;

    private static Action<RenderTreeBuilder> Child(Action<RenderTreeBuilder> a) => b => { b.OpenComponent<TnTAccordionChild>(0); a(b); b.CloseComponent(); };

    private static RenderFragment Children(params Action<RenderTreeBuilder>[] children) => b => { foreach (var c in children) c(b); };
}
