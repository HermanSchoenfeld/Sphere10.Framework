// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>Portable item metadata and registration-time notification handlers.</summary>
/// <remarks>Shared registrations must not capture scoped services or mutable per-user state in event handlers.</remarks>
public class ApplicationMenuItem : IApplicationMenuItem {
	public event EventHandlerEx Hover;

	public event EventHandlerEx Select;

	private string _id;

	public virtual string Id {
		get => _id ?? Title;
		set => _id = value;
	}

	public virtual string Title { get; set; } = string.Empty;

	/// <summary>Copies invocation lists at snapshot time without forwarding to the mutable source.</summary>
	public void CopySubscriptionsFrom(ApplicationMenuItem source) {
		Guard.ArgumentNotNull(source, nameof(source));
		Hover = source.Hover;
		Select = source.Select;
	}

	public virtual void NotifyHover() => Hover?.Invoke();

	public virtual void NotifySelect() => Select?.Invoke();
}
