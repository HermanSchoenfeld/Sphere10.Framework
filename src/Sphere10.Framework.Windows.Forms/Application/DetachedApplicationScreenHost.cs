// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Projects one detached window's active screen while borrowing the root application's host and lifetime.</summary>
internal sealed class DetachedApplicationScreenHost : ApplicationScreenHostDecorator {
	private readonly ApplicationScreenForm _window;
	private IUserInterfaceServices _userInterface;

	// Borrows the root host while supplying the detached window's local presentation context.
	internal DetachedApplicationScreenHost(IApplicationScreenHost rootHost, ApplicationScreenForm window)
		: base(rootHost) {
		Guard.ArgumentNotNull(window, nameof(window));
		_window = window;
	}

	/// <summary>Main-frame selection changes do not change this window's active screen.</summary>
	public override event EventHandlerEx<ApplicationScreen> ActiveScreenChanging {
		add {
		}
		remove {
		}
	}

	/// <summary>Main-frame selection changes do not change this window's active screen.</summary>
	public override event EventHandlerEx<ApplicationScreen> ActiveScreenChanged {
		add {
		}
		remove {
		}
	}

	/// <summary>The screen presented by this detached window.</summary>
	public override ApplicationScreen ActiveScreen => _window.Screen;

	/// <summary>The command context is the detached screen's owning block.</summary>
	public override IApplicationBlock ActiveBlock => ActiveScreen.Block;

	/// <summary>Provides the local active-screen host while forwarding application interactions to the root UI.</summary>
	public override IUserInterfaceServices UserInterfaceServices {
		get {
			if (_userInterface == null && InternalHost.UserInterfaceServices is { } services)
				_userInterface = new DetachedUserInterfaceServices(services, this);
			return _userInterface;
		}
	}

	/// <summary>The borrowed owner used to validate canonical screen membership and restore docking.</summary>
	internal IApplicationScreenHost RootHost => InternalHost;

	/// <summary>Closes this window's screen through the root host's existing guard and cleanup path.</summary>
	public override Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) {
		return InternalHost.CloseScreenAsync(ActiveScreen, cancellationToken);
	}

	/// <summary>Checks this window's screen without depending on the main frame's current tab.</summary>
	public override Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) {
		return InternalHost.CanCloseScreenAsync(ActiveScreen, cancellationToken);
	}

	/// <summary>Releasing a borrowed window projection never disposes the shared application host.</summary>
	public override void Dispose() {
	}

	/// <summary>Releasing a borrowed window projection never disposes the shared application host.</summary>
	public override ValueTask DisposeAsync() {
		return ValueTask.CompletedTask;
	}

	/// <summary>Supplies the local host to commands while application controllers and interactions retain their existing owner.</summary>
	private sealed class DetachedUserInterfaceServices : UserInterfaceServicesDecorator {
		private readonly DetachedApplicationScreenHost _host;

		// Routes interactions through the root services with this detached window's active-screen context.
		internal DetachedUserInterfaceServices(IUserInterfaceServices services, DetachedApplicationScreenHost host)
			: base(services) {
			_host = host;
		}

		public override SharedUI.IApplicationScreenHost ScreenHost => _host;

		/// <summary>Executes with this window's active-screen context while navigation delegates through its root-backed host.</summary>
		public override Task<bool> ExecuteMenuItemAsync(SharedUI.IApplicationMenuItem item, CancellationToken cancellationToken = default) {
			return WinFormsCommandControls.ExecuteMenuItemAsync(this, item, cancellationToken);
		}
	}
}
