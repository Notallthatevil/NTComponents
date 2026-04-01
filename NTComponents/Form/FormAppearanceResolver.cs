using Microsoft.Extensions.DependencyInjection;
using NTComponents.Core;

namespace NTComponents;

internal static class FormAppearanceResolver {
    internal static FormAppearance ResolveLocal(FormAppearance appearance, IServiceProvider? services) {
        if (appearance is not FormAppearance.Default) {
            return appearance;
        }

        var configuredDefaultAppearance = services?.GetService<NTComponentsDefaultOptions>()?.DefaultFormAppearance
            ?? NTComponentsDefaultOptions.Default.DefaultFormAppearance;

        return configuredDefaultAppearance is FormAppearance.Default
            ? FormAppearance.OutlinedCompact
            : configuredDefaultAppearance;
    }

    internal static FormAppearance ResolveEffective(ITnTForm? parentForm, FormAppearance appearance, IServiceProvider? services, bool useParentForm = true) {
        if (useParentForm && parentForm is not null) {
            return parentForm.Appearance is FormAppearance.Default
                ? ResolveLocal(FormAppearance.Default, services)
                : parentForm.Appearance;
        }

        return ResolveLocal(appearance, services);
    }
}
