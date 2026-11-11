// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Forwards neutral host operations through an overridable existing host.</summary>
public abstract class ApplicationScreenHostDecorator<TConcrete> : IApplicationScreenHost
	where TConcrete : IApplicationScreenHost {
	/// <inheritdoc />
	public virtual event EventHandlerEx Changed {
		add {
			InternalHost.Changed += value;
		}
		remove {
			InternalHost.Changed -= value;
		}
	}

	/// <inheritdoc />
	public virtual event EventHandlerEx<IApplicationScreen> ScreenSelected {
		add {
			InternalHost.ScreenSelected += value;
		}
		remove {
			InternalHost.ScreenSelected -= value;
		}
	}

	/// <summary>The borrowed host forwarded by this decorator.</summary>
	protected readonly TConcrete InternalHost;

	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
	protected ApplicationScreenHostDecorator(TConcrete internalHost) {
		Guard.ArgumentNotNull(internalHost, nameof(internalHost));
		InternalHost = internalHost;
	}

	/// <inheritdoc />
	public virtual IApplicationBlock[] AvailableBlocks => InternalHost.AvailableBlocks;

	/// <inheritdoc />
	public virtual IApplicationBlock[] Blocks => InternalHost.Blocks;

	/// <inheritdoc />
	public virtual IApplicationBlock ActiveBlock => InternalHost.ActiveBlock;

	/// <inheritdoc />
	public virtual IApplicationBlock SelectedBlock => InternalHost.SelectedBlock;

	/// <inheritdoc />
	public virtual IApplicationScreen ActiveScreen => InternalHost.ActiveScreen;

	/// <inheritdoc />
	public virtual IUserInterfaceServices UserInterfaceServices => InternalHost.UserInterfaceServices;

	/// <inheritdoc />
	public virtual ScreenMode ScreenMode {
		get => InternalHost.ScreenMode;
		set => InternalHost.ScreenMode = value;
	}

	/// <inheritdoc />
	public virtual bool HasUnsavedChanges => InternalHost.HasUnsavedChanges;

	/// <summary>Initializes registered screen policies and the initial selection through the guarded transition owner.</summary>
	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => InternalHost.InitializeAsync(cancellationToken);

	/// <summary>Changes the browsed navigation block independently of the visible screen.</summary>
	public virtual Task SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default)
		=> InternalHost.SelectBlockAsync(block, cancellationToken);

	/// <summary>Activates the block's registered default or selects the block when no default is declared.</summary>
	public virtual Task<IApplicationScreen> ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default)
		=> InternalHost.ActivateBlockAsync(block, cancellationToken);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	public virtual Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default)
		=> InternalHost.ActivateScreenAsync(block, screenType, title, cancellationToken);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	public virtual Task<IApplicationScreen> ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default)
		=> InternalHost.ActivateScreenAsync(block, item, cancellationToken);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	public virtual Task<IApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default)
		=> InternalHost.OpenScreenAsync(block, definition, cancellationToken);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	public virtual Task<IApplicationScreen> OpenScreenAsync(IApplicationBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default)
		=> InternalHost.OpenScreenAsync(block, registration, cancellationToken);

	/// <summary>Shows a retained screen through the platform's detached presentation support.</summary>
	public virtual Task<bool> ShowScreenUndockedAsync(IApplicationScreen screen, CancellationToken cancellationToken = default)
		=> InternalHost.ShowScreenUndockedAsync(screen, cancellationToken);

	/// <summary>Registers an available block and its canonical screen definitions.</summary>
	public virtual Task RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default)
		=> InternalHost.RegisterBlockAsync(block, cancellationToken);

	/// <summary>Closes a registered block's screens through their guards before removing the registration.</summary>
	public virtual Task<bool> UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default)
		=> InternalHost.UnregisterBlockAsync(block, cancellationToken);

	/// <summary>Clears the current presentation after its navigation guard accepts.</summary>
	public virtual Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) => InternalHost.ClearSelectionAsync(cancellationToken);

	/// <summary>Changes presentation mode after any required screen closures are accepted.</summary>
	public virtual Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default)
		=> InternalHost.TrySetScreenModeAsync(mode, cancellationToken);

	/// <summary>Checks whether the current presentation permits navigation.</summary>
	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => InternalHost.CanNavigateAsync(cancellationToken);

	/// <summary>Publishes presentation changes for a retained screen.</summary>
	public virtual void NotifyScreenChanged(IApplicationScreen screen) => InternalHost.NotifyScreenChanged(screen);

	/// <summary>Disposes the wrapped host through its existing ownership contract.</summary>
	public virtual void Dispose() => InternalHost.Dispose();

	/// <summary>Awaits disposal of the wrapped host.</summary>
	public virtual ValueTask DisposeAsync() => InternalHost.DisposeAsync();
}

