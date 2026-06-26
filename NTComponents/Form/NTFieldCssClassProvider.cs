using Microsoft.AspNetCore.Components.Forms;
using System.Runtime.CompilerServices;

namespace NTComponents;

internal sealed class NTFieldCssClassProvider : FieldCssClassProvider {
    private static readonly ConditionalWeakTable<EditContext, ValidationRequestedState> ValidationRequestedStates = new();

    internal static readonly NTFieldCssClassProvider Instance = new();

    internal static void Configure(EditContext editContext) {
        editContext.SetFieldCssClassProvider(Instance);

        if (ValidationRequestedStates.TryGetValue(editContext, out _)) {
            return;
        }

        var state = new ValidationRequestedState();
        editContext.OnValidationRequested += state.HandleValidationRequested;
        ValidationRequestedStates.Add(editContext, state);
    }

    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier) {
        var isModified = editContext.IsModified(fieldIdentifier) || IsValidationRequested(editContext);
        var isInvalid = editContext.GetValidationMessages(fieldIdentifier).Any();

        return (isModified, isInvalid) switch {
            (true, true) => "nt-modified nt-invalid",
            (true, false) => "nt-modified nt-valid",
            (false, true) => "nt-invalid",
            _ => "nt-valid"
        };
    }

    private static bool IsValidationRequested(EditContext editContext) => ValidationRequestedStates.TryGetValue(editContext, out var state) && state.IsValidationRequested;

    private sealed class ValidationRequestedState {
        public bool IsValidationRequested { get; private set; }

        public void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args) => IsValidationRequested = true;
    }
}