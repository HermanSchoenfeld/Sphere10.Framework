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
	private readonly Dictionary<IBlazorApplicationBlock, Dictionary<(string MenuItemId, Type ScreenType, string InstanceKey), BlazorApplicationScreenSession>> _keyedScreens = new(ReferenceEqualityComparer.Instance);
	private BlazorApplicationScreenActivation _activation;
	private BlazorScreenMenuItem _menuItem;
	private BlazorUserInterfaceServices _ownedUserInterface;
	private BlazorScreenMenuItem _startupMenuItem;

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
		var item = BlazorApplicationBlockSnapshot.ResolveMenuItem(block, screenMenuItemId) as BlazorScreenMenuItem;
		Guard.Argument(item != null, nameof(screenMenuItemId), "The menu item does not open a registered screen.");
		return OpenScreenAsync(block, item.ScreenDefinition, item, activation, cancellationToken);
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
		var registration = block.ScreenDefinitions.FirstOrDefault(definition => definition.ScreenType == screenType);
		Guard.Argument(registration != null, nameof(screenType), "The screen type is not registered in this block.");
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
			_menuItem = menuItem ?? GetScreenMenuItem(block, registration ?? definition);
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

	private static BlazorApplicationStartupScreen CreateStartupScreenIdentity(BlazorApplicationScreenSession screen) => new(
		screen.Block.Source?.Name ?? string.Empty, screen.Block.Id, screen.MenuItemId, screen.ScreenType.FullName, screen.ScreenType.Assembly.GetName().Name);

	private BlazorScreenMenuItem ResolveStartupScreen(BlazorApplicationStartupScreen identity) {
		if (identity == null || string.IsNullOrEmpty(identity.BlockId) || string.IsNullOrEmpty(identity.MenuItemId)
			|| string.IsNullOrEmpty(identity.ScreenTypeName) || string.IsNullOrEmpty(identity.ScreenAssemblyName))
			return null;
		var block = Blocks.FirstOrDefault(candidate => candidate.Id == identity.BlockId && (candidate.Source?.Name ?? string.Empty) == (identity.PluginName ?? string.Empty));
		if (block == null)
			return null;
		BlazorScreenMenuItem item = null;
		Tools.Exceptions.ExecuteCapturingException<ArgumentException>(() => {
			item = BlazorApplicationBlockSnapshot.ResolveMenuItem(block, identity.MenuItemId) as BlazorScreenMenuItem;
		}, out var capturedError);
		if (capturedError != null) {
			return null;
		}
		return item != null && block.ScreenDefinitions.Any(definition => ReferenceEquals(definition, item.ScreenDefinition))
			&& item.ScreenType.FullName == identity.ScreenTypeName && item.ScreenType.Assembly.GetName().Name == identity.ScreenAssemblyName
			&& CanRestoreScreenAtStartup(block, item.ScreenDefinition) ? item : null;
	}

	protected BlazorApplicationScreenActivation CurrentActivation => _activation;

	protected override BlazorApplicationScreenSession FindReusableScreen(IBlazorApplicationBlock block, ApplicationScreenDefinition definition, ApplicationScreenDefinition registration) {
		var reusable = base.FindReusableScreen(block, definition);
		if (reusable != null || definition.ActivationMode != ScreenActivationMode.MultiInstance || _activation?.InstanceKey == null)
			return reusable;
		return _keyedScreens.TryGetValue(block, out var ownerScreens)
			&& ownerScreens.TryGetValue((GetScreenMenuItem(block, registration).Id, definition.ScreenType, _activation.InstanceKey), out var keyed) ? keyed : null;
	}

	protected void TrackKeyedScreen(BlazorApplicationScreenSession screen) {
		if (screen.InstanceKey == null)
			return;
		if (!_keyedScreens.TryGetValue(screen.Block, out var ownerScreens))
			_keyedScreens.Add(screen.Block, ownerScreens = new());
		ownerScreens[(screen.MenuItemId, screen.ScreenType, screen.InstanceKey)] = screen;
	}

	protected override void OnScreenRemoved(BlazorApplicationScreenSession screen) {
		if (screen.InstanceKey != null && _keyedScreens.TryGetValue(screen.Block, out var ownerScreens)) {
			ownerScreens.Remove((screen.MenuItemId, screen.ScreenType, screen.InstanceKey));
			if (ownerScreens.Count == 0)
				_keyedScreens.Remove(screen.Block);
		}
		screen.SetScreen(null);
		base.OnScreenRemoved(screen);
	}

	protected override ApplicationScreenDefinition[] GetDefinitions() {
		return GetDefinitions(BlazorApplicationBlockSnapshot.OrderBlocks(Services.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Blocks));
	}

	protected override ApplicationScreenDefinition GetDefaultScreen(IBlazorApplicationBlock block) =>
		BlazorApplicationBlockSnapshot.GetDefaultScreen(block)?.ScreenDefinition ?? base.GetDefaultScreen(block);

	protected override ApplicationScreenDefinition GetStartupScreen() {
		_startupMenuItem = ResolveStartupScreen(StartupScreen);
		if (_startupMenuItem != null)
			return _startupMenuItem.ScreenDefinition;
		var blocks = BlazorApplicationBlockSnapshot.OrderBlocks(Services.GetServices<ApplicationPluginRegistration<IBlazorPlugin>>(), Blocks);
		return Tools.UI.GetDefaultScreen(blocks) ?? (blocks.FirstOrDefault() is { } first ? GetDefaultScreen(first) : null);
	}

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));

	protected override void NotifyScreenActivated(BlazorApplicationScreenSession screen) {
		base.NotifyScreenActivated(screen);
		(_menuItem ?? screen.MenuItem).NotifySelect();
	}

	protected BlazorScreenMenuItem GetScreenMenuItem(IBlazorApplicationBlock block, ApplicationScreenDefinition definition) {
		if (_menuItem != null)
			return _menuItem;
		if (_startupMenuItem != null && ReferenceEquals(_startupMenuItem.ScreenDefinition, definition)) {
			var startup = _startupMenuItem;
			_startupMenuItem = null;
			return startup;
		}
		var defaultScreen = BlazorApplicationBlockSnapshot.GetDefaultScreen(block);
		if (ReferenceEquals(block.DefaultScreenDefinition, definition))
			return defaultScreen;
		return definition.Activators.OfType<BlazorScreenMenuItem>().FirstOrDefault(item => ReferenceEquals(item.Parent?.Parent, block))
			?? BlazorApplicationBlockSnapshot.GetDefinitionMenuItem(block, definition);
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
		_keyedScreens.Clear();
		base.FreeManagedResources();
	}
}
