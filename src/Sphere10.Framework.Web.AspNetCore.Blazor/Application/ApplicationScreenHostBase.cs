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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Hosts logical Blazor screen proxies while the renderer owns their component instances.</summary>
public abstract class ApplicationScreenHostBase : SharedUI.ApplicationScreenHost<IApplicationBlock, ApplicationScreenSession>, IApplicationScreenHost {
	private BlazorScreenMenuItem _menuItem;
	private BlazorUserInterfaceServices _ownedUserInterface;

	protected ApplicationScreenHostBase(IServiceProvider services)
		: this(Array.Empty<IApplicationBlock>(), services) {
	}

	protected ApplicationScreenHostBase(IEnumerable<IApplicationBlock> blocks, IServiceProvider services)
		: base(ApplicationBlockSnapshot.CreateBlocks(blocks), services) {
	}

	/// <summary>The host's circuit-local commands and interactions, resolved lazily to avoid a host/service construction cycle.</summary>
	public override IBlazorUserInterfaceServices UserInterfaceServices {
		get {
			EnsureUsable();
			return Services.GetService<IBlazorUserInterfaceServices>() ?? (_ownedUserInterface ??= new BlazorUserInterfaceServices(this, Services));
		}
	}

	public virtual Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default) => SelectBlockAsync(GetRegisteredBlock(blockId), cancellationToken);

	public virtual Task<ApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) =>
		ActivateBlockAsync(GetRegisteredBlock(blockId), cancellationToken);

	public virtual Task<ApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) =>
		ActivateScreenAsync(blockId, screenMenuItemId, null, cancellationToken);

	public virtual Task<ApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, ApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) {
		var block = GetRegisteredBlock(blockId);
		var definition = ApplicationBlockSnapshot.ResolveScreenDefinition(block, screenMenuItemId);
		Guard.Argument(definition != null, nameof(screenMenuItemId), "The route does not identify a registered screen.");
		var item = ApplicationBlockSnapshot.EnumerateItems(block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems))
			.OfType<BlazorScreenMenuItem>().FirstOrDefault(candidate => candidate.Id == screenMenuItemId);
		return OpenScreenAsync(block, definition, item, activation, cancellationToken);
	}

	public virtual Task<ApplicationScreenSession> OpenScreenAsync(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition, ApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) => OpenScreenAsync(block, definition, null, activation, cancellationToken);

	public override Task<ApplicationScreenSession> OpenScreenAsync(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition,
		CancellationToken cancellationToken = default) => OpenScreenAsync(block, definition, null, null, cancellationToken);

	public override Task<ApplicationScreenSession> ActivateBlockAsync(IApplicationBlock block, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(block, nameof(block));
		var definition = GetDefaultScreen(block);
		return definition == null ? base.ActivateBlockAsync(block, cancellationToken) : OpenScreenAsync(block, definition, null, null, cancellationToken);
	}

	public override Task<ApplicationScreenSession> ActivateScreenAsync(IApplicationBlock block, Type screenType, string title = null,
		CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(block, nameof(block));
		var registration = ApplicationHelper.ResolveScreenRegistration(block.ScreenDefinitions, ApplicationScreenRegistration.ForType(screenType));
		return OpenScreenAsync(block, title == null ? registration : registration.WithTitle(title), null, null, cancellationToken, registration);
	}

	public override Task<ApplicationScreenSession> ActivateScreenAsync(IApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(item, nameof(item));
		return OpenScreenAsync(block, item.ScreenDefinition, item as BlazorScreenMenuItem, null, cancellationToken);
	}

	private Task<ApplicationScreenSession> OpenScreenAsync(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition, BlazorScreenMenuItem menuItem,
		ApplicationScreenActivation activation, CancellationToken cancellationToken, SharedUI.ApplicationScreenDefinition registration = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return RunTransitionAsync(async token => {
			Guard.Argument(block != null && Blocks.Any(candidate => ReferenceEquals(candidate, block)), nameof(block), "The block is not registered.");
			Guard.Argument(block.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, registration ?? definition)), nameof(definition), "The screen definition is not owned by this block.");
			var previous = CurrentActivation;
			var previousMenuItem = _menuItem;
			using var request = Tools.Scope.ExecuteOnDispose(() => {
				CurrentActivation = previous;
				_menuItem = previousMenuItem;
			});
			CurrentActivation = activation;
			_menuItem = menuItem;
			return await ActivateCoreAsync(block, definition, token, registration);
		}, cancellationToken);
	}

	public virtual Task<bool> ExecuteMenuItemAsync(SharedUI.IApplicationMenuItem item, CancellationToken cancellationToken = default) =>
		UserInterfaceServices.ExecuteMenuItemAsync(item, cancellationToken);

	public virtual Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => UnregisterBlockAsync(GetRegisteredBlock(blockId), cancellationToken);

	public virtual Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default) => RegisterBlockAsync(GetAvailableBlock(blockId), cancellationToken);

	public virtual Task MoveScreenAsync(Guid id, int index, CancellationToken cancellationToken = default) => MoveScreenAsync(GetScreen(id), index, cancellationToken);

	public virtual Task<bool> CloseScreensAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(ids, nameof(ids));
		return CloseScreensAsync(ids.Select(GetScreen).ToArray(), cancellationToken);
	}

	public virtual Task<bool> ShowScreenAsync(Guid id, CancellationToken cancellationToken = default) => ShowScreenAsync(GetScreen(id), cancellationToken);

	public virtual Task<bool> CloseScreenAsync(Guid id, CancellationToken cancellationToken = default) => CloseScreenAsync(GetScreen(id), cancellationToken);

	public virtual Task<bool> CanCloseScreenAsync(Guid id, CancellationToken cancellationToken = default) => CanCloseScreenAsync(GetScreen(id), cancellationToken);

	public virtual Task AttachScreenAsync(Guid id, IApplicationScreen component, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(component, nameof(component));
		return RunTransitionAsync(async token => {
			var screen = GetScreen(id);
			Guard.Ensure(screen.Screen == null || ReferenceEquals(screen.Screen, component), "A retained screen already has a renderer component.");
			if (ReferenceEquals(screen.Screen, component))
				return true;
			var attached = false;
			using var attachment = Tools.Scope.ExecuteOnDispose(() => {
				if (!attached)
					screen.SetScreen(null);
			});
			screen.SetScreen(component);
			if (ReferenceEquals(ActiveScreen, screen))
				await component.OnActivatedAsync(token);
			token.ThrowIfCancellationRequested();
			attached = true;
			NotifyScreenChanged(screen);
			return true;
		}, cancellationToken);
	}

	public virtual void DetachScreen(Guid id, IApplicationScreen component) {
		var screen = Screens.FirstOrDefault(candidate => candidate.Id == id);
		if (screen != null && ReferenceEquals(screen.Screen, component)) {
			screen.SetScreen(null);
			NotifyScreenChanged(screen);
		}
	}

	public virtual void NotifyScreenChanged(Guid id) => NotifyScreenChanged(GetScreen(id));

	public virtual ApplicationStartupScreen StartupScreen { get; private set; }

	public virtual bool CanMakeStartupScreen(ApplicationScreenSession screen) => GetStartupScreenUnavailableReason(screen) == null;

	public virtual string GetStartupScreenUnavailableReason(ApplicationScreenSession screen) {
		if (screen == null || !Screens.Any(candidate => ReferenceEquals(candidate, screen)) || !ReferenceEquals(screen.Host, this))
			return "This screen is no longer registered with the application.";
		if (!screen.CanRestoreAtStartup)
			return "This screen requires session-specific state and cannot be restored at startup.";
		if (screen.Definition.StableKey == null && screen.Block.ScreenDefinitions.Count(definition => definition.ScreenType == screen.ScreenType) > 1)
			return "Configure WithStableKey on this screen registration before choosing it as the startup screen.";
		var identity = CreateStartupScreenIdentity(screen);
		return ResolveStartupScreen(identity) == null ? "This screen is excluded by the application's startup policy." : null;
	}

	public virtual bool IsStartupScreen(ApplicationScreenSession screen) =>
		CanMakeStartupScreen(screen) && StartupScreen == CreateStartupScreenIdentity(screen);

	public virtual ApplicationStartupScreen GetStartupScreenIdentity(ApplicationScreenSession screen) {
		Guard.Ensure(CanMakeStartupScreen(screen), GetStartupScreenUnavailableReason(screen));
		return CreateStartupScreenIdentity(screen);
	}

	/// <summary>Updates circuit-local selection after a caller loads or successfully saves browser preferences.</summary>
	public virtual bool SetStartupScreen(ApplicationStartupScreen identity) {
		EnsureUsable();
		StartupScreen = ResolveStartupScreen(identity) != null ? identity : null;
		OnChanged();
		return StartupScreen != null;
	}

	/// <summary>Override to exclude registrations requiring application-specific state before a component is constructed.</summary>
	protected virtual bool CanRestoreScreenAtStartup(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition) => definition.ScreenKind == ScreenKind.Normal;

	private static ApplicationStartupScreen CreateStartupScreenIdentity(ApplicationScreenSession screen) {
		return new ApplicationStartupScreen(screen.Block.Source?.Name ?? string.Empty, screen.Block.Id, null, screen.ScreenType.FullName,
			screen.ScreenType.Assembly.GetName().Name, ApplicationHelper.GetScreenRegistrationKey(screen.Block, screen.Definition));
	}

	private SharedUI.ApplicationScreenDefinition ResolveStartupScreen(ApplicationStartupScreen identity) {
		if (identity == null || string.IsNullOrEmpty(identity.BlockId)
			|| string.IsNullOrEmpty(identity.ScreenTypeName) || string.IsNullOrEmpty(identity.ScreenAssemblyName))
			return null;
		var block = Blocks.FirstOrDefault(candidate => candidate.Id == identity.BlockId && (candidate.Source?.Name ?? string.Empty) == (identity.PluginName ?? string.Empty));
		if (block == null)
			return null;
		var matches = block.ScreenDefinitions.Where(definition => definition.ScreenType.FullName == identity.ScreenTypeName
			&& definition.ScreenType.Assembly.GetName().Name == identity.ScreenAssemblyName).ToArray();
		// Legacy type-only choices can migrate only when the current registration is unambiguous.
		if (string.IsNullOrEmpty(identity.RegistrationKey))
			return matches.Length == 1 && (string.IsNullOrEmpty(identity.MenuItemId)
				|| identity.MenuItemId.StartsWith(ApplicationBlockSnapshot.DefinitionScreenItemPrefix, StringComparison.Ordinal)
				&& int.TryParse(identity.MenuItemId[ApplicationBlockSnapshot.DefinitionScreenItemPrefix.Length..], out _)
				|| ReferenceEquals(ApplicationBlockSnapshot.ResolveScreenDefinition(block, identity.MenuItemId), matches[0]))
				&& CanRestoreScreenAtStartup(block, matches[0]) ? matches[0] : null;
		var selected = matches.SingleOrDefault(definition => definition.StableKey == identity.RegistrationKey
			|| definition.StableKey == null && matches.Length == 1 && ApplicationHelper.GetScreenRegistrationKey(block, definition) == identity.RegistrationKey);
		return selected != null && CanRestoreScreenAtStartup(block, selected) ? selected : null;
	}

	protected ApplicationScreenActivation CurrentActivation { get; private set; }

	protected override ApplicationScreenSession FindReusableScreen(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition, SharedUI.ApplicationScreenDefinition registration) {
		var reusable = base.FindReusableScreen(block, definition, registration);
		if (reusable != null || definition.ActivationMode != ScreenActivationMode.MultiInstance || CurrentActivation?.InstanceKey == null)
			return reusable;
		// Instance identity comes from the retained sessions; failed preparation cannot leave a second retention index behind.
		return Screens.FirstOrDefault(screen => ReferenceEquals(screen.Block, block)
			&& screen.Definition.Registration.Token == registration.Registration.Token
			&& string.Equals(screen.InstanceKey, CurrentActivation.InstanceKey, StringComparison.Ordinal));
	}

	protected override void OnScreenRemoved(ApplicationScreenSession screen) {
		screen.SetScreen(null);
		base.OnScreenRemoved(screen);
	}

	protected override SharedUI.ApplicationScreenDefinition[] GetDefinitions() {
		return GetDefinitions(ApplicationBlockSnapshot.OrderBlocks(Services.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Blocks));
	}

	protected override SharedUI.ApplicationScreenDefinition GetStartupScreen() {
		var startup = ResolveStartupScreen(StartupScreen);
		if (startup != null)
			return startup;
		var blocks = ApplicationBlockSnapshot.OrderBlocks(Services.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Blocks);
		return ApplicationHelper.GetDefaultScreen(blocks) ?? (blocks.FirstOrDefault() is { } first ? GetDefaultScreen(first) : null);
	}

	protected override void ValidateScreenType(Type screenType) => ApplicationHelper.ValidateScreenType(screenType, typeof(IApplicationScreen));

	protected override void NotifyScreenActivated(ApplicationScreenSession screen) {
		base.NotifyScreenActivated(screen);
		_menuItem?.NotifySelect();
	}

	protected BlazorScreenMenuItem GetScreenMenuItem(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition) {
		if (_menuItem != null)
			return _menuItem;
		return definition.Activators.OfType<BlazorScreenMenuItem>().FirstOrDefault(item => ReferenceEquals(item.Parent?.Parent, block));
	}

	private IApplicationBlock GetRegisteredBlock(string blockId) => GetBlock(Blocks, blockId);

	private IApplicationBlock GetAvailableBlock(string blockId) => GetBlock(AvailableBlocks, blockId);

	private static IApplicationBlock GetBlock(IEnumerable<IApplicationBlock> blocks, string blockId) {
		Guard.ArgumentNotNullOrEmpty(blockId, nameof(blockId));
		var block = blocks.FirstOrDefault(candidate => candidate.Id == blockId);
		Guard.Argument(block != null, nameof(blockId), $"Unknown block '{blockId}'.");
		return block;
	}

	protected ApplicationScreenSession GetScreen(Guid id) {
		var screen = Screens.FirstOrDefault(candidate => candidate.Id == id);
		Guard.Argument(screen != null, nameof(id), "The screen is no longer retained by this host.");
		return screen;
	}

	// The shared host invokes this only after every close guard accepts and before ownership changes.
	protected override async Task OnClosingScreensAsync(ApplicationScreenSession[] screens, CancellationToken cancellationToken) {
		foreach (var screen in screens.Select(session => session.Screen).OfType<ApplicationScreen>())
			await screen.SaveSettingsAsync(cancellationToken);
	}

	protected override void DisposeInternal() {
		_ownedUserInterface?.Dispose();
		foreach (var screen in Screens)
			screen.SetScreen(null);
		base.DisposeInternal();
	}
}
