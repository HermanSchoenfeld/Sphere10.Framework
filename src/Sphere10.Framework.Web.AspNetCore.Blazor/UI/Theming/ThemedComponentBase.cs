// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Theming;

/// <summary>Observes the scoped theme and releases the subscription when the renderer disposes the component.</summary>
public abstract class ThemedComponentBase : ComponentBase, IDisposable {
	private IDisposable _subscription;
	private bool _disposed;

	[Inject]
	protected IThemeService ThemeService { get; set; }

	protected string ThemeName => ThemeService.CurrentTheme switch {
		ThemeMode.Dark => "dark",
		ThemeMode.Blue => "blue",
		ThemeMode.ClassicBlue => "classic-blue",
		_ => "light"
	};

	protected string BootstrapThemeName => ThemeService.CurrentTheme == ThemeMode.Dark ? "dark" : "light";

	public virtual void Dispose() {
		_disposed = true;
		_subscription?.Dispose();
		_subscription = null;
		GC.SuppressFinalize(this);
	}

	protected override void OnInitialized() {
		base.OnInitialized();
		ThemeService.Changed += HandleThemeChanged;
		_subscription = Tools.Scope.ExecuteOnDispose(() => ThemeService.Changed -= HandleThemeChanged);
	}

	private void HandleThemeChanged() {
		if (_disposed)
			return;
		_ = InvokeAsync(() => {
			if (!_disposed)
				StateHasChanged();
		});
	}
}
