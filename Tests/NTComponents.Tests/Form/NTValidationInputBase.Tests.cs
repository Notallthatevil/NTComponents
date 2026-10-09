using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using System.Reflection;

namespace NTComponents.Tests.Form;

public class NTValidationInputBase_Tests : BunitContext {
    private sealed class Model<TValue> {
        public TValue Value { get; set; } = default!;
        public string? Other { get; set; }
    }

    private sealed class CountingLegacySelect : NTInputSelect<string?> {
        public int OwnRenderCount { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            OwnRenderCount++;
            base.BuildRenderTree(builder);
        }
    }

    private sealed class LocalStateInput : NTInputText {
        private string _status = "Ready";

        public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        [Parameter]
        public bool AwaitParameters { get; set; }

        public void UpdateStatus() {
            _status = "Updated";
            StateHasChanged();
        }

        public void UpdateStatusUsingFrameworkRender() {
            _status = "Updated";
            // Older consumer assemblies call ComponentBase's method directly, bypassing a newly hidden method.
            typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(this, null);
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder) {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, UpdateStatusAsync));
            builder.AddContent(2, _status);
            builder.CloseElement();
        }

        private async Task UpdateStatusAsync() {
            _status = "Waiting";
            await Completion.Task;
            _status = "Completed";
        }

        protected override async Task OnParametersSetAsync() {
            await base.OnParametersSetAsync();
            if (AwaitParameters) {
                await UpdateStatusAsync();
            }
        }
    }

    public NTValidationInputBase_Tests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("Server", true));
    }

    [Fact]
    public async Task ValidationChanged_ConcreteInputs_RenderOnlyForTheirOwnMessageChanges() {
        await VerifyMessageRendering<NTInputText, string?>("Original");
        await VerifyMessageRendering<NTTextArea, string?>("Original");
        await VerifyMessageRendering<NTInputColor, string?>("#123456");
        await VerifyMessageRendering<NTInputNumeric<int>, int?>(1);
        await VerifyMessageRendering<NTInputCurrency, decimal?>(1m);
        await VerifyMessageRendering<NTInputDateTime<DateOnly?>, DateOnly?>(new DateOnly(2026, 10, 9));
        await VerifyMessageRendering<NTInputCheckbox, bool>(false);
        await VerifyMessageRendering<NTInputSwitch, bool>(false);
        await VerifyMessageRendering<NTInputSlider<int>, int>(25);
        await VerifyMessageRendering<NTInputRangeSlider<int>, NTSliderRange<int>>(new(25, 75));
        await VerifyMessageRendering<NTInputRadioGroup<string?>, string?>("Original");
        await VerifyMessageRendering<NTSelect<string?>, string?>("Original");
        await VerifyMessageRendering<NTCombobox<string>, IReadOnlyList<string>>([]);
        await VerifyMessageRendering<NTAutocomplete, string?>("Original");
        await VerifyMessageRendering<NTTypeahead<string>, string?>("Original");
        await VerifyMessageRendering<NTFileUpload, IReadOnlyList<IBrowserFile>?>([]);
    }

    [Fact]
    public async Task ValidationChanged_LegacySelect_RendersOnlyForItsOwnMessageChanges() {
        var model = new Model<string?> { Value = "Original" };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var field = new FieldIdentifier(model, nameof(model.Value));
        var cut = RenderInput<CountingLegacySelect, string?>(model, editContext);
        var renderCount = cut.Instance.OwnRenderCount;

        await cut.InvokeAsync(() => {
            messages.Add(new FieldIdentifier(model, nameof(model.Other)), "Unrelated error");
            editContext.NotifyValidationStateChanged();
        });
        cut.Instance.OwnRenderCount.Should().Be(renderCount);

        await cut.InvokeAsync(() => {
            messages.Add(field, "Own error");
            editContext.NotifyValidationStateChanged();
        });
        cut.Instance.OwnRenderCount.Should().Be(renderCount + 1);
        cut.Find(".tnt-validation-message").TextContent.Should().Be("Own error");

        await cut.InvokeAsync(editContext.NotifyValidationStateChanged);
        cut.Instance.OwnRenderCount.Should().Be(renderCount + 1);

        await cut.InvokeAsync(() => {
            messages.Clear(field);
            editContext.NotifyValidationStateChanged();
        });
        cut.Instance.OwnRenderCount.Should().Be(renderCount + 2);
        cut.FindAll(".tnt-validation-message:not([hidden])").Should().BeEmpty();
    }

    [Fact]
    public async Task AdditionalMessages_ChangedOrReordered_RenderEvenWhenFirstMessageIsUnchanged() {
        var model = new Model<string?>();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var field = new FieldIdentifier(model, nameof(model.Value));
        var cut = RenderInput<NTInputText, string?>(model, editContext);
        await cut.InvokeAsync(() => {
            messages.Add(field, ["First", "Second", "Third"]);
            editContext.NotifyValidationStateChanged();
        });
        var renderCount = cut.RenderCount;

        await cut.InvokeAsync(() => {
            messages.Clear(field);
            messages.Add(field, ["First", "Replaced", "Third"]);
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount + 1);

        await cut.InvokeAsync(() => {
            messages.Clear(field);
            messages.Add(field, ["First", "Third", "Replaced"]);
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount + 2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task SingleFieldEdit_UnrelatedInputRenderCount_DoesNotGrowWithPageSize(int inputCount) {
        var editContext = new EditContext(new object());
        var messages = new ValidationMessageStore(editContext);
        var fields = Enumerable.Range(0, inputCount).Select(_ => new Model<string?>()).ToArray();
        var inputs = fields.Select(model => RenderInput<NTInputText, string?>(model, editContext)).ToArray();
        var renderCounts = inputs.Select(input => input.RenderCount).ToArray();

        await inputs[0].InvokeAsync(() => {
            messages.Add(new FieldIdentifier(fields[0], nameof(Model<string?>.Value)), "Changed field");
            editContext.NotifyValidationStateChanged();
        });

        inputs[0].RenderCount.Should().Be(renderCounts[0] + 1);
        inputs.Skip(1).Select((input, index) => input.RenderCount - renderCounts[index + 1]).Should().OnlyContain(count => count == 0);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void NativeValueEdit_UnrelatedFieldCssAndRenders_DoNotChange(int inputCount) {
        var editContext = new EditContext(new object());
        editContext.OnFieldChanged += (_, _) => editContext.NotifyValidationStateChanged();
        var fields = Enumerable.Range(0, inputCount).Select(_ => new Model<string?>()).ToArray();
        var inputs = fields.Select(model => RenderInput<NTInputText, string?>(model, editContext)).ToArray();
        var renderCounts = inputs.Select(input => input.RenderCount).ToArray();

        inputs[0].Find("input").Change("Edited");

        fields[0].Value.Should().Be("Edited");
        inputs[0].Find(".nt-input").ClassList.Should().Contain("nt-modified");
        inputs.Skip(1).Select((input, index) => input.RenderCount - renderCounts[index + 1]).Should().OnlyContain(count => count == 0);
        inputs.Skip(1).Should().OnlyContain(input => !input.Find(".nt-input").ClassList.Contains("nt-modified"));
    }

    [Fact]
    public async Task DisposedConcreteInputs_ReleaseValidationSubscriptions() {
        await VerifyDisposedSubscriptions<NTInputText, string?>("Original");
        await VerifyDisposedSubscriptions<NTInputCheckbox, bool>(false);
        await VerifyDisposedSubscriptions<NTInputSwitch, bool>(false);
        await VerifyDisposedSubscriptions<NTInputRadioGroup<string?>, string?>("Original");
        await VerifyDisposedSubscriptions<NTTextArea, string?>("Original");
        await VerifyDisposedSubscriptions<NTCombobox<string>, IReadOnlyList<string>>([]);
        await VerifyDisposedSubscriptions<NTAutocomplete, string?>("Original");
        await VerifyDisposedSubscriptions<NTTypeahead<string>, string?>("Original");
        await VerifyDisposedSubscriptions<NTInputSelect<string?>, string?>("Original");
    }

    [Fact]
    public void UnchangedMessages_ParameterAndNativeValueChanges_StillRender() {
        var model = new Model<string?> { Value = "Original" };
        var editContext = new EditContext(model);
        var cut = RenderInput<NTInputText, string?>(model, editContext);

        cut.Find("input").Change("Edited");
        model.Value.Should().Be("Edited");
        cut.Find("input").GetAttribute("value").Should().Be("Edited");

        cut.Render(parameters => parameters.Add(p => p.Value, "From parent").Add(p => p.Label, "Updated label"));
        cut.Find("input").GetAttribute("value").Should().Be("From parent");
        cut.Find(".nt-input-label").TextContent.Should().Be("Updated label");
    }

    [Fact]
    public async Task UnchangedMessages_LocalStateChange_StillRenders() {
        var model = new Model<string?>();
        var cut = RenderInput<LocalStateInput, string?>(model, new EditContext(model));

        await cut.InvokeAsync(cut.Instance.UpdateStatus);

        cut.Find("button").TextContent.Should().Be("Updated");
    }

    [Fact]
    public async Task UnchangedMessages_FrameworkRenderRequest_StillUpdatesLocalState() {
        var model = new Model<string?>();
        var cut = RenderInput<LocalStateInput, string?>(model, new EditContext(model));

        await cut.InvokeAsync(cut.Instance.UpdateStatusUsingFrameworkRender);

        cut.Find("button").TextContent.Should().Be("Updated");
    }

    [Fact]
    public async Task UnchangedError_Submit_UpdatesModifiedCss() {
        var model = new Model<string?>();
        var editContext = new EditContext(model);
        var field = new FieldIdentifier(model, nameof(model.Value));
        var messages = new ValidationMessageStore(editContext);
        messages.Add(field, "Existing error");
        var cut = RenderInput<NTInputText, string?>(model, editContext);
        cut.Find(".nt-input").ClassList.Should().Contain("nt-invalid").And.NotContain("nt-modified");

        await cut.InvokeAsync(() => {
            editContext.Validate();
            editContext.NotifyValidationStateChanged();
        });

        cut.Find(".nt-input").ClassList.Should().Contain("nt-modified").And.Contain("nt-invalid");
        editContext.GetValidationMessages(field).Should().Equal("Existing error");
    }

    [Fact]
    public async Task UnchangedMessages_ModifiedStateReset_UpdatesFieldCss() {
        var model = new Model<string?> { Value = "Original" };
        var editContext = new EditContext(model);
        var cut = RenderInput<NTInputText, string?>(model, editContext);
        cut.Find("input").Change("Edited");
        cut.Find(".nt-input").ClassList.Should().Contain("nt-modified");

        await cut.InvokeAsync(() => {
            editContext.MarkAsUnmodified(new FieldIdentifier(model, nameof(model.Value)));
            editContext.NotifyValidationStateChanged();
        });

        cut.Find(".nt-input").ClassList.Should().NotContain("nt-modified").And.Contain("nt-valid");
        cut.Find("input").GetAttribute("value").Should().Be("Edited");
    }

    [Fact]
    public async Task UnchangedMessages_AsyncEvent_RendersBeforeAndAfterCompletion() {
        var model = new Model<string?>();
        var cut = RenderInput<LocalStateInput, string?>(model, new EditContext(model));

        var click = cut.Find("button").ClickAsync(new());
        cut.Find("button").TextContent.Should().Be("Waiting");
        cut.Instance.Completion.SetResult();
        await click;

        cut.Find("button").TextContent.Should().Be("Completed");
    }

    [Fact]
    public async Task UnchangedMessages_AsyncParameters_RenderAfterCompletion() {
        var model = new Model<string?>();
        var cut = Render<LocalStateInput>(parameters => parameters
            .Add(p => p.ValueExpression, () => model.Value)
            .Add(p => p.AwaitParameters, true)
            .AddCascadingValue(new EditContext(model)));
        cut.Find("button").TextContent.Should().Be("Waiting");

        await cut.InvokeAsync(() => cut.Instance.Completion.SetResult());

        cut.WaitForAssertion(() => cut.Find("button").TextContent.Should().Be("Completed"));
    }

    [Fact]
    public async Task CanceledAsyncEvent_DoesNotRenderCompletionOrThrow() {
        var model = new Model<string?>();
        var cut = RenderInput<LocalStateInput, string?>(model, new EditContext(model));
        var click = cut.Find("button").ClickAsync(new());
        var renderCount = cut.RenderCount;

        cut.Instance.Completion.SetCanceled(Xunit.TestContext.Current.CancellationToken);
        await click;

        cut.RenderCount.Should().Be(renderCount);
        cut.Find("button").TextContent.Should().Be("Waiting");
    }

    [Fact]
    public async Task FailedAsyncEvent_PropagatesConsumerException() {
        var model = new Model<string?>();
        var cut = RenderInput<LocalStateInput, string?>(model, new EditContext(model));
        var exception = new InvalidOperationException("Consumer failed.");
        var eventTask = Task.CompletedTask;
        await cut.InvokeAsync(() => {
            eventTask = ((IHandleEvent)cut.Instance).HandleEventAsync(new EventCallbackWorkItem((Func<Task>)(() => cut.Instance.Completion.Task)), null);
        });

        cut.Instance.Completion.SetException(exception);

        var act = async () => await eventTask;
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(exception);
    }

    [Fact]
    public async Task FocusedNativeDate_MessageChange_WaitsForBlurWithoutLosingValidation() {
        var model = new Model<DateOnly?> { Value = new DateOnly(2026, 10, 9) };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var cut = RenderInput<NTInputDateTime<DateOnly?>, DateOnly?>(model, editContext);
        cut.Render(parameters => parameters.Add(p => p.BindOnInput, true));
        cut.Find("input").Focus();
        var renderCount = cut.RenderCount;

        cut.Find("input").Input("0002-10-09");
        model.Value.Should().Be(new DateOnly(2, 10, 9));
        await cut.InvokeAsync(() => {
            messages.Add(new FieldIdentifier(model, nameof(model.Value)), "Finish entering the year.");
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount);

        cut.Find("input").Blur();

        cut.RenderCount.Should().BeGreaterThan(renderCount);
        cut.Find("input").GetAttribute("value").Should().Be("0002-10-09");
        cut.Find("[role=alert]").TextContent.Should().Be("Finish entering the year.");
    }

    private IRenderedComponent<TInput> RenderInput<TInput, TValue>(Model<TValue> model, EditContext editContext) where TInput : InputBase<TValue> => Render<TInput>(parameters => parameters
        .Add(p => p.Value, model.Value)
        .Add(p => p.ValueChanged, EventCallback.Factory.Create<TValue>(this, value => model.Value = value))
        .Add(p => p.ValueExpression, () => model.Value)
        .AddCascadingValue(editContext));

    private async Task VerifyDisposedSubscriptions<TInput, TValue>(TValue value) where TInput : NTValidationInputBase<TValue> {
        var model = new Model<TValue> { Value = value };
        var editContext = new EditContext(model);
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;
        // Observe retained handlers without changing the framework or adding production-only test hooks.
        var eventField = typeof(EditContext).GetField(nameof(EditContext.OnValidationStateChanged), BindingFlags.Instance | BindingFlags.NonPublic)!;
        var originalHandlers = ((MulticastDelegate)eventField.GetValue(editContext)!).GetInvocationList();
        var showInput = true;
        RenderFragment content = builder => {
            if (!showInput) {
                return;
            }

            builder.OpenComponent<TInput>(0);
            builder.AddComponentParameter(1, nameof(InputBase<TValue>.Value), model.Value);
            builder.AddComponentParameter(2, nameof(InputBase<TValue>.ValueChanged), EventCallback.Factory.Create<TValue>(this, value => model.Value = value));
            builder.AddComponentParameter(3, nameof(InputBase<TValue>.ValueExpression), (System.Linq.Expressions.Expression<Func<TValue>>)(() => model.Value));
            builder.CloseComponent();
        };
        var host = Render<CascadingValue<EditContext>>(parameters => parameters
            .Add(p => p.Value, editContext)
            .Add(p => p.ChildContent, content));

        showInput = false;
        host.Render(parameters => parameters.Add(p => p.Value, editContext).Add(p => p.ChildContent, content));

        host.WaitForAssertion(() => ((MulticastDelegate)eventField.GetValue(editContext)!).GetInvocationList().Should().Equal(originalHandlers, $"{typeof(TInput).Name} must release its validation subscriptions"));
        await Renderer.Dispatcher.InvokeAsync(editContext.NotifyValidationStateChanged);
        notifications.Should().Be(1);
    }

    private async Task VerifyMessageRendering<TInput, TValue>(TValue value) where TInput : NTValidationInputBase<TValue> {
        var model = new Model<TValue> { Value = value };
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        var field = new FieldIdentifier(model, nameof(model.Value));
        var cut = RenderInput<TInput, TValue>(model, editContext);
        var renderCount = cut.RenderCount;

        await cut.InvokeAsync(() => {
            messages.Add(new FieldIdentifier(model, nameof(model.Other)), "Unrelated error");
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount, $"{typeof(TInput).Name} should ignore unrelated messages");

        await cut.InvokeAsync(() => {
            messages.Add(field, "Own error");
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount + 1);
        cut.Find("[role=alert]").TextContent.Should().Be("Own error");

        await cut.InvokeAsync(() => {
            messages.Clear(field);
            messages.Add(field, "Own error");
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount + 1);

        await cut.InvokeAsync(() => {
            messages.Clear(field);
            messages.Add(field, "Replacement error");
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount + 2);
        cut.Find("[role=alert]").TextContent.Should().Be("Replacement error");

        await cut.InvokeAsync(() => {
            messages.Clear(field);
            editContext.NotifyValidationStateChanged();
        });
        cut.RenderCount.Should().Be(renderCount + 3);
        cut.FindAll("[role=alert]").Should().BeEmpty();

        await cut.InvokeAsync(editContext.NotifyValidationStateChanged);
        cut.RenderCount.Should().Be(renderCount + 3);
    }
}
