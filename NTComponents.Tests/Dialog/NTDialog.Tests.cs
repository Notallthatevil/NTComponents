using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace NTComponents.Tests.Dialog;

public class NTDialog_Tests : BunitContext {
    private readonly BunitJSModuleInterop _module;

    public NTDialog_Tests() {
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        _module = JSInterop.SetupModule(NTDialog.JsModulePathValue);
        _module.SetupVoid("onLoad", _ => true).SetVoidResult();
        _module.SetupVoid("onUpdate", _ => true).SetVoidResult();
        _module.SetupVoid("onDispose", _ => true).SetVoidResult();
        _module.Setup<bool>("isOpen", _ => true).SetResult(false);
        _module.Setup<bool>("openDialogFromBlazor", _ => true).SetResult(true);
        _module.Setup<bool>("closeDialogFromBlazor", _ => true).SetResult(true);
    }

    [Fact]
    public void NTDialog_Renders_Native_Dialog_With_Title_Content_And_Default_Button() {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "delete-dialog")
            .Add(p => p.Title, "Delete item?")
            .Add(p => p.SupportingText, "This cannot be undone.")
            .Add(p => p.ChildContent, _ => builder => builder.AddMarkupContent(0, "<p>Dialog body</p>")));

        var dialog = component.Find("dialog.nt-dialog");
        dialog.GetAttribute("id").Should().Be("delete-dialog");
        dialog.ClassList.Should().Contain("nt-elevation-medium");
        dialog.HasAttribute("open").Should().BeFalse();
        dialog.GetAttribute("data-nt-dialog-close-on-backdrop").Should().Be("false");
        dialog.GetAttribute("aria-labelledby").Should().Be("delete-dialog-title");
        dialog.GetAttribute("aria-describedby").Should().Be("delete-dialog-supporting-text");
        component.Find(".nt-dialog-title").TextContent.Should().Be("Delete item?");
        component.Find(".nt-dialog-supporting-text").TextContent.Should().Be("This cannot be undone.");
        component.Find(".nt-dialog-content").InnerHtml.Should().Contain("Dialog body");
        var defaultButton = component.Find(".nt-dialog-actions .nt-dialog-button");
        defaultButton.GetAttribute("command").Should().Be("request-close");
        defaultButton.GetAttribute("commandfor").Should().Be("delete-dialog");
    }

    [Fact]
    public void NTDialog_Defers_ChildContent_When_Renderer_Is_Interactive() {
        var childRenderCount = 0;

        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "deferred-dialog")
            .Add(p => p.ChildContent, _ => builder => {
                childRenderCount++;
                builder.AddContent(0, "Deferred body");
            }));

        component.Markup.Should().NotContain("Deferred body");
        component.FindAll(".nt-dialog-content").Should().BeEmpty();
        childRenderCount.Should().Be(0);
    }

    [Fact]
    public void NTDialog_Renders_ChildContent_Immediately_When_Renderer_Is_Not_Interactive() {
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "static-dialog")
            .Add(p => p.ChildContent, _ => builder => builder.AddContent(0, "Static body")));

        component.Markup.Should().Contain("Static body");
    }

    [Fact]
    public async Task OpenAsync_With_Parameters_Renders_ChildContent_And_Opens_Dialog() {
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "parameter-dialog")
            .Add(p => p.ChildContent, dialogParameters => builder => builder.AddContent(0, $"Record {dialogParameters!.Get<int>("RecordId")}")));

        var opened = await component.Instance.OpenAsync(new Dictionary<string, object?> {
            ["RecordId"] = 42
        }, Xunit.TestContext.Current.CancellationToken);

        opened.Should().BeTrue();
        component.Markup.Should().Contain("Record 42");
        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "openDialogFromBlazor");
    }

    [Fact]
    public async Task NotifyClosedFromJavaScript_Removes_Deferred_ChildContent_When_Renderer_Is_Interactive() {
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "close-removes-content-dialog")
            .Add(p => p.ChildContent, _ => builder => builder.AddContent(0, "Temporary body")));

        await component.Instance.OpenAsync(Xunit.TestContext.Current.CancellationToken);
        component.Markup.Should().Contain("Temporary body");

        await component.Instance.NotifyClosedFromJavaScript(null);

        component.Markup.Should().NotContain("Temporary body");
    }

    [Fact]
    public async Task RefreshAsync_Recreates_ChildContent_With_Updated_Parameters() {
        var initializedCount = 0;
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "refresh-dialog")
            .Add(p => p.ChildContent, dialogParameters => builder => {
                builder.OpenComponent<RefreshableDialogChild>(0);
                dialogParameters!.TryGet<int>("RecordId", out var recordId).Should().BeTrue();
                builder.AddComponentParameter(1, nameof(RefreshableDialogChild.OnInitializedCallback), (Action)(() => initializedCount++));
                builder.AddComponentParameter(2, nameof(RefreshableDialogChild.Value), recordId);
                builder.CloseComponent();
            }));

        await component.Instance.OpenAsync(new Dictionary<string, object?> {
            ["RecordId"] = 42
        }, Xunit.TestContext.Current.CancellationToken);

        initializedCount.Should().Be(1);
        component.Markup.Should().Contain("Record 42");

        await component.Instance.RefreshAsync(new Dictionary<string, object?> {
            ["RecordId"] = 84
        }, Xunit.TestContext.Current.CancellationToken);

        initializedCount.Should().Be(2);
        component.Markup.Should().Contain("Record 84");
    }

    [Fact]
    public void NTDialog_Renders_Open_Attribute_Only_When_Open_Is_True() {
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "open-attribute-dialog")
            .Add(p => p.Open, true));

        component.Find("dialog").GetAttribute("open").Should().Be("open");
    }

    [Fact]
    public void NTDialog_Renders_Configured_Elevation_Class() {
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "elevation-dialog")
            .Add(p => p.Elevation, NTElevation.High));

        component.Find("dialog").ClassList.Should().Contain("nt-elevation-high");
    }

    [Fact]
    public void NTDialog_Renders_Custom_Button_Fragment() {
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "custom-dialog")
            .Add(p => p.Buttons, builder => {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "class", "confirm-action");
                builder.AddAttribute(2, "command", "request-close");
                builder.AddAttribute(3, "commandfor", "custom-dialog");
                builder.AddContent(4, "Confirm");
                builder.CloseElement();
            }));

        component.Find(".confirm-action").TextContent.Should().Be("Confirm");
        component.FindAll(".nt-dialog-actions .nt-dialog-button").Should().BeEmpty();
    }

    [Fact]
    public void NTDialog_Renders_Icon_Fragment() {
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "icon-dialog")
            .Add(p => p.Title, "Icon dialog")
            .Add(p => p.Icon, builder => {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "class", "material-symbols-sharp");
                builder.AddContent(2, "info");
                builder.CloseElement();
            }));

        var dialog = component.Find("dialog.nt-dialog");
        dialog.ClassList.Should().Contain("nt-dialog-has-icon");
        component.Find(".nt-dialog-icon").GetAttribute("aria-hidden").Should().Be("true");
        component.Find(".nt-dialog-icon .material-symbols-sharp").TextContent.Should().Be("info");
    }

    [Fact]
    public async Task OpenAsync_Invokes_Open_Lifecycle_And_Javascript() {
        var events = new List<string>();
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "open-dialog")
            .Add(p => p.OnOpen, EventCallback.Factory.Create<NTDialogEventArgs>(this, args => events.Add($"open:{args.DialogId}")))
            .Add(p => p.OnOpening, EventCallback.Factory.Create<NTDialogEventArgs>(this, args => events.Add($"opening:{args.DialogId}")))
            .Add(p => p.OnOpened, EventCallback.Factory.Create<NTDialogEventArgs>(this, args => events.Add($"opened:{args.DialogId}"))));

        var opened = await component.Instance.OpenAsync(Xunit.TestContext.Current.CancellationToken);

        opened.Should().BeTrue();
        events.Should().Equal("open:open-dialog", "opening:open-dialog", "opened:open-dialog");
        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "openDialogFromBlazor");
    }

    [Fact]
    public async Task OpenAsync_Can_Be_Cancelled_By_OnOpen() {
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.OnOpen, EventCallback.Factory.Create<NTDialogEventArgs>(this, args => args.Cancel = true)));

        var opened = await component.Instance.OpenAsync(Xunit.TestContext.Current.CancellationToken);

        opened.Should().BeFalse();
        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "openDialogFromBlazor");
    }

    [Fact]
    public async Task CloseAsync_Invokes_Close_Lifecycle_And_Javascript() {
        _module.Setup<bool>("isOpen", _ => true).SetResult(true);
        var events = new List<string>();
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.Id, "close-dialog")
            .Add(p => p.OnClose, EventCallback.Factory.Create<NTDialogEventArgs>(this, args => events.Add($"close:{args.ReturnValue}")))
            .Add(p => p.OnClosing, EventCallback.Factory.Create<NTDialogEventArgs>(this, args => events.Add($"closing:{args.ReturnValue}"))));

        var closed = await component.Instance.CloseAsync("done", Xunit.TestContext.Current.CancellationToken);

        closed.Should().BeTrue();
        events.Should().Equal("close:done", "closing:done");
        JSInterop.Invocations.Should().Contain(invocation => invocation.Identifier == "closeDialogFromBlazor");
    }

    [Fact]
    public async Task CloseAsync_Can_Be_Cancelled_By_OnClose() {
        _module.Setup<bool>("isOpen", _ => true).SetResult(true);
        var component = Render<NTDialog>(parameters => parameters
            .Add(p => p.OnClose, EventCallback.Factory.Create<NTDialogEventArgs>(this, args => args.Cancel = true)));

        var closed = await component.Instance.CloseAsync(cancellationToken: Xunit.TestContext.Current.CancellationToken);

        closed.Should().BeFalse();
        JSInterop.Invocations.Should().NotContain(invocation => invocation.Identifier == "closeDialogFromBlazor");
    }

    private sealed class RefreshableDialogChild : ComponentBase {
        [Parameter]
        public Action? OnInitializedCallback { get; set; }

        [Parameter]
        public object? Value { get; set; }

        protected override void OnInitialized() {
            OnInitializedCallback?.Invoke();
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.AddContent(0, $"Record {Value}");
        }
    }
}
