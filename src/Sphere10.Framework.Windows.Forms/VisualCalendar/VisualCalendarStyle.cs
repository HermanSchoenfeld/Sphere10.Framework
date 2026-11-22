// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

/// <summary>The look of a calendar's appointments and free time slots. Each style derives its colors from the appointment's own color.</summary>
public enum VisualCalendarStyle {
	/// <summary>A pastel fill with darker text and outline of the same hue, over the standard grid lines. The default.</summary>
	[Description("Pastel")] Pastel,

	/// <summary>The appointment color as the fill, with black or white text chosen for contrast and a darker outline.</summary>
	[Description("Solid")] Solid,

	/// <summary>The window color as the fill, with a heavy outline and text in the appointment color.</summary>
	[Description("Outline")] Outline,

	/// <summary>A pastel fill without an outline, over faint dotted hour lines and no column lines.</summary>
	[Description("Minimal")] Minimal
}
