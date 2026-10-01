// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

public interface IScreenMenuItem : ILinkMenuItem {
	Type Screen { get; }
	/// <summary>Optional declaration of the screen type's instance policy, shared by every menu entry for that type.</summary>
	ScreenActivationMode? ActivationMode => null;
	string? ScreenTitle => null;
}

