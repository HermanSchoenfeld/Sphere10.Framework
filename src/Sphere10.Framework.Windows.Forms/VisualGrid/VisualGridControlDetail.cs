// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Displays a custom row-detail control and updates it from each prepared context.</summary>
/// <remarks>
/// Return this adapter from <see cref="VisualGrid.CreateDetail"/> for an image, editor or other custom panel.
/// Its optional apply callback runs on the UI thread with the control and prepared context. The control and any supplied
/// resources are disposed when the detail closes, while the base adapter manages the applied context's lifetime.
/// </remarks>
public sealed class VisualGridControlDetail : VisualGridDetailBase {
	private readonly Action<Control, object> _apply;
	private readonly IDisposable[] _resources;

	public VisualGridControlDetail(Control content, Action<Control, object> apply = null, params IDisposable[] resources)
		: base(content) {
		_apply = apply;
		_resources = resources ?? Array.Empty<IDisposable>();
	}

	protected override void DisposeInternal() {
		base.DisposeInternal();
		foreach (var resource in _resources)
			resource?.Dispose();
	}

	protected override Task ApplyCoreAsync(object prepared, CancellationToken cancellationToken) {
		_apply?.Invoke(Content, prepared);
		return Task.CompletedTask;
	}
}
