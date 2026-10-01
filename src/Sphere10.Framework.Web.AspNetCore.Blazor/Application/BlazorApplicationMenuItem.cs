// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public abstract class BlazorApplicationMenuItem : IBlazorApplicationMenuItem {
	public event EventHandlerEx Hover {
		add => _definition.Hover += value;
		remove => _definition.Hover -= value;
	}

	public event EventHandlerEx Select {
		add => _definition.Select += value;
		remove => _definition.Select -= value;
	}

	private readonly ApplicationMenuItem _definition = new();

	public string Id {
		get => _definition.Id;
		init => _definition.Id = value;
	}

	public string Icon { get; init; }

	public string Title {
		get => _definition.Title;
		init => _definition.Title = value;
	}

	internal void CopySubscriptionsFrom(BlazorApplicationMenuItem source) {
		Guard.ArgumentNotNull(source, nameof(source));
		_definition.CopySubscriptionsFrom(source._definition);
	}

	internal void NotifyHover() {
		_definition.NotifyHover();
		OnHover();
	}

	internal void NotifySelect() {
		_definition.NotifySelect();
		OnSelect();
	}

	protected virtual void OnHover() {
	}

	protected virtual void OnSelect() {
	}
}
