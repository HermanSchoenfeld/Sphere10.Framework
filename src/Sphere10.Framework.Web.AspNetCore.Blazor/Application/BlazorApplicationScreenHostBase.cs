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

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Hosts logical Blazor screen proxies while the renderer owns their component instances.</summary>
public abstract class BlazorApplicationScreenHostBase : ApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession>, IBlazorApplicationScreenHost {
	private BlazorApplicationScreenActivation _activation;
	private BlazorScreenMenuItem _menuItem;
	private BlazorUserInterfaceServices _ownedUserInterface;

	protected BlazorApplicationScreenHostBase(IServiceProvider services)
		: this(Array.Empty<IBlazorApplicationBlock>(), services) {
	}

	protected BlazorApplicationScreenHostBase(IEnumerable<IBlazorApplicationBlock> blocks, IServiceProvider services)
		: base(BlazorApplicationBlockSnapshot.CreateBlocks(blocks), services) {
	}

	/// <summary>The host's circuit-local commands and interactions, resolved lazily to avoid a host/service construction cycle.</summary>
	public override IBlazorUserInterfaceServices UserInterfaceServices {
		get {
			EnsureUsable();
			return Services.GetService<IBlazorUserInterfaceServices>() ?? (_ownedUserInterface ??= new BlazorUserInterfaceServices(this, Services));
		}
	}

