// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A native application screen whose synchronous hooks are exposed through the shared lifecycle contract.</summary>
/// <remarks>Callbacks run on the calling UI thread. The native host never blocks an asynchronous continuation.</remarks>
public interface IApplicationScreen : SharedUI.IApplicationScreen,
	IApplicationCommandProvider<IApplicationMenu, IApplicationMenuItem> {
	new string Title { get; set; }

	new IApplicationScreenHost Host { get; set; }

	new IApplicationBlock Block { get; set; }

	IApplicationBlock ApplicationBlock { get; set; }

	ScreenActivationMode ActivationMode { get; }

	ToolStripItem[] MenuItems { get; }

	/// <summary>The actual screen-owned Designer menu strip, borrowed by the native host while this screen is active.</summary>
	MenuStrip ScreenMenuStrip { get; set; }

	/// <summary>The actual screen-owned Designer toolbar, with native items restored when this screen is deactivated.</summary>
	ToolStrip ScreenToolBar { get; set; }

	bool CanDeactivate();

	void OnActivated();

	void OnDeactivated();

	#region Base Interface Bridges

	string SharedUI.IApplicationScreen.Title => Title;

	SharedUI.IApplicationScreenHost SharedUI.IApplicationScreen.Host {
		get => Host;
		set => Host = value == null ? null : Guard.ArgumentCast<IApplicationScreenHost>(value, nameof(value));
	}

	SharedUI.IApplicationBlock SharedUI.IApplicationScreen.Block {
		get => Block;
		set => Block = value == null ? null : Guard.ArgumentCast<IApplicationBlock>(value, nameof(value));
	}

	Task<bool> SharedUI.IApplicationScreen.CanDeactivateAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(CanDeactivate());
	}

	Task SharedUI.IApplicationScreen.OnActivatedAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		OnActivated();
		return Task.CompletedTask;
	}

	Task SharedUI.IApplicationScreen.OnDeactivatedAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		OnDeactivated();
		return Task.CompletedTask;
	}

	#endregion
}
