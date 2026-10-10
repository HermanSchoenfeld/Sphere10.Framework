// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public abstract class BlazorApplicationMenuItemBase : ApplicationMenuItemBase, IBlazorApplicationMenuItem {
	private string _id;

	public new IBlazorApplicationMenu Parent {
		get => (IBlazorApplicationMenu)base.Parent;
		set => base.Parent = value;
	}

	public string Id {
		get => _id ?? Title;
		init => _id = value;
	}

	public string Icon { get; init; }

	public bool IsVisible { get; init; } = true;

	/// <summary>Use circuit-owned predicates for dynamic commands; shared registrations must not capture user state.</summary>
	public Func<bool> CanExecute { get; init; }

	public bool IsEnabled => CanExecute?.Invoke() ?? true;

	public new string Title {
		get => base.Title;
		init => base.Title = value;
	}

	internal void CopySubscriptionsFrom(BlazorApplicationMenuItemBase source) {
		Guard.ArgumentNotNull(source, nameof(source));
		base.CopySubscriptionsFrom(source);
	}

	public override void NotifyHover() {
		base.NotifyHover();
		OnHover();
	}

	public override void NotifySelect() {
		base.NotifySelect();
		OnSelect();
	}

	protected virtual void OnHover() {
	}

	protected virtual void OnSelect() {
	}
}
