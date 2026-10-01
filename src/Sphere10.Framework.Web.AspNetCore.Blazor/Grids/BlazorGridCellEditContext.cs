// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Provides the buffered value and callback used by a custom grid editor.</summary>
/// <remarks>Editors report values through ValueChanged; the grid applies them to Item only when saving.</remarks>
public class BlazorGridCellEditContext<T> {
	public T Item { get; init; }

	public BlazorGridColumn<T> Column { get; init; }

	public object Value { get; init; }

	public EventCallback<object> ValueChanged { get; init; }

	public bool IsNewItem { get; init; }
}
