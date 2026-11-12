// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Drawing;

/// <summary>The colour vision deficiencies <see cref="ColorMatrices.CreateColorVisionDeficiency"/> simulates.</summary>
public enum ColorVisionDeficiency {
	[Description("Red-blind")]
	Protanopia,

	[Description("Red-weak")]
	Protanomaly,

	[Description("Green-blind")]
	Deuteranopia,

	[Description("Green-weak")]
	Deuteranomaly,

	[Description("Blue-blind")]
	Tritanopia,

	[Description("Blue-weak")]
	Tritanomaly,

	[Description("Colour-blind")]
	Achromatopsia,

	[Description("Colour-weak")]
	Achromatomaly
}
