// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public interface IWinFormsScreenMenuItem : IWinFormsLinkMenuItem, IScreenMenuItem {
	Type Screen { get; }

	/// <summary>An optional per-type policy; absent declarations retain the native screen constructor's default.</summary>
	new ScreenActivationMode? ActivationMode => null;

	new string ScreenTitle => null;

	Type IScreenMenuItem.ScreenType => Screen;
	ScreenActivationMode? IScreenMenuItem.ActivationMode => ActivationMode;
	string IScreenMenuItem.ScreenTitle => ScreenTitle;
}
