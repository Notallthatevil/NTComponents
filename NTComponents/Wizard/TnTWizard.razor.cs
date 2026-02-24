using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using NTComponents.Core;
using NTComponents.Wizard;

namespace NTComponents;

/// <summary>
///     Represents a wizard component that manages multiple steps and provides navigation between them.
/// </summary>
public partial class TnTWizard : TnTComponentBase, IDisposable {

    /// <summary>
    ///     The child content to be rendered inside the wizard.
    /// </summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = default!;

    /// <inheritdoc />
    public override string? ElementClass => CssClassBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .AddClass("tnt-wizard")
        .AddClass("tnt-layout-horizontal", LayoutDirection == LayoutDirection.Horizontal)
        .AddClass("tnt-vertical-on-small-screens", VerticalOnSmallScreens)
        .AddClass("tnt-wizard-buttons-on-bottom", PushNavigationToBottom)
        .Build();

    /// <inheritdoc />
    public override string? ElementStyle => CssStyleBuilder.Create()
        .AddFromAdditionalAttributes(AdditionalAttributes)
        .Build();

    /// <summary>
    ///     The visual layout style of the wizard component.
    /// </summary>
    [Parameter]
    public LayoutDirection LayoutDirection { get; set; } = LayoutDirection.Vertical;

    /// <summary>
    ///     Determines whether the wizard should remain vertical when viewed on smaller screens.
    /// </summary>
    [Parameter]
    public bool VerticalOnSmallScreens { get; set; } = true;

    /// <summary>
    ///     A value indicating whether the "Next" button is disabled.
    /// </summary>
    [Parameter]
    public bool NextButtonDisabled { get; set; }

    /// <summary>
    ///     Callback invoked when the "Next" button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnNextButtonClicked { get; set; }

    /// <summary>
    ///     Callback invoked when the "Previous" button is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnPreviousButtonClicked { get; set; }

    /// <summary>
    ///     The callback to be invoked when the wizard is submitted.
    /// </summary>
    [Parameter]
    public EventCallback OnSubmitCallback { get; set; }

    /// <summary>
    ///     A value indicating whether the "Previous" button is disabled.
    /// </summary>
    [Parameter]
    public bool PreviousButtonDisabled { get; set; }

    /// <summary>
    ///     A value indicating whether the "Submit" button is disabled.
    /// </summary>
    [Parameter]
    public bool SubmitButtonDisabled { get; set; }

    /// <summary>
    ///     Controls how Next/Submit buttons behave when the current form step is invalid.
    /// </summary>
    [Parameter]
    public TnTWizardInvalidFormButtonBehavior InvalidFormButtonBehavior { get; set; } = TnTWizardInvalidFormButtonBehavior.GrayOutOnly;

    /// <summary>
    ///     The title of the wizard, displayed at the top.
    /// </summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>
    ///     Gets the current step in the wizard.
    /// </summary>
    private TnTWizardStepBase? _currentStep => _steps.ElementAtOrDefault(_stepIndex);

    /// <summary>
    ///     Static field to generate unique IDs for child steps.
    /// </summary>
    private static int _childId;

    /// <summary>
    ///     List of all steps added to the wizard.
    /// </summary>
    private readonly List<TnTWizardStepBase> _steps = [];

    /// <summary>
    ///     Tracks wizard steps that have already been visited.
    /// </summary>
    private readonly HashSet<int> _visitedStepIds = [];

    /// <summary>
    /// If set, the navigation section of the wizard will be pushed to the bottom of the component, otherwise it will be displayed directly below the step content. This can be used to achieve a more compact layout when using the wizard in horizontal mode.
    /// </summary>
    [Parameter]
    public bool PushNavigationToBottom { get; set; } = true;

    /// <summary>
    ///     Index of the currently active step.
    /// </summary>
    private int _stepIndex;

    /// <summary>
    ///     Tracks validity of the currently active form step.
    /// </summary>
    private bool _currentStepFormIsValid = true;

    /// <summary>
    ///     Tracks the current form step edit context subscription.
    /// </summary>
    private EditContext? _currentStepEditContext;

    /// <summary>
    ///     True when the current step is a form step with an invalid form state.
    /// </summary>
    private bool IsCurrentStepFormInvalid => _currentStep is TnTWizardFormStep && !_currentStepFormIsValid;