/// <summary>Forwards typed host operations while retaining neutral interface dispatch.</summary>
public abstract class ApplicationScreenHostDecorator<TBlock, TScreen, TConcrete> : IApplicationScreenHost<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen
	where TConcrete : IApplicationScreenHost<TBlock, TScreen> {
	/// <inheritdoc />
	public virtual event EventHandlerEx Changed {
		add {
			InternalHost.Changed += value;
		}
		remove {
			InternalHost.Changed -= value;
		}
	}

	/// <inheritdoc />
	public virtual event EventHandlerEx<IApplicationScreen> ScreenSelected {
		add {
			InternalHost.ScreenSelected += value;
		}
		remove {
			InternalHost.ScreenSelected -= value;
		}
	}

	/// <summary>The borrowed host forwarded by this decorator.</summary>
	protected readonly TConcrete InternalHost;

	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
	protected ApplicationScreenHostDecorator(TConcrete internalHost) {
		Guard.ArgumentNotNull(internalHost, nameof(internalHost));
		InternalHost = internalHost;
	}

	/// <inheritdoc />
	public virtual TBlock[] AvailableBlocks => InternalHost.AvailableBlocks;

	/// <inheritdoc />
	public virtual TBlock[] Blocks => InternalHost.Blocks;

	/// <inheritdoc />
	public virtual TBlock ActiveBlock => InternalHost.ActiveBlock;

	/// <inheritdoc />
	public virtual TBlock SelectedBlock => InternalHost.SelectedBlock;

	/// <inheritdoc />
	public virtual TScreen ActiveScreen => InternalHost.ActiveScreen;

	/// <inheritdoc />
	public virtual IUserInterfaceServices UserInterfaceServices => InternalHost.UserInterfaceServices;

	/// <inheritdoc />
	public virtual ScreenMode ScreenMode {
		get => InternalHost.ScreenMode;
		set => InternalHost.ScreenMode = value;
	}

	/// <inheritdoc />
	public virtual bool HasUnsavedChanges => InternalHost.HasUnsavedChanges;

	/// <summary>Initializes registered screen policies and the initial selection through the guarded transition owner.</summary>
	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => InternalHost.InitializeAsync(cancellationToken);

	/// <summary>Changes the browsed navigation block independently of the visible screen.</summary>
	public virtual Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default) => InternalHost.SelectBlockAsync(block, cancellationToken);

	/// <summary>Activates the block's registered default or selects the block when no default is declared.</summary>
	public virtual Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default)
		=> InternalHost.ActivateBlockAsync(block, cancellationToken);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	public virtual Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default)
		=> InternalHost.ActivateScreenAsync(block, screenType, title, cancellationToken);

	/// <summary>Resolves a registered screen and activates it through the host's existing lifetime policy.</summary>
	public virtual Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default)
		=> InternalHost.ActivateScreenAsync(block, item, cancellationToken);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	public virtual Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default)
		=> InternalHost.OpenScreenAsync(block, definition, cancellationToken);

	/// <summary>Opens the exact registration owned by the supplied block.</summary>
	public virtual Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default)
		=> InternalHost.OpenScreenAsync(block, registration, cancellationToken);

	/// <summary>Shows a retained screen through the platform's detached presentation support.</summary>
	public virtual Task<bool> ShowScreenUndockedAsync(TScreen screen, CancellationToken cancellationToken = default)
		=> InternalHost.ShowScreenUndockedAsync(screen, cancellationToken);

	/// <summary>Registers an available block and its canonical screen definitions.</summary>
	public virtual Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default) => InternalHost.RegisterBlockAsync(block, cancellationToken);

	/// <summary>Closes a registered block's screens through their guards before removing the registration.</summary>
	public virtual Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default)
		=> InternalHost.UnregisterBlockAsync(block, cancellationToken);

	/// <summary>Clears the current presentation after its navigation guard accepts.</summary>
	public virtual Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) => InternalHost.ClearSelectionAsync(cancellationToken);

	/// <summary>Changes presentation mode after any required screen closures are accepted.</summary>
	public virtual Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default)
		=> InternalHost.TrySetScreenModeAsync(mode, cancellationToken);

	/// <summary>Checks whether the current presentation permits navigation.</summary>
	public virtual Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) => InternalHost.CanNavigateAsync(cancellationToken);

	/// <summary>Publishes presentation changes for a retained screen.</summary>
	public virtual void NotifyScreenChanged(TScreen screen) => InternalHost.NotifyScreenChanged(screen);

	/// <summary>Disposes the wrapped host through its existing ownership contract.</summary>
	public virtual void Dispose() => InternalHost.Dispose();

	/// <summary>Awaits disposal of the wrapped host.</summary>
	public virtual ValueTask DisposeAsync() => InternalHost.DisposeAsync();

	#region Base Interface Bridges
	// Preserve neutral calls to virtual class members when a subclass explicitly remaps the typed interface.
	IApplicationBlock[] IApplicationScreenHost.AvailableBlocks => AvailableBlocks;

	/// <inheritdoc />
	IApplicationBlock[] IApplicationScreenHost.Blocks => Blocks;

	/// <inheritdoc />
	IApplicationBlock IApplicationScreenHost.ActiveBlock => ActiveBlock;

	/// <inheritdoc />
	IApplicationBlock IApplicationScreenHost.SelectedBlock => SelectedBlock;

	/// <inheritdoc />
	IApplicationScreen IApplicationScreenHost.ActiveScreen => ActiveScreen;

	/// <inheritdoc />
	Task IApplicationScreenHost.SelectBlockAsync(IApplicationBlock block, CancellationToken cancellationToken)
		=> SelectBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken)
		=> await ActivateBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.ActivateScreenAsync(IApplicationBlock block, Type screenType, string title, CancellationToken cancellationToken)
		=> await ActivateScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), screenType, title, cancellationToken);

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken)
		=> await ActivateScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), item, cancellationToken);

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.OpenScreenAsync(IApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken)
		=> await OpenScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), definition, cancellationToken);

	/// <inheritdoc />
	async Task<IApplicationScreen> IApplicationScreenHost.OpenScreenAsync(IApplicationBlock block, ApplicationScreenRegistration registration, CancellationToken cancellationToken)
		=> await OpenScreenAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), registration, cancellationToken);

	/// <inheritdoc />
	Task<bool> IApplicationScreenHost.ShowScreenUndockedAsync(IApplicationScreen screen, CancellationToken cancellationToken)
		=> ShowScreenUndockedAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);

	/// <inheritdoc />
	Task IApplicationScreenHost.RegisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken)
		=> RegisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	/// <inheritdoc />
	Task<bool> IApplicationScreenHost.UnregisterBlockAsync(IApplicationBlock block, CancellationToken cancellationToken)
		=> UnregisterBlockAsync(Guard.ArgumentCast<TBlock>(block, nameof(block)), cancellationToken);

	/// <inheritdoc />
	void IApplicationScreenHost.NotifyScreenChanged(IApplicationScreen screen)
		=> NotifyScreenChanged(screen == null ? null : Guard.ArgumentCast<TScreen>(screen, nameof(screen)));
	#endregion
}

public abstract class ApplicationScreenHostDecorator : ApplicationScreenHostDecorator<IApplicationScreenHost> {
	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
	protected ApplicationScreenHostDecorator(IApplicationScreenHost internalHost)
		: base(internalHost) {
	}
}

public abstract class ApplicationScreenHostDecorator<TBlock, TScreen> : ApplicationScreenHostDecorator<TBlock, TScreen, IApplicationScreenHost<TBlock, TScreen>>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
	protected ApplicationScreenHostDecorator(IApplicationScreenHost<TBlock, TScreen> internalHost)
		: base(internalHost) {
	}
}