	public virtual Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default) => SelectBlockAsync(GetRegisteredBlock(blockId), cancellationToken);

	public virtual Task<BlazorApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) =>
		ActivateBlockAsync(GetRegisteredBlock(blockId), cancellationToken);

	public virtual Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) =>
		ActivateScreenAsync(blockId, screenMenuItemId, null, cancellationToken);

	public virtual Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, BlazorApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) {
		var block = GetRegisteredBlock(blockId);
		var definition = BlazorApplicationBlockSnapshot.ResolveScreenDefinition(block, screenMenuItemId);
		Guard.Argument(definition != null, nameof(screenMenuItemId), "The route does not identify a registered screen.");
		var item = BlazorApplicationBlockSnapshot.EnumerateItems(block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems))
			.OfType<BlazorScreenMenuItem>().FirstOrDefault(candidate => candidate.Id == screenMenuItemId);
		return OpenScreenAsync(block, definition, item, activation, cancellationToken);
	}

	public virtual Task<BlazorApplicationScreenSession> OpenScreenAsync(IBlazorApplicationBlock block, ApplicationScreenDefinition definition, BlazorApplicationScreenActivation activation,
		CancellationToken cancellationToken = default) => OpenScreenAsync(block, definition, null, activation, cancellationToken);

	public override Task<BlazorApplicationScreenSession> OpenScreenAsync(IBlazorApplicationBlock block, ApplicationScreenDefinition definition,
		CancellationToken cancellationToken = default) => OpenScreenAsync(block, definition, null, null, cancellationToken);

	public override Task<BlazorApplicationScreenSession> ActivateBlockAsync(IBlazorApplicationBlock block, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(block, nameof(block));
		var definition = GetDefaultScreen(block);
		return definition == null ? base.ActivateBlockAsync(block, cancellationToken) : OpenScreenAsync(block, definition, null, null, cancellationToken);
	}

	public override Task<BlazorApplicationScreenSession> ActivateScreenAsync(IBlazorApplicationBlock block, Type screenType, string title = null,
		CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(block, nameof(block));
		var registration = Tools.UI.ResolveScreenRegistration(block.ScreenDefinitions, ApplicationScreenRegistration.ForType(screenType));
		return OpenScreenAsync(block, title == null ? registration : registration.WithTitle(title), null, null, cancellationToken, registration);
	}

	public override Task<BlazorApplicationScreenSession> ActivateScreenAsync(IBlazorApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(item, nameof(item));
		return OpenScreenAsync(block, item.ScreenDefinition, item as BlazorScreenMenuItem, null, cancellationToken);
	}

	private Task<BlazorApplicationScreenSession> OpenScreenAsync(IBlazorApplicationBlock block, ApplicationScreenDefinition definition, BlazorScreenMenuItem menuItem,
		BlazorApplicationScreenActivation activation, CancellationToken cancellationToken, ApplicationScreenDefinition registration = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return RunTransitionAsync(async token => {
			Guard.Argument(block != null && Blocks.Any(candidate => ReferenceEquals(candidate, block)), nameof(block), "The block is not registered.");
			Guard.Argument(block.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, registration ?? definition)), nameof(definition), "The screen definition is not owned by this block.");
			var previous = _activation;
			var previousMenuItem = _menuItem;
			using var request = Tools.Scope.ExecuteOnDispose(() => {
				_activation = previous;
				_menuItem = previousMenuItem;
			});
			_activation = activation;
			_menuItem = menuItem;
			return await ActivateCoreAsync(block, definition, token, registration);
		}, cancellationToken);
	}

	public virtual Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) =>
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

	public virtual Task AttachScreenAsync(Guid id, IBlazorApplicationScreen component, CancellationToken cancellationToken = default) {
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

	public virtual void DetachScreen(Guid id, IBlazorApplicationScreen component) {
		var screen = Screens.FirstOrDefault(candidate => candidate.Id == id);
		if (screen != null && ReferenceEquals(screen.Screen, component)) {
			screen.SetScreen(null);
			NotifyScreenChanged(screen);
		}
	}

	public virtual void NotifyScreenChanged(Guid id) => NotifyScreenChanged(GetScreen(id));

	public virtual BlazorApplicationStartupScreen StartupScreen { get; private set; }

	public virtual bool CanMakeStartupScreen(BlazorApplicationScreenSession screen) => GetStartupScreenUnavailableReason(screen) == null;

	public virtual string GetStartupScreenUnavailableReason(BlazorApplicationScreenSession screen) {
		if (screen == null || !Screens.Any(candidate => ReferenceEquals(candidate, screen)) || !ReferenceEquals(screen.Host, this))
			return "This screen is no longer registered with the application.";
		if (!screen.CanRestoreAtStartup)
			return "This screen requires session-specific state and cannot be restored at startup.";
		if (screen.Definition.StableKey == null && screen.Block.ScreenDefinitions.Count(definition => definition.ScreenType == screen.ScreenType) > 1)
			return "Configure WithStableKey on this screen registration before choosing it as the startup screen.";
		var identity = CreateStartupScreenIdentity(screen);
		return ResolveStartupScreen(identity) == null ? "This screen is excluded by the application's startup policy." : null;
	}

	public virtual bool IsStartupScreen(BlazorApplicationScreenSession screen) =>
		CanMakeStartupScreen(screen) && StartupScreen == CreateStartupScreenIdentity(screen);

	public virtual BlazorApplicationStartupScreen GetStartupScreenIdentity(BlazorApplicationScreenSession screen) {
		Guard.Ensure(CanMakeStartupScreen(screen), GetStartupScreenUnavailableReason(screen));
		return CreateStartupScreenIdentity(screen);
	}

	/// <summary>Updates circuit-local selection after a caller loads or successfully saves browser preferences.</summary>
	public virtual bool SetStartupScreen(BlazorApplicationStartupScreen identity) {
		EnsureUsable();
		StartupScreen = ResolveStartupScreen(identity) != null ? identity : null;
		OnChanged();
		return StartupScreen != null;
	}

	/// <summary>Override to exclude registrations requiring application-specific state before a component is constructed.</summary>
	protected virtual bool CanRestoreScreenAtStartup(IBlazorApplicationBlock block, ApplicationScreenDefinition definition) => definition.ScreenKind == ScreenKind.Normal;

	private static BlazorApplicationStartupScreen CreateStartupScreenIdentity(BlazorApplicationScreenSession screen) {
		return new BlazorApplicationStartupScreen(screen.Block.Source?.Name ?? string.Empty, screen.Block.Id, null, screen.ScreenType.FullName,
			screen.ScreenType.Assembly.GetName().Name, Tools.UI.GetScreenRegistrationKey(screen.Block, screen.Definition));
	}

	private ApplicationScreenDefinition ResolveStartupScreen(BlazorApplicationStartupScreen identity) {
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
				|| identity.MenuItemId.StartsWith(BlazorApplicationBlockSnapshot.DefinitionScreenItemPrefix, StringComparison.Ordinal)
				&& int.TryParse(identity.MenuItemId[BlazorApplicationBlockSnapshot.DefinitionScreenItemPrefix.Length..], out _)
				|| ReferenceEquals(BlazorApplicationBlockSnapshot.ResolveScreenDefinition(block, identity.MenuItemId), matches[0]))
				&& CanRestoreScreenAtStartup(block, matches[0]) ? matches[0] : null;
		var selected = matches.SingleOrDefault(definition => definition.StableKey == identity.RegistrationKey
			|| definition.StableKey == null && matches.Length == 1 && Tools.UI.GetScreenRegistrationKey(block, definition) == identity.RegistrationKey);
		return selected != null && CanRestoreScreenAtStartup(block, selected) ? selected : null;
	}

	protected BlazorApplicationScreenActivation CurrentActivation => _activation;

	protected override BlazorApplicationScreenSession FindReusableScreen(IBlazorApplicationBlock block, ApplicationScreenDefinition definition, ApplicationScreenDefinition registration) {
		var reusable = base.FindReusableScreen(block, definition, registration);
		if (reusable != null || definition.ActivationMode != ScreenActivationMode.MultiInstance || _activation?.InstanceKey == null)
			return reusable;
		// Instance identity comes from the retained sessions; failed preparation cannot leave a second retention index behind.
		return Screens.FirstOrDefault(screen => ReferenceEquals(screen.Block, block)
			&& screen.Definition.Registration.Token == registration.Registration.Token
			&& string.Equals(screen.InstanceKey, _activation.InstanceKey, StringComparison.Ordinal));
	}

	protected override void OnScreenRemoved(BlazorApplicationScreenSession screen) {
		screen.SetScreen(null);
		base.OnScreenRemoved(screen);
	}

	protected override ApplicationScreenDefinition[] GetDefinitions() {
		return GetDefinitions(BlazorApplicationBlockSnapshot.OrderBlocks(Services.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Blocks));
	}

	protected override ApplicationScreenDefinition GetStartupScreen() {
		var startup = ResolveStartupScreen(StartupScreen);
		if (startup != null)
			return startup;
		var blocks = BlazorApplicationBlockSnapshot.OrderBlocks(Services.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Blocks);
		return Tools.UI.GetDefaultScreen(blocks) ?? (blocks.FirstOrDefault() is { } first ? GetDefaultScreen(first) : null);
	}

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));

	protected override void NotifyScreenActivated(BlazorApplicationScreenSession screen) {
		base.NotifyScreenActivated(screen);
		_menuItem?.NotifySelect();
	}

	protected BlazorScreenMenuItem GetScreenMenuItem(IBlazorApplicationBlock block, ApplicationScreenDefinition definition) {
		if (_menuItem != null)
			return _menuItem;
		return definition.Activators.OfType<BlazorScreenMenuItem>().FirstOrDefault(item => ReferenceEquals(item.Parent?.Parent, block));
	}

	private IBlazorApplicationBlock GetRegisteredBlock(string blockId) => GetBlock(Blocks, blockId);

	private IBlazorApplicationBlock GetAvailableBlock(string blockId) => GetBlock(AvailableBlocks, blockId);

	private static IBlazorApplicationBlock GetBlock(IEnumerable<IBlazorApplicationBlock> blocks, string blockId) {
		Guard.ArgumentNotNullOrEmpty(blockId, nameof(blockId));
		var block = blocks.FirstOrDefault(candidate => candidate.Id == blockId);
		Guard.Argument(block != null, nameof(blockId), $"Unknown block '{blockId}'.");
		return block;
	}

	protected BlazorApplicationScreenSession GetScreen(Guid id) {
		var screen = Screens.FirstOrDefault(candidate => candidate.Id == id);
		Guard.Argument(screen != null, nameof(id), "The screen is no longer retained by this host.");
		return screen;
	}

	protected override void FreeManagedResources() {
		_ownedUserInterface?.Dispose();
		foreach (var screen in Screens)
			screen.SetScreen(null);
		base.FreeManagedResources();
	}
}