    /// <summary>
    ///     Effective disabled state for the Next button.
    /// </summary>
    private bool EffectiveNextButtonDisabled => NextButtonDisabled || ShouldDisableProgressButtonsForInvalidForm();

    /// <summary>
    ///     Effective disabled state for the Submit button.
    /// </summary>
    private bool EffectiveSubmitButtonDisabled => SubmitButtonDisabled || ShouldDisableProgressButtonsForInvalidForm();

    /// <summary>
    ///     Class used to visually grey out progression buttons while still allowing clicks.
    /// </summary>
    private string? ProgressButtonInvalidClass => IsCurrentStepFormInvalid && InvalidFormButtonBehavior == TnTWizardInvalidFormButtonBehavior.GrayOutOnly ? "tnt-disabled" : null;

    /// <inheritdoc />
    protected override Task OnAfterRenderAsync(bool firstRender) {
        if (SyncCurrentStepValidationSubscription()) {
            _ = InvokeAsync(StateHasChanged);
        }

        return base.OnAfterRenderAsync(firstRender);
    }

    /// <inheritdoc />
    public void Dispose() {
        DetachCurrentStepValidationSubscription();
    }

    /// <summary>
    ///     Adds a child step to the wizard.
    /// </summary>
    /// <param name="step">The step to add.</param>
    /// <exception cref="ArgumentNullException">Thrown if the step is null.</exception>
    internal void AddChildStep(TnTWizardStepBase step) {
        ArgumentNullException.ThrowIfNull(step);
        if (step._internalId == -1) {
            step._internalId = Interlocked.Increment(ref _childId);
        }

        if (!_steps.Any(s => s._internalId == step._internalId)) {
            _steps.Add(step);
        }
        MarkCurrentStepVisited();
        SyncCurrentStepValidationSubscription();
        StateHasChanged();
    }

