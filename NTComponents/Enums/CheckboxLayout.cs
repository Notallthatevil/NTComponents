using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NTComponents.Enums;

/// <summary>
///     Specifies the layout options for a checkbox and its associated label.
/// </summary>
/// <remarks>
///     Use this enumeration to control the visual arrangement of the checkbox and label within a container. The layout affects alignment and appearance, which can be important for accessibility and
///     consistency in user interfaces.
/// </remarks>
public enum CheckboxLayout {

    /// <summary>
    ///     The default appearance of the checkbox.
    /// </summary>
    Default,

    /// <summary>
    ///     Puts the label on the left of the checkbox and spans the entire width of the container, allowing for better alignment in certain layouts.
    /// </summary>
    FlipAndSpan
}