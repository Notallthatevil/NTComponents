using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents.Core;

/// <summary>
/// Provides global default options for NTComponents, allowing host applications to configure baseline behavior across
/// all components.
/// </summary>
public class NTComponentsDefaultOptions {
    /// <summary>
    /// Gets the built-in fallback defaults used when no configured options instance has been registered in DI.
    /// </summary>
    public static readonly NTComponentsDefaultOptions Default = new();

    /// <summary>
    /// Gets or sets the default <see cref="FormAppearance"/> applied to form components when no explicit appearance is
    /// specified. Defaults to <see cref="FormAppearance.OutlinedCompact"/>.
    /// </summary>
    public FormAppearance DefaultFormAppearance { get; set; } = FormAppearance.OutlinedCompact;
}