    /// <summary>
    ///     Advances to the next step in the wizard.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal async Task NextStepAsync() {
        if (_stepIndex + 1 < _steps.Count && await ValidateCurrentStepAsync()) {
            await OnNextButtonClicked.InvokeAsync(_stepIndex + 1);
            _stepIndex++;
            MarkCurrentStepVisited();
            SyncCurrentStepValidationSubscription();
        }
        return;
    }

    /// <summary>
    ///     Navigates to the previous step in the wizard.
    /// </summary>
    internal async Task PreviousStepAsync() {
        if (_stepIndex > 0) {
            await OnPreviousButtonClicked.InvokeAsync(_stepIndex);
            _stepIndex--;
            MarkCurrentStepVisited();
            SyncCurrentStepValidationSubscription();
        }
    }

    /// <summary>
    ///     Navigates directly to a step when it has already been visited or when it is the immediate next step.
    /// </summary>
    /// <param name="stepIndex">The index of the step to navigate to.</param>
    internal async Task NavigateToStepAsync(int stepIndex) {
        if (stepIndex == _stepIndex || stepIndex < 0 || stepIndex >= _steps.Count) {
            return;
        }

        if (IsImmediateNextStep(stepIndex)) {
            await NextStepAsync();
            return;
        }

        if (!IsStepVisited(stepIndex)) {
            return;
        }

        _stepIndex = stepIndex;
        MarkCurrentStepVisited();
        SyncCurrentStepValidationSubscription();
    }

    /// <summary>
    ///     Removes a child step from the wizard.
    /// </summary>
    /// <param name="step">The step to remove.</param>
    /// <exception cref="ArgumentNullException">Thrown if the step is null.</exception>
    internal void RemoveChildStep(TnTWizardStepBase step) {
        ArgumentNullException.ThrowIfNull(step);
        _visitedStepIds.Remove(step._internalId);
        _steps.RemoveAll(s => s._internalId == step._internalId);
        if (_stepIndex >= _steps.Count) {
            _stepIndex = Math.Max(0, _steps.Count - 1);
        }
        MarkCurrentStepVisited();
        SyncCurrentStepValidationSubscription();
        StateHasChanged();
    }

    /// <summary>
    ///     Handles the key press event for keyboard input, advancing the workflow or submitting the form when the Enter key is pressed.
    /// </summary>
    /// <remarks>If the Enter key is pressed on the final step, the form is submitted; otherwise, the workflow advances to the next step.</remarks>
    /// <param name="args">The keyboard event arguments containing information about the key that was pressed.</param>
    /// <returns>A task that represents the asynchronous operation of handling the key press event.</returns>
    private async Task HandleKeyPressAsync(KeyboardEventArgs args) {
        if (args.Key == "Enter") {
            if (_stepIndex + 1 == _steps.Count) {
                if (!EffectiveSubmitButtonDisabled) {
                    await SubmitClickedAsync();
                }
            }
            else {
                if (!EffectiveNextButtonDisabled) {
                    await NextStepAsync();
                }
            }
        }
    }

    /// <summary>
    ///     Invokes the submit callback when the submit button is clicked.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SubmitClickedAsync() {
        if (await ValidateCurrentStepAsync()) {
            await OnSubmitCallback.InvokeAsync();
        }
    }

    private bool IsStepVisited(int stepIndex) {
        var step = _steps.ElementAtOrDefault(stepIndex);
        return step is not null && _visitedStepIds.Contains(step._internalId);
    }

    private bool IsImmediateNextStep(int stepIndex) => stepIndex == _stepIndex + 1;

    private void MarkCurrentStepVisited() {
        if (_currentStep is not null) {
            _visitedStepIds.Add(_currentStep._internalId);
        }
    }

    private bool ShouldDisableProgressButtonsForInvalidForm() => IsCurrentStepFormInvalid && InvalidFormButtonBehavior == TnTWizardInvalidFormButtonBehavior.DisableButtons;

    private bool SyncCurrentStepValidationSubscription() {
        var formStep = _currentStep as TnTWizardFormStep;
        var editContext = formStep?.EditContext;
        var shouldRender = false;

        if (editContext is null) {
            DetachCurrentStepValidationSubscription();
            shouldRender = SetCurrentStepFormValidity(formStep is null);
            return shouldRender;
        }

        if (!ReferenceEquals(_currentStepEditContext, editContext)) {
            DetachCurrentStepValidationSubscription();
            _currentStepEditContext = editContext;
            _currentStepEditContext.OnFieldChanged += HandleCurrentStepFieldChanged;
            shouldRender = SetCurrentStepFormValidity(GetCurrentStepFormValidityWithoutMessageEmission()) || shouldRender;
        }

        return shouldRender;
    }

    private bool SetCurrentStepFormValidity(bool isValid) {
        if (_currentStepFormIsValid == isValid) {
            return false;
        }

        _currentStepFormIsValid = isValid;
        return true;
    }

    private void DetachCurrentStepValidationSubscription() {
        if (_currentStepEditContext is null) {
            return;
        }

        _currentStepEditContext.OnFieldChanged -= HandleCurrentStepFieldChanged;
        _currentStepEditContext = null;
    }

    private void HandleCurrentStepFieldChanged(object? sender, FieldChangedEventArgs args) {
        if (sender is not EditContext editContext || !ReferenceEquals(editContext, _currentStepEditContext)) {
            return;
        }

        if (SetCurrentStepFormValidity(GetCurrentStepFormValidityWithoutMessageEmission())) {
            _ = InvokeAsync(StateHasChanged);
        }
    }

    private async Task<bool> ValidateCurrentStepAsync() {
        if (_currentStep is not TnTWizardFormStep formStep) {
            return true;
        }

        var isValid = await formStep.FormValidAsync();
        SetCurrentStepFormValidity(isValid);
        return isValid;
    }

    private bool GetCurrentStepFormValidityWithoutMessageEmission() {
        if (_currentStep is not TnTWizardFormStep formStep) {
            return true;
        }

        return formStep.RequiredFieldsSatisfiedWithoutMessageEmission();
    }
}

/// <summary>
///     Defines how wizard progression buttons should behave when the current form step is invalid.
/// </summary>
public enum TnTWizardInvalidFormButtonBehavior {

    /// <summary>
    ///     Applies disabled styling but keeps buttons clickable so validation feedback can be shown on click.
    /// </summary>
    GrayOutOnly,

    /// <summary>
    ///     Fully disables buttons while the current form step is invalid.
    /// </summary>
    DisableButtons
}
