// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

public abstract class ApplicationDecorator<TConcrete> : IApplication
	where TConcrete : IApplication {
	public virtual event EventHandlerEx Changed {
		add => InternalApplication.Changed += value;
		remove => InternalApplication.Changed -= value;
	}

	public virtual event EventHandlerEx Initializing {
		add => InternalApplication.Initializing += value;
		remove => InternalApplication.Initializing -= value;
	}

	public virtual event EventHandlerEx Initialized {
		add => InternalApplication.Initialized += value;
		remove => InternalApplication.Initialized -= value;
	}

	public virtual event EventHandlerEx Finalizing {
		add => InternalApplication.Finalizing += value;
		remove => InternalApplication.Finalizing -= value;
	}

	public virtual event EventHandlerEx Finalized {
		add => InternalApplication.Finalized += value;
		remove => InternalApplication.Finalized -= value;
	}

	public virtual event ProgressHandler Progress {
		add => InternalApplication.Progress += value;
		remove => InternalApplication.Progress -= value;
	}

	protected readonly TConcrete InternalApplication;

	protected ApplicationDecorator(TConcrete application) {
		Guard.ArgumentNotNull(application, nameof(application));
		InternalApplication = application;
	}

	public virtual IApplicationPlugin[] LoadedPlugins => InternalApplication.LoadedPlugins;

	public virtual ILogger Logger => InternalApplication.Logger;

	public virtual Disposables Disposables => InternalApplication.Disposables;

	public virtual bool IsInitialized => InternalApplication.IsInitialized;

	public virtual bool IsExiting => InternalApplication.IsExiting;

	public virtual bool HasExited => InternalApplication.HasExited;

	public virtual IApplicationBlock[] Blocks => InternalApplication.Blocks;

	public virtual IApplicationScreenHost ScreenHost => InternalApplication.ScreenHost;

	public virtual bool HasUnsavedChanges => InternalApplication.HasUnsavedChanges;

	public virtual IApplicationMenu[] Menus => InternalApplication.Menus;

	public virtual IApplicationMenuItem[] ToolBarItems => InternalApplication.ToolBarItems;

	public virtual void Initialize() => InternalApplication.Initialize();

	public virtual Task InitializeAsync(CancellationToken cancellationToken = default) => InternalApplication.InitializeAsync(cancellationToken);

	public virtual bool Exit(bool force = false, CancellationToken cancellationToken = default) => InternalApplication.Exit(force, cancellationToken);

	public virtual Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default) => InternalApplication.ExitAsync(force, cancellationToken);

	public virtual Task FinalizeAsync(CancellationToken cancellationToken = default) => InternalApplication.FinalizeAsync(cancellationToken);

#pragma warning disable CS0465 // Preserve the existing synchronous application lifecycle method name.
	public virtual void Finalize() => InternalApplication.Finalize();
#pragma warning restore CS0465

	public virtual void Dispose() => InternalApplication.Dispose();

	public virtual ValueTask DisposeAsync() => InternalApplication.DisposeAsync();
}

public abstract class ApplicationDecorator : ApplicationDecorator<IApplication> {
	protected ApplicationDecorator(IApplication application)
		: base(application) {
	}
}
