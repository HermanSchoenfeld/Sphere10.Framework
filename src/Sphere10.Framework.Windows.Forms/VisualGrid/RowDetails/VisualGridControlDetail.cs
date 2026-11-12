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

/// <summary>Adapts an arbitrary control and optional owned resources to the shared row-detail lifetime.</summary>
public sealed class VisualGridControlDetail : VisualGridDetail {
	private readonly Action<Control, object> _apply;
	private readonly IDisposable[] _resources;
	private bool _resourcesReleased;

	public VisualGridControlDetail(Control content, Action<Control, object> apply = null, params IDisposable[] resources)
		: base(content) {
		_apply = apply;
		_resources = resources ?? Array.Empty<IDisposable>();
	}

	public override void Dispose() {
		base.Dispose();
		if (_resourcesReleased)
			return;
		_resourcesReleased = true;
		foreach (var resource in _resources)
			resource?.Dispose();
	}

	protected override Task ApplyCoreAsync(object prepared, CancellationToken cancellationToken) {
		_apply?.Invoke(Content, prepared);
		return Task.CompletedTask;
	}
}
