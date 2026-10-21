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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application;

/// <summary>Forwards application interactions and command execution, borrowing the decorated service.</summary>
public abstract class UserInterfaceServicesDecorator<TConcrete> : IUserInterfaceServices
	where TConcrete : IUserInterfaceServices {
	protected readonly TConcrete InternalServices;

	protected UserInterfaceServicesDecorator(TConcrete services) {
		Guard.ArgumentNotNull(services, nameof(services));
		InternalServices = services;
	}

	public virtual event EventHandlerEx Changed {
		add => InternalServices.Changed += value;
		remove => InternalServices.Changed -= value;
	}

	public virtual IApplicationScreenHost ScreenHost => InternalServices.ScreenHost;

	public virtual object PrimaryUIController => InternalServices.PrimaryUIController;

	public virtual bool ApplicationExiting {
		get => InternalServices.ApplicationExiting;
		set => InternalServices.ApplicationExiting = value;
	}

	public virtual string Status {
		get => InternalServices.Status;
		set => InternalServices.Status = value;
	}

	public virtual Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) => InternalServices.ExecuteMenuItemAsync(item, cancellationToken);


	public virtual void Exit(bool force = false) => InternalServices.Exit(force);

	public virtual Task ShowSendCommentDialog() => InternalServices.ShowSendCommentDialog();

	public virtual Task ShowSubmitBugReportDialog() => InternalServices.ShowSubmitBugReportDialog();

	public virtual Task ShowRequestFeatureDialog() => InternalServices.ShowRequestFeatureDialog();

	public virtual Task ShowAboutBox() => InternalServices.ShowAboutBox();

	public virtual void ReportError(Exception error) => InternalServices.ReportError(error);

	public virtual void ReportError(string message) => InternalServices.ReportError(message);

	public virtual void ReportError(string title, string message) => InternalServices.ReportError(title, message);

	public virtual void ReportFatalError(string title, string message) => InternalServices.ReportFatalError(title, message);

	public virtual void ReportInfo(string title, string message) => InternalServices.ReportInfo(title, message);

	public virtual bool AskYN(string question) => InternalServices.AskYN(question);

	public virtual Task<bool> AskYNAsync(string question, CancellationToken cancellationToken = default) => InternalServices.AskYNAsync(question, cancellationToken);

	public virtual Task ReportErrorAsync(string title, string message, CancellationToken cancellationToken = default) => InternalServices.ReportErrorAsync(title, message, cancellationToken);

	public virtual Task ReportInfoAsync(string title, string message, CancellationToken cancellationToken = default) => InternalServices.ReportInfoAsync(title, message, cancellationToken);

	public virtual void ExecuteInUIFriendlyContext(Action action, bool executeAsync = false) => InternalServices.ExecuteInUIFriendlyContext(action, executeAsync);

	public virtual Task ExecuteInUIFriendlyContextAsync(Func<Task> action, CancellationToken cancellationToken = default) => InternalServices.ExecuteInUIFriendlyContextAsync(action, cancellationToken);

}

public abstract class UserInterfaceServicesDecorator : UserInterfaceServicesDecorator<IUserInterfaceServices> {
	protected UserInterfaceServicesDecorator(IUserInterfaceServices services) : base(services) {
	}
}
