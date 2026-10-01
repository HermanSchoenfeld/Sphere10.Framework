// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

public abstract class ThemeServiceDecorator<TConcrete> : IThemeService where TConcrete : IThemeService {
	public virtual event EventHandlerEx Changed {
		add => InternalThemeService.Changed += value;
		remove => InternalThemeService.Changed -= value;
	}

	protected readonly TConcrete InternalThemeService;

	protected ThemeServiceDecorator(TConcrete themeService) {
		Guard.ArgumentNotNull(themeService, nameof(themeService));
		InternalThemeService = themeService;
	}

	public virtual ThemeMode CurrentTheme => InternalThemeService.CurrentTheme;

	public virtual void SetTheme(ThemeMode theme) => InternalThemeService.SetTheme(theme);

	public virtual void Toggle() => InternalThemeService.Toggle();
}

public abstract class ThemeServiceDecorator : ThemeServiceDecorator<IThemeService> {
	protected ThemeServiceDecorator(IThemeService themeService)
		: base(themeService) {
	}
}
