// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>A screen declaration. A null activation mode leaves the platform's screen default in effect.</summary>
public interface IScreenMenuItem : IApplicationMenuItem {
	Type ScreenType { get; }

	ScreenActivationMode? ActivationMode { get; }

	string ScreenTitle => null;

	/// <summary>Empty screens are tabless placeholders and never coexist visibly with an open normal screen.</summary>
	ScreenKind ScreenKind => ScreenKind.Normal;

	/// <summary>Marks a startup candidate; the first candidate in plugin, block and menu order wins.</summary>
	bool IsDefault => false;
}
