// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Owns circuit-local screen sessions. Components remain owned and disposed by the Razor renderer.</summary>
public class BlazorApplicationScreenHost : BlazorApplicationScreenHostBase {
	private readonly IServiceProvider _services;
	private readonly Dictionary<string, IBlazorApplicationBlock> _blocks;
	private readonly Dictionary<Guid, BlazorApplicationScreenSession> _screens = new();
	private readonly Dictionary<Type, Guid> _singleInstances = new();
	private readonly List<Guid> _tabOrder = new();
	private ScreenMode _screenMode = ScreenMode.MultiView;
	private readonly SemaphoreSlim _transitions = new(1, 1);
	private readonly CancellationTokenSource _lifetime = new();
	private readonly CancellationToken _lifetimeToken;
	private BlazorApplicationScreenSession _activeScreen;
	private IBlazorApplicationBlock _activeBlock;
	private bool _disposed;
	private bool _initialized;

	public BlazorApplicationScreenHost(IBlazorApplicationBlockCatalog catalog, IServiceProvider services) {
		Guard.ArgumentNotNull(catalog, nameof(catalog));
		Guard.ArgumentNotNull(services, nameof(services));
		Catalog = catalog;
		_services = services;
		_blocks = catalog.Blocks.ToDictionary(block => block.Id, StringComparer.Ordinal);
		_lifetimeToken = _lifetime.Token;
	}

	public override IBlazorApplicationBlockCatalog Catalog { get; }

	public override IBlazorApplicationBlock[] Blocks => _blocks.Values.OrderBy(block => block.Position).ToArray();

	public override IBlazorApplicationBlock ActiveBlock => _activeBlock;

	public override BlazorApplicationScreenSession ActiveScreen => _activeScreen;

	public override BlazorApplicationScreenSession[] Screens => _screens.Values.ToArray();

	public override BlazorApplicationScreenSession[] OpenScreens => _tabOrder.Select(id => _screens[id]).ToArray();

	public override ScreenMode ScreenMode => _screenMode;

	public override async Task InitializeAsync(CancellationToken cancellationToken = default) {
		await RunTransitionAsync(async token => {
			if (_initialized)
				return true;
			var definitions = GetDefinitions();
			var startup = Tools.UI.GetDefaultScreen(definitions);
			var hasPermanent = definitions.Any(definition => definition.ActivationMode == ScreenActivationMode.PermanentSingleton);
			if (hasPermanent && _activeScreen?.ScreenKind == ScreenKind.Empty) {
				if (!await CanDeactivateAsync(_activeScreen, token))
					return false;
				await NotifyDeactivatedAsync(_activeScreen, token);
			}
			token.ThrowIfCancellationRequested();
			OpenPermanentScreens(definitions);
			if (hasPermanent && _activeScreen?.ScreenKind == ScreenKind.Empty) {
				HideInSingleView(_activeScreen);
				_activeScreen = null;
			}
			_initialized = true;
			if (_activeScreen == null) {
				if (startup != null)
					await ActivateCoreAsync((IBlazorApplicationBlock)startup.Block, GetScreenItem(startup), CancellationToken.None);
				else if (Blocks.FirstOrDefault() is { } firstBlock) {
					_activeBlock = firstBlock;
					var first = BlazorApplicationBlockSnapshot.GetDefaultScreen(firstBlock);
					if (first != null)
						await ActivateCoreAsync(firstBlock, first, CancellationToken.None);
				}
				if (_activeScreen == null)
					await ShowFallbackAsync();
			}
			OnChanged();
			return true;
		}, cancellationToken);
	}

