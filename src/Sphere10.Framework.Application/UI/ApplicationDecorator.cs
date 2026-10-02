// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

public abstract class ApplicationDecorator<TConcrete> : IApplication where TConcrete : IApplication {
	public virtual event EventHandlerEx Changed {
		add => InternalApplication.Changed += value;
		remove => InternalApplication.Changed -= value;
	}

	protected readonly TConcrete InternalApplication;

	protected ApplicationDecorator(TConcrete application) {
		Guard.ArgumentNotNull(application, nameof(application));
		InternalApplication = application;
	}

	public virtual IApplicationPlugin[] Plugins => InternalApplication.Plugins;

	public virtual IApplicationPlugin ActivePlugin => InternalApplication.ActivePlugin;

	public virtual IApplicationBlock[] Blocks => InternalApplication.Blocks;

	public virtual IApplicationBlock ActiveBlock => InternalApplication.ActiveBlock;

	public virtual IApplicationScreen ActiveScreen => InternalApplication.ActiveScreen;

	public virtual bool HasUnsavedChanges => InternalApplication.HasUnsavedChanges;

	public virtual IApplicationMenu[] Menus => InternalApplication.Menus;

	public virtual IApplicationMenuItem[] ToolBarItems => InternalApplication.ToolBarItems;

	public virtual void Dispose() => InternalApplication.Dispose();

	public virtual ValueTask DisposeAsync() => InternalApplication.DisposeAsync();
}

public abstract class ApplicationDecorator : ApplicationDecorator<IApplication> {
	protected ApplicationDecorator(IApplication application)
		: base(application) {
	}
}
