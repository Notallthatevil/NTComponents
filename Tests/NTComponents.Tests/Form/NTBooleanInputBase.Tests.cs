using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace NTComponents.Tests.Form;

public class NTBooleanInputBase_Tests : BunitContext {
    private sealed class Model {
        public bool Enabled { get; set; }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StaticSsr_PreexistingValidation_RendersNativeInputAndFieldError(bool isSwitch) {
        var model = new Model();
        var editContext = new EditContext(model);
        var messages = new ValidationMessageStore(editContext);
        messages.Add(new FieldIdentifier(model, nameof(Model.Enabled)), "External Boolean error.");
        RenderFragment content = builder => {
            builder.OpenComponent(0, isSwitch ? typeof(NTInputSwitch) : typeof(NTInputCheckbox));
            builder.AddAttribute(1, nameof(NTBooleanInputBase.Value), model.Enabled);
            builder.AddAttribute(2, nameof(NTBooleanInputBase.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
            builder.AddAttribute(3, nameof(NTBooleanInputBase.ElementId), "ssr-boolean");
            builder.AddAttribute(4, nameof(NTBooleanInputBase.Required), true);
            builder.CloseComponent();
        };
        await using var renderer = new HtmlRenderer(Services, Services.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () => {
            var output = await renderer.RenderComponentAsync<CascadingValue<EditContext>>(ParameterView.FromDictionary(new Dictionary<string, object?> {
                [nameof(CascadingValue<EditContext>.Value)] = editContext,
                [nameof(CascadingValue<EditContext>.IsFixed)] = true,
                [nameof(CascadingValue<EditContext>.ChildContent)] = content
            }));
            return output.ToHtmlString();
        });

        using var document = new HtmlParser().ParseDocument(html);
        var input = document.QuerySelector("input[type=checkbox]")!;
        input.GetAttribute("id").Should().Be("ssr-boolean");
        input.GetAttribute("name").Should().NotBeNullOrWhiteSpace();
        input.GetAttribute("aria-invalid").Should().Be("true");
        document.QuerySelector("[role=alert]")!.TextContent.Should().Be("External Boolean error.");
        document.QuerySelector("input[type=hidden]")!.GetAttribute("value").Should().Be("false");
        editContext.GetValidationMessages().Should().Equal("External Boolean error.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OptionalInput_ValidationRequestsAndFieldChanges_DoNotNotify(bool isSwitch) {
        var model = new Model();
        var editContext = new EditContext(model);
        var cut = RenderInput(isSwitch, model, editContext, required: false);
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;

        await cut.InvokeAsync(() => {
            editContext.Validate().Should().BeTrue();
            editContext.Validate().Should().BeTrue();
            editContext.NotifyFieldChanged(new FieldIdentifier(model, nameof(Model.Enabled)));
        });

        notifications.Should().Be(0);
        editContext.GetValidationMessages().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OptionalInput_EnablingRequiredAfterValidation_WaitsForRequiredValidation(bool isSwitch) {
        var model = new Model();
        var editContext = new EditContext(model);
        var required = false;
        var cut = RenderInput(isSwitch, model, editContext, () => required, () => "Select this field.");
        await cut.InvokeAsync(() => {
            editContext.Validate();
            editContext.NotifyFieldChanged(new FieldIdentifier(model, nameof(Model.Enabled)));
        });

        required = true;
        cut.Render();
        editContext.GetValidationMessages().Should().BeEmpty();
        cut.FindAll("[role=alert]").Should().BeEmpty();

        await cut.InvokeAsync(() => editContext.Validate().Should().BeFalse());
        editContext.GetValidationMessages().Should().Equal("Select this field.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredInput_RepeatedValidation_NotifiesOnlyWhenMessageAppearsOrClears(bool isSwitch) {
        var model = new Model();
        var editContext = new EditContext(model);
        var cut = RenderInput(isSwitch, model, editContext, required: true);
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;
        var field = new FieldIdentifier(model, nameof(Model.Enabled));

        await cut.InvokeAsync(() => editContext.NotifyFieldChanged(field));
        notifications.Should().Be(1);
        editContext.GetValidationMessages(field).Should().Equal("Select this field.");
        cut.Find("[role=alert]").TextContent.Should().Be("Select this field.");

        await cut.InvokeAsync(() => {
            editContext.NotifyFieldChanged(field);
            editContext.Validate().Should().BeFalse();
            editContext.Validate().Should().BeFalse();
        });
        notifications.Should().Be(1);
        editContext.GetValidationMessages(field).Should().Equal("Select this field.");

        cut.Find("input[type=checkbox]").Change(true);
        notifications.Should().Be(2);
        editContext.GetValidationMessages(field).Should().BeEmpty();
        cut.FindAll("[role=alert]").Should().BeEmpty();

        await cut.InvokeAsync(() => editContext.NotifyFieldChanged(field));
        notifications.Should().Be(2);

        cut.Find("input[type=checkbox]").Change(false);
        notifications.Should().Be(3);
        editContext.GetValidationMessages(field).Should().Equal("Select this field.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckedRequiredInput_ValidationRequest_DoesNotAddNotification(bool isSwitch) {
        var model = new Model { Enabled = true };
        var editContext = new EditContext(model);
        var cut = RenderInput(isSwitch, model, editContext, required: true);
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;

        await cut.InvokeAsync(() => editContext.Validate().Should().BeTrue());

        notifications.Should().Be(0);
        editContext.GetValidationMessages().Should().BeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequiredMessage_ParameterChanges_NotifyOnlyForChangedMessage(bool isSwitch) {
        var model = new Model();
        var editContext = new EditContext(model);
        var required = true;
        var errorText = "Select this field.";
        var cut = RenderInput(isSwitch, model, editContext, () => required, () => errorText);
        await cut.InvokeAsync(() => editContext.Validate());
        var notifications = 0;
        editContext.OnValidationStateChanged += (_, _) => notifications++;

        cut.Render();
        notifications.Should().Be(0);

        errorText = "Accept the updated terms.";
        cut.Render();
        notifications.Should().Be(1);
        editContext.GetValidationMessages().Should().Equal(errorText);
        cut.Find("[role=alert]").TextContent.Should().Be(errorText);

        required = false;
        cut.Render();
        notifications.Should().Be(2);
        editContext.GetValidationMessages().Should().BeEmpty();
        cut.FindAll("[role=alert]").Should().BeEmpty();

        cut.Render();
        notifications.Should().Be(2);
    }

    private IRenderedComponent<EditForm> RenderInput(bool isSwitch, Model model, EditContext editContext, bool required) => RenderInput(isSwitch, model, editContext, () => required, () => "Select this field.");

    private IRenderedComponent<EditForm> RenderInput(bool isSwitch, Model model, EditContext editContext, Func<bool> required, Func<string> errorText) => Render<EditForm>(parameters => parameters
        .Add(p => p.EditContext, editContext)
        .Add(p => p.ChildContent, (EditContext _) => builder => {
            builder.OpenComponent(0, isSwitch ? typeof(NTInputSwitch) : typeof(NTInputCheckbox));
            builder.AddAttribute(1, nameof(NTBooleanInputBase.Value), model.Enabled);
            builder.AddAttribute(2, nameof(NTBooleanInputBase.ValueChanged), EventCallback.Factory.Create<bool>(this, value => model.Enabled = value));
            builder.AddAttribute(3, nameof(NTBooleanInputBase.ValueExpression), (Expression<Func<bool>>)(() => model.Enabled));
            builder.AddAttribute(4, nameof(NTBooleanInputBase.Required), required());
            builder.AddAttribute(5, nameof(NTBooleanInputBase.RequiredErrorText), errorText());
            builder.CloseComponent();
        }));
}
