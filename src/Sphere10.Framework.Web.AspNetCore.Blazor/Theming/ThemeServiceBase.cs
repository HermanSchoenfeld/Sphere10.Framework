// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

public abstract class ThemeServiceBase : IThemeService {
	public event EventHandlerEx Changed;

	public abstract ThemeMode CurrentTheme { get; }

	public abstract void SetTheme(ThemeMode theme);

	public virtual void Toggle() => SetTheme(CurrentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark);

	protected virtual void OnChanged() => Changed?.Invoke();
}