	public override Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			Guard.Argument(mode is ScreenMode.SingleView or ScreenMode.MultiView, nameof(mode), "Unknown screen mode.");
			if (_screenMode == mode)
				return true;
			if (mode == ScreenMode.SingleView && !await CloseCoreAsync(OpenScreens.Where(session => session != _activeScreen && !session.IsPermanent).ToArray(), null, token))
				return false;
			// Batch closure is the commit point; cancellation cannot leave the mode half changed.
			_screenMode = mode;
			OnChanged();
			return true;
		}, cancellationToken);

	public override async Task MoveScreenAsync(Guid sessionId, int index, CancellationToken cancellationToken = default) {
		await RunTransitionAsync(token => {
			Guard.Argument(_tabOrder.Contains(sessionId), nameof(sessionId), "The screen has no open tab.");
			Guard.ArgumentInRange(index, 0, _tabOrder.Count - 1, nameof(index));
			token.ThrowIfCancellationRequested();
			if (_tabOrder.IndexOf(sessionId) != index) {
				_tabOrder.Remove(sessionId);
				_tabOrder.Insert(index, sessionId);
				OnChanged();
			}
			return Task.FromResult(true);
		}, cancellationToken);
	}

	public override Task<bool> CloseScreensAsync(IEnumerable<Guid> sessionIds, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(sessionIds, nameof(sessionIds));
		var ids = sessionIds.Distinct().ToArray();
		return RunTransitionAsync(token => CloseCoreAsync(ids.Select(GetSession).ToArray(), null, token), cancellationToken);
	}

	public override async Task<bool> ExecuteMenuItemAsync(IBlazorApplicationMenuItem item, CancellationToken cancellationToken = default) {
		EnsureUsable();
		Guard.ArgumentNotNull(item, nameof(item));
		if (item is BlazorScreenMenuItem screen) {
			var candidates = Blocks.Where(candidate => candidate.Menus.SelectMany(menu => menu.Items).OfType<BlazorScreenMenuItem>()
				.Any(command => command.Id == screen.Id && command.ScreenType == screen.ScreenType && command.ActivationMode == screen.ActivationMode)).ToArray();
			var block = candidates.FirstOrDefault(candidate => candidate.Menus.SelectMany(menu => menu.Items).Concat(candidate.ToolBarItems)
				.Any(command => ReferenceEquals(command, item)))
				?? candidates.FirstOrDefault(candidate => ReferenceEquals(candidate, ActiveScreen?.Block ?? ActiveBlock))
				?? (candidates.Length == 1 ? candidates[0] : null);
			Guard.Argument(block != null, nameof(item), "Screen commands must identify a registered block menu item unambiguously.");
			// The registered definition owns activation policy and parameters; caller-supplied aliases cannot replace them.
			return await ActivateScreenAsync(block.Id, screen.Id, cancellationToken) != null;
		}
		Guard.Argument(item is BlazorActionMenuItem, nameof(item), "The menu item is not executable.");
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeToken);
		await ((BlazorActionMenuItem)item).ExecuteAsync(_services, cancellation.Token);
		if (!_disposed)
			OnChanged();
		return true;
	}

	public override bool HasUnsavedChanges => _screens.Values.Any(session => session.Screen?.HasUnsavedChanges ?? false);

	public override async Task SelectBlockAsync(string blockId, CancellationToken cancellationToken = default) {
		await RunTransitionAsync(token => {
			var block = GetRegisteredBlock(blockId);
			token.ThrowIfCancellationRequested();
			if (!ReferenceEquals(_activeBlock, block)) {
				_activeBlock = block;
				OnChanged();
			}
			return Task.FromResult(true);
		}, cancellationToken);
	}

	public override Task<BlazorApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			var block = GetRegisteredBlock(blockId);
			var item = BlazorApplicationBlockSnapshot.GetDefaultScreen(block);
			if (item != null)
				return await ActivateCoreAsync(block, item, token);
			if (!await CanDeactivateAsync(_activeScreen, token))
				return null;
			await NotifyDeactivatedAsync(_activeScreen, token);
			token.ThrowIfCancellationRequested();
			HideInSingleView(_activeScreen);
			_activeScreen = null;
			if (_tabOrder.Count == 0)
				await ShowFallbackAsync();
			_activeBlock = block;
			OnChanged();
			return null;
		}, cancellationToken);

	public override Task<BlazorApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			var block = GetRegisteredBlock(blockId);
			var item = GetMenuItem(block, screenMenuItemId);
			Guard.Argument(item is BlazorScreenMenuItem, nameof(screenMenuItemId), "The menu item does not open a screen.");
			return await ActivateCoreAsync(block, (BlazorScreenMenuItem)item, token);
		}, cancellationToken);

	public override Task<bool> ShowScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => ShowCoreAsync(GetSession(sessionId), token), cancellationToken);

	public override Task<bool> CloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => CloseCoreAsync(new[] { GetSession(sessionId) }, null, token), cancellationToken);

	public override Task<bool> CanCloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => GetSession(sessionId).IsPermanent ? Task.FromResult(false) : CanDeactivateAsync(GetSession(sessionId), token), cancellationToken);

	public override Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => CanDeactivateAllAsync(_screens.Values.ToArray(), token), cancellationToken);

	public override async Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default) {
		EnsureUsable();
		var block = GetRegisteredBlock(blockId);
		var item = GetMenuItem(block, menuItemId);
		if (item is BlazorScreenMenuItem)
			return await ActivateScreenAsync(blockId, menuItemId, cancellationToken) != null;
		Guard.Ensure(item is BlazorActionMenuItem, "The menu item is not executable.");
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeToken);
		// Actions may navigate; execute outside the transition gate using this circuit's provider.
		await ((BlazorActionMenuItem)item).ExecuteAsync(_services, cancellation.Token);
		if (!_disposed)
			OnChanged();
		return true;
	}

	public override Task<bool> UnregisterBlockAsync(string blockId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			GetRegisteredBlock(blockId);
			var closing = _screens.Values.Where(session => session.Block.Id == blockId).ToArray();
			return await CloseCoreAsync(closing, () => {
				_blocks.Remove(blockId);
				if (_activeBlock?.Id == blockId)
					_activeBlock = _activeScreen?.Block ?? _blocks.Values.FirstOrDefault();
			}, token, true);
		}, cancellationToken);

	public override async Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default) {
		await RunTransitionAsync(async token => {
			var block = Catalog.Get(blockId);
			Guard.Argument(!_blocks.ContainsKey(blockId), nameof(blockId), "The block is already registered.");
			var definitions = Tools.UI.GetScreenDefinitions(new[] { block });
			var replacingEmpty = _activeScreen?.ScreenKind == ScreenKind.Empty && definitions.Any(item => item.ActivationMode == ScreenActivationMode.PermanentSingleton);
			if (replacingEmpty) {
				if (!await CanDeactivateAsync(_activeScreen, token))
					return false;
				await NotifyDeactivatedAsync(_activeScreen, token);
			}
			token.ThrowIfCancellationRequested();
			_blocks.Add(blockId, block);
			OpenPermanentScreens(definitions);
			if (replacingEmpty) {
				HideInSingleView(_activeScreen);
				_activeScreen = null;
			}
			if (_activeScreen == null)
				await ShowFallbackAsync();
			OnChanged();
			return true;
		}, cancellationToken);
	}

	public override async Task AttachScreenAsync(Guid sessionId, IBlazorApplicationScreen screen, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		await RunTransitionAsync(async token => {
			var session = GetSession(sessionId);
			Guard.Argument(session.ScreenType.IsInstanceOfType(screen), nameof(screen), "The component does not match the screen session.");
			Guard.Ensure(session.Screen == null || ReferenceEquals(session.Screen, screen), "The session already has an attached component.");
			if (ReferenceEquals(session.Screen, screen))
				return true;
			session.Screen = screen;
			var attached = false;
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
				if (!attached && ReferenceEquals(session.Screen, screen))
					session.Screen = null;
			});
			if (ReferenceEquals(session, _activeScreen))
				await screen.OnActivatedAsync(token);
			token.ThrowIfCancellationRequested();
			attached = true;
			OnChanged();
			return true;
		}, cancellationToken);
	}

	public override void DetachScreen(Guid sessionId, IBlazorApplicationScreen screen) {
		if (_screens.TryGetValue(sessionId, out var session) && ReferenceEquals(session.Screen, screen)) {
			session.Screen = null;
			if (!_disposed)
				OnChanged();
		}
	}

	public override void NotifyScreenChanged(Guid sessionId) {
		EnsureUsable();
		Guard.Ensure(GetSession(sessionId).Screen != null, "The screen has not attached to its session.");
		OnChanged();
	}

	protected override void FreeManagedResources() {
		if (_disposed)
			return;
		_disposed = true;
		_lifetime.Cancel();
		_activeScreen = null;
		_activeBlock = null;
		_screens.Clear();
		_tabOrder.Clear();
		_singleInstances.Clear();
		_blocks.Clear();
		_lifetime.Dispose();
		// Pending transitions may still release the semaphore after their cancellation is observed.
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}

	private async Task<BlazorApplicationScreenSession> ActivateCoreAsync(IBlazorApplicationBlock block, BlazorScreenMenuItem item, CancellationToken token) {
		item = ResolveScreenItem(block, item);
		if (item.ScreenKind == ScreenKind.Empty && _tabOrder.Count > 0)
			return null;
		if (Tools.UI.IsSingleton(item.ActivationMode) && _singleInstances.TryGetValue(item.ScreenType, out var existingId)) {
			var existing = GetSession(existingId);
			if (!await ShowCoreAsync(existing, token))
				return null;
			item.NotifySelect();
			return existing;
		}
		if (!await CanDeactivateAsync(_activeScreen, token))
			return null;
		await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();

		HideInSingleView(_activeScreen);
		var session = CreateSession(block, item);
		_activeScreen = session;
		_activeBlock = block;
		OnChanged();
		item.NotifySelect();
		return session;
	}

	private async Task<bool> ShowCoreAsync(BlazorApplicationScreenSession session, CancellationToken token) {
		if (session.ScreenKind == ScreenKind.Empty && _tabOrder.Count > 0)
			return false;
		if (ReferenceEquals(session, _activeScreen)) {
			token.ThrowIfCancellationRequested();
			if (!ReferenceEquals(_activeBlock, session.Block)) {
				_activeBlock = session.Block;
				OnChanged();
			}
			return true;
		}
		if (!await CanDeactivateAsync(_activeScreen, token))
			return false;
		await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		HideInSingleView(_activeScreen);
		if (session.ScreenKind == ScreenKind.Normal && !_tabOrder.Contains(session.Id))
			_tabOrder.Add(session.Id);
		_activeScreen = session;
		_activeBlock = session.Block;
		OnChanged();
		// Cancellation is honored before committing the selection, not after the active screen changes.
		if (session.Screen != null)
			await session.Screen.OnActivatedAsync(CancellationToken.None);
		return true;
	}

	private async Task<bool> CloseCoreAsync(BlazorApplicationScreenSession[] closing, Action afterClose, CancellationToken token, bool unregistering = false) {
		if (!unregistering && closing.Any(session => session.IsPermanent))
			return false;
		if (!await CanDeactivateAllAsync(closing, token))
			return false;
		var activeClosing = closing.Contains(_activeScreen);
		var activeIndex = _activeScreen == null ? -1 : _tabOrder.IndexOf(_activeScreen.Id);
		if (activeClosing)
			await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		foreach (var session in closing) {
			_screens.Remove(session.Id);
			_tabOrder.Remove(session.Id);
			if (_singleInstances.TryGetValue(session.ScreenType, out var singleId) && singleId == session.Id)
				_singleInstances.Remove(session.ScreenType);
			// Removing its descriptor causes the renderer to dispose the keyed component.
		}
		if (activeClosing) {
			_activeScreen = _tabOrder.Count == 0 ? null : _screens[_tabOrder[Math.Min(Math.Max(activeIndex, 0), _tabOrder.Count - 1)]];
			if (_activeScreen != null)
				_activeBlock = _activeScreen.Block;
		}
		afterClose?.Invoke();
		if (unregistering) {
			var removedPermanentTypes = closing.Where(session => session.IsPermanent).Select(session => session.ScreenType).ToHashSet();
			OpenPermanentScreens(GetDefinitions().Where(definition => removedPermanentTypes.Contains(definition.ScreenType)));
		}
		if (_activeScreen == null)
			await ShowFallbackAsync();
		else if (activeClosing && _activeScreen.Screen != null)
			await _activeScreen.Screen.OnActivatedAsync(CancellationToken.None);
		OnChanged();
		return true;
	}

	private void HideInSingleView(BlazorApplicationScreenSession session) {
		if (session == null || session.IsPermanent || session.ScreenKind == ScreenKind.Normal && _screenMode != ScreenMode.SingleView)
			return;
		_tabOrder.Remove(session.Id);
		if (session.MenuItem.ActivationMode == ScreenActivationMode.MultiInstance)
			_screens.Remove(session.Id);
	}

	private ApplicationScreenDefinition[] GetDefinitions() => Tools.UI.GetScreenDefinitions(
		Tools.UI.OrderApplicationBlocks(_services.GetServices<IBlazorPlugin>(), _blocks.Values));

	private static BlazorScreenMenuItem GetScreenItem(ApplicationScreenDefinition definition) {
		var block = (IBlazorApplicationBlock)definition.Block;
		var defaultScreen = block.DefaultScreen == null ? null : BlazorApplicationBlockSnapshot.GetDefaultScreen(block);
		if (defaultScreen?.Id == definition.MenuItemId)
			return defaultScreen;
		return (BlazorScreenMenuItem)GetMenuItem(block, definition.MenuItemId);
	}

	private BlazorScreenMenuItem ResolveScreenItem(IBlazorApplicationBlock block, BlazorScreenMenuItem item) {
		var definitions = GetDefinitions();
		var definition = definitions.First(screen => screen.Block.Id == block.Id && screen.MenuItemId == item.Id);
		var mode = definition.ActivationMode ?? item.ActivationMode;
		if (item.ActivationMode == mode && item.ScreenKind == definition.ScreenKind && item.IsDefault == definition.IsDefault)
			return item;
		var resolved = new BlazorScreenMenuItem {
			Id = item.Id, Title = item.Title, Icon = item.Icon, ScreenType = item.ScreenType, ActivationMode = mode,
			ScreenKind = definition.ScreenKind, IsDefault = definition.IsDefault, Parameters = item.Parameters
		};
		resolved.CopySubscriptionsFrom(item);
		return resolved;
	}

	private BlazorApplicationScreenSession CreateSession(IBlazorApplicationBlock block, BlazorScreenMenuItem item) {
		var session = new BlazorApplicationScreenSession(block, item);
		_screens.Add(session.Id, session);
		if (item.ScreenKind == ScreenKind.Normal)
			_tabOrder.Add(session.Id);
		if (Tools.UI.IsSingleton(item.ActivationMode))
			_singleInstances.Add(item.ScreenType, session.Id);
		return session;
	}

	private void OpenPermanentScreens(IEnumerable<ApplicationScreenDefinition> definitions) {
		foreach (var definition in definitions.Where(item => item.ActivationMode == ScreenActivationMode.PermanentSingleton))
			if (!_singleInstances.ContainsKey(definition.ScreenType))
				CreateSession((IBlazorApplicationBlock)definition.Block, ResolveScreenItem((IBlazorApplicationBlock)definition.Block, GetScreenItem(definition)));
	}

	private async Task ShowFallbackAsync() {
		if (_tabOrder.Count > 0) {
			_activeScreen = _screens[_tabOrder[0]];
			_activeBlock = _activeScreen.Block;
			if (_activeScreen.Screen != null)
				await _activeScreen.Screen.OnActivatedAsync(CancellationToken.None);
			return;
		}
		var empty = Tools.UI.GetEmptyScreen(GetDefinitions());
		if (empty != null)
			await ActivateCoreAsync((IBlazorApplicationBlock)empty.Block, GetScreenItem(empty), CancellationToken.None);
	}

	private static async Task<bool> CanDeactivateAsync(BlazorApplicationScreenSession session, CancellationToken token) {
		token.ThrowIfCancellationRequested();
		var allowed = session?.Screen == null || await session.Screen.CanDeactivateAsync(token);
		token.ThrowIfCancellationRequested();
		return allowed;
	}

	private static async Task<bool> CanDeactivateAllAsync(IEnumerable<BlazorApplicationScreenSession> sessions, CancellationToken token) {
		foreach (var session in sessions)
			if (!await CanDeactivateAsync(session, token))
				return false;
		return true;
	}

	private static Task NotifyDeactivatedAsync(BlazorApplicationScreenSession session, CancellationToken token) =>
		session?.Screen?.OnDeactivatedAsync(token) ?? Task.CompletedTask;

	private IBlazorApplicationBlock GetRegisteredBlock(string blockId) {
		Guard.ArgumentNotNullOrEmpty(blockId, nameof(blockId));
		Guard.Argument(_blocks.TryGetValue(blockId, out var block), nameof(blockId), $"Block '{blockId}' is not registered.");
		return block;
	}

	private static IBlazorApplicationMenuItem GetMenuItem(IBlazorApplicationBlock block, string menuItemId) {
		Guard.ArgumentNotNullOrEmpty(menuItemId, nameof(menuItemId));
		var item = block.Menus.SelectMany(menu => menu.Items).FirstOrDefault(item => item.Id == menuItemId);
		var defaultScreen = block.DefaultScreen == null ? null : BlazorApplicationBlockSnapshot.GetDefaultScreen(block);
		if (defaultScreen?.Id == menuItemId)
			item = defaultScreen;
		Guard.Argument(item != null, nameof(menuItemId), $"Unknown menu item '{menuItemId}' in block '{block.Id}'.");
		return item;
	}

	private BlazorApplicationScreenSession GetSession(Guid sessionId) {
		Guard.Argument(_screens.TryGetValue(sessionId, out var session), nameof(sessionId), "The screen session is not open.");
		return session;
	}

	private async Task<TResult> RunTransitionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken) {
		EnsureUsable();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeToken);
		await _transitions.WaitAsync(cancellation.Token);
		using var transition = Tools.Scope.ExecuteOnDispose(() => _transitions.Release());
		EnsureUsable();
		return await action(cancellation.Token);
	}

	private void EnsureUsable() => Guard.Ensure(!_disposed, "The screen host has been disposed.");
}

