// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A native application screen whose synchronous hooks are exposed through the shared lifecycle contract.</summary>
/// <remarks>Callbacks run on the calling UI thread. The native host never blocks an asynchronous continuation.</remarks>
public interface IWinFormsApplicationScreen : IApplicationScreen {
	string Title { get; set; }

	IWinFormsApplicationBlock ApplicationBlock { get; set; }

	ScreenActivationMode ActivationMode { get; }

	bool CanDeactivate();

	void OnActivated();

	void OnDeactivated();

	Task<bool> IApplicationScreen.CanDeactivateAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(CanDeactivate());
	}

	Task IApplicationScreen.OnActivatedAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		OnActivated();
		return Task.CompletedTask;
	}

	Task IApplicationScreen.OnDeactivatedAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		OnDeactivated();
		return Task.CompletedTask;
	}
}
