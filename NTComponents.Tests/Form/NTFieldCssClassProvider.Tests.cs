using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Forms;

namespace NTComponents.Tests.Form;

public class NTFieldCssClassProvider_Tests {
    [Fact]
    public void GetFieldCssClass_WhenUntouchedAndValid_ReturnsNtValid() {
        var (editContext, fieldIdentifier) = CreateContext();

        NTFieldCssClassProvider.Instance.GetFieldCssClass(editContext, fieldIdentifier).Should().Be("nt-valid");
    }

    [Fact]
    public void GetFieldCssClass_WhenModifiedAndValid_ReturnsNtModifiedNtValid() {
        var (editContext, fieldIdentifier) = CreateContext();

        editContext.NotifyFieldChanged(fieldIdentifier);

        NTFieldCssClassProvider.Instance.GetFieldCssClass(editContext, fieldIdentifier).Should().Be("nt-modified nt-valid");
    }

    [Fact]
    public void GetFieldCssClass_WhenUntouchedAndInvalid_ReturnsNtInvalid() {
        var (editContext, fieldIdentifier) = CreateContext();
        AddValidationMessage(editContext, fieldIdentifier);

        NTFieldCssClassProvider.Instance.GetFieldCssClass(editContext, fieldIdentifier).Should().Be("nt-invalid");
    }

    [Fact]
    public void GetFieldCssClass_WhenModifiedAndInvalid_ReturnsNtModifiedNtInvalid() {
        var (editContext, fieldIdentifier) = CreateContext();
        AddValidationMessage(editContext, fieldIdentifier);

        editContext.NotifyFieldChanged(fieldIdentifier);

        NTFieldCssClassProvider.Instance.GetFieldCssClass(editContext, fieldIdentifier).Should().Be("nt-modified nt-invalid");
    }

    [Fact]
    public void GetFieldCssClass_AfterValidationRequested_WhenUntouchedAndValid_ReturnsNtModifiedNtValid() {
        var (editContext, fieldIdentifier) = CreateContext();
        NTFieldCssClassProvider.Configure(editContext);

        editContext.Validate();

        NTFieldCssClassProvider.Instance.GetFieldCssClass(editContext, fieldIdentifier).Should().Be("nt-modified nt-valid");
    }

    [Fact]
    public void GetFieldCssClass_AfterValidationRequested_WhenUntouchedAndInvalid_ReturnsNtModifiedNtInvalid() {
        var (editContext, fieldIdentifier) = CreateContext();
        NTFieldCssClassProvider.Configure(editContext);
        var store = new ValidationMessageStore(editContext);
        editContext.OnValidationRequested += (_, _) => store.Add(fieldIdentifier, "Required");

        editContext.Validate();

        NTFieldCssClassProvider.Instance.GetFieldCssClass(editContext, fieldIdentifier).Should().Be("nt-modified nt-invalid");
    }

    private static (EditContext EditContext, FieldIdentifier FieldIdentifier) CreateContext() {
        var model = new TestModel();
        var editContext = new EditContext(model);
        return (editContext, new FieldIdentifier(model, nameof(TestModel.Name)));
    }

    private static void AddValidationMessage(EditContext editContext, FieldIdentifier fieldIdentifier) {
        var store = new ValidationMessageStore(editContext);
        store.Add(fieldIdentifier, "Required");
        editContext.NotifyValidationStateChanged();
    }

    private sealed class TestModel {
        public string? Name { get; set; }
    }
}