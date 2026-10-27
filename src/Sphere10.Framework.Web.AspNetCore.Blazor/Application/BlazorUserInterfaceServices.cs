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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Circuit-local command execution and asynchronous modal interaction using the borrowed screen host.</summary>
public class BlazorUserInterfaceServices : ApplicationUserInterfaceServices, IBlazorUserInterfaceServices {
	private object _controller;
	private Func<Func<Task>, Task> _dispatcher;
	private object _controllerBinding;
	private readonly ILogger<BlazorUserInterfaceServices> _logger;

	public BlazorUserInterfaceServices(IBlazorApplicationScreenHost screenHost, IServiceProvider services)
		: base(screenHost, services) {
		_logger = services.GetService<ILogger<BlazorUserInterfaceServices>>();
	}

	public override object PrimaryUIController => _controller;

	public override bool ApplicationExiting {
		get {
			var application = Services.GetService<IApplication>();
			return base.ApplicationExiting || application?.IsExiting == true || application?.HasExited == true;
		}
		set => base.ApplicationExiting = value;
	}

	/// <summary>Legacy exit commands are observed asynchronously; callers requiring the result await IApplication.ExitAsync.</summary>
	public override void Exit(bool force = false) => ObserveInteraction(Services.GetRequiredService<IApplication>().ExitAsync(force));

	public ActionScope AttachController(object controller, Func<Func<Task>, Task> dispatcher) {
		Guard.ArgumentNotNull(controller, nameof(controller));
		Guard.ArgumentNotNull(dispatcher, nameof(dispatcher));
		_controller = controller;
		_dispatcher = dispatcher;
		var binding = _controllerBinding = new object();
		return Tools.Scope.ExecuteOnDispose(() => {
			if (ReferenceEquals(_controllerBinding, binding)) {
				_controller = null;
				_dispatcher = null;
				_controllerBinding = null;
			}
		});
	}

	public override async Task ExecuteInUIFriendlyContextAsync(Func<Task> action, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(action, nameof(action));
		cancellationToken.ThrowIfCancellationRequested();
		Guard.Ensure(_dispatcher != null, "An application shell must attach its renderer dispatcher before UI interactions are used.");
		await _dispatcher(async () => {
			cancellationToken.ThrowIfCancellationRequested();
			await action();
		});
	}

	public override void ExecuteInUIFriendlyContext(Action action, bool executeAsync = false) {
		Guard.ArgumentNotNull(action, nameof(action));
		ObserveInteraction(ExecuteInUIFriendlyContextAsync(() => {
			action();
			return Task.CompletedTask;
		}));
	}

	public override Task ReportErrorAsync(string title, string message, CancellationToken cancellationToken = default) =>
		ExecuteInUIFriendlyContextAsync(() => Services.GetRequiredService<Modal.ViewService>().DialogAsync(title, message), cancellationToken);

	public override Task ReportInfoAsync(string title, string message, CancellationToken cancellationToken = default) =>
		ExecuteInUIFriendlyContextAsync(() => Services.GetRequiredService<Modal.ViewService>().DialogAsync(title, message), cancellationToken);

	public override async Task<bool> AskYNAsync(string question, CancellationToken cancellationToken = default) {
		var answer = false;
		await ExecuteInUIFriendlyContextAsync(async () => answer = await Services.GetRequiredService<Modal.ViewService>()
			.ConfirmDialogAsync("Confirm", question, "Yes", cancellationToken), cancellationToken);
		return answer;
	}

	/// <summary>A synchronous modal would block the renderer; Blazor callers must await AskYNAsync.</summary>
	public override bool AskYN(string question) => throw new NotSupportedException("Blazor confirmation dialogs must be awaited with AskYNAsync.");

	public override void ReportError(Exception error) {
		Guard.ArgumentNotNull(error, nameof(error));
		ObserveInteraction(ReportErrorAsync("Error", error.Message));
	}

	public override void ReportError(string message) => ReportError("Error", message);
	public override void ReportError(string title, string message) => ObserveInteraction(ReportErrorAsync(title, message));
	public override void ReportFatalError(string title, string message) => ReportError(title, message);
	public override void ReportInfo(string title, string message) => ObserveInteraction(ReportInfoAsync(title, message));

	protected override bool IsMenuItemEnabled(IApplicationMenuItem item) => Guard.ArgumentCast<IBlazorApplicationMenuItem>(item, nameof(item)).IsEnabled;

	protected override Task ExecuteActionAsync(IApplicationMenuItem item, CancellationToken cancellationToken) =>
		Guard.ArgumentCast<BlazorActionMenuItem>(item, nameof(item)).ExecuteAsync(Services, cancellationToken);

	private void ObserveInteraction(Task interaction) => _ = ObserveInteractionAsync(interaction);

	private async Task ObserveInteractionAsync(Task interaction) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => interaction);
		if (error != null) {
			_logger?.LogError(error, "A legacy UI interaction failed. Await the asynchronous interaction API to handle errors in the caller.");
		}
	}

	protected override void FreeManagedResources() {
		_controller = null;
		_dispatcher = null;
		_controllerBinding = null;
		base.FreeManagedResources();
	}
}
