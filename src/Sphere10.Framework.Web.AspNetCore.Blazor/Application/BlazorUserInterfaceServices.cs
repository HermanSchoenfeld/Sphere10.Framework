// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
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

	/// <summary>Legacy browser commands can resolve local IDs; portable commands use their owned definition references.</summary>
	public override Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(item, nameof(item));
		var host = (IBlazorApplicationScreenHost)ScreenHost;
		if (item is BlazorScreenMenuItem supplied && !host.Blocks.Any(block => ReferenceEquals(block, supplied.Parent?.Parent))) {
			if (!supplied.IsEnabled)
				return Task.FromResult(false);
			var owner = supplied.Parent?.Parent is { } sourceBlock
				? host.Blocks.FirstOrDefault(block => block.Id == sourceBlock.Id)
				: host.ActiveScreen?.Block ?? host.ActiveBlock;
			var blocks = owner == null ? host.Blocks : new[] { owner };
			var candidates = blocks.SelectMany(block => BlazorApplicationBlockSnapshot.EnumerateItems(block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems)))
				.OfType<BlazorScreenMenuItem>().Where(command => command.Id == supplied.Id && command.ScreenType == supplied.ScreenType
					&& command.ActivationMode == supplied.ActivationMode && command.ScreenKind == supplied.ScreenKind).ToArray();
			Guard.Argument(candidates.Length == 1, nameof(item), "The browser screen command does not identify a registered command unambiguously.");
			item = candidates[0];
		}
		return base.ExecuteMenuItemAsync(item, cancellationToken);
	}

	public IDisposable AttachController(object controller, Func<Func<Task>, Task> dispatcher) {
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
		await ExecuteInUIFriendlyContextAsync(async () => answer = await Services.GetRequiredService<Modal.ViewService>().ConfirmDialogAsync("Confirm", question, "Yes"), cancellationToken);
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

	public virtual Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrEmpty(blockId, nameof(blockId));
		var block = ((IBlazorApplicationScreenHost)ScreenHost).Blocks.FirstOrDefault(candidate => candidate.Id == blockId);
		Guard.Argument(block != null, nameof(blockId), $"Unknown block '{blockId}'.");
		return ExecuteMenuItemAsync(BlazorApplicationBlockSnapshot.ResolveMenuItem(block, menuItemId), cancellationToken);
	}

	private void ObserveInteraction(Task interaction) => _ = ObserveInteractionAsync(interaction);

	private async Task ObserveInteractionAsync(Task interaction) {
		try {
			await interaction;
		} catch (Exception error) {
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
