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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

/// <summary>Owns circuit-local screen sessions. Components remain owned and disposed by the Razor renderer.</summary>
public class ApplicationScreenHost : ApplicationScreenHostBase {
	private readonly IServiceProvider _services;
	private readonly Dictionary<string, IApplicationBlock> _blocks;
	private readonly Dictionary<Guid, ApplicationScreenSession> _screens = new();
	private readonly Dictionary<Type, Guid> _singleInstances = new();
	private readonly SemaphoreSlim _transitions = new(1, 1);
	private readonly CancellationTokenSource _lifetime = new();
	private readonly CancellationToken _lifetimeToken;
	private ApplicationScreenSession _activeScreen;
	private IApplicationBlock _activeBlock;
	private bool _disposed;

	public ApplicationScreenHost(IApplicationBlockCatalog catalog, IServiceProvider services) {
		Guard.ArgumentNotNull(catalog, nameof(catalog));
		Guard.ArgumentNotNull(services, nameof(services));
		Catalog = catalog;
		_services = services;
		_blocks = catalog.Blocks.ToDictionary(block => block.Id, StringComparer.Ordinal);
		_lifetimeToken = _lifetime.Token;
	}

	public override IApplicationBlockCatalog Catalog { get; }

	public override IReadOnlyList<IApplicationBlock> Blocks => Array.AsReadOnly(_blocks.Values.OrderBy(block => block.Position).ToArray());

	public override IApplicationBlock ActiveBlock => _activeBlock;

	public override ApplicationScreenSession ActiveScreen => _activeScreen;

	public override IReadOnlyList<ApplicationScreenSession> OpenScreens => Array.AsReadOnly(_screens.Values.ToArray());

	public override bool HasUnsavedChanges => _screens.Values.Any(session => session.Screen?.HasUnsavedChanges ?? false);

	public override Task<ApplicationScreenSession> ActivateBlockAsync(string blockId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			var block = GetRegisteredBlock(blockId);
			var item = ApplicationBlockSnapshot.GetDefaultScreen(block);
			if (item != null)
				return await ActivateCoreAsync(block, item, token);
			if (!await CanDeactivateAsync(_activeScreen, token))
				return null;
			await NotifyDeactivatedAsync(_activeScreen, token);
			token.ThrowIfCancellationRequested();
			_activeScreen = null;
			_activeBlock = block;
			OnChanged();
			return null;
		}, cancellationToken);

	public override Task<ApplicationScreenSession> ActivateScreenAsync(string blockId, string screenMenuItemId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			var block = GetRegisteredBlock(blockId);
			var item = GetMenuItem(block, screenMenuItemId);
			Guard.Argument(item is ShowScreenMenuItem, nameof(screenMenuItemId), "The menu item does not open a screen.");
			return await ActivateCoreAsync(block, (ShowScreenMenuItem)item, token);
		}, cancellationToken);

	public override Task<bool> ShowScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => ShowCoreAsync(GetSession(sessionId), token), cancellationToken);

	public override Task<bool> CloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => CloseCoreAsync(new[] { GetSession(sessionId) }, null, token), cancellationToken);

	public override Task<bool> CanCloseScreenAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => CanDeactivateAsync(GetSession(sessionId), token), cancellationToken);

	public override Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => CanDeactivateAllAsync(_screens.Values.ToArray(), token), cancellationToken);

	public override async Task<bool> ExecuteMenuItemAsync(string blockId, string menuItemId, CancellationToken cancellationToken = default) {
		EnsureUsable();
		var block = GetRegisteredBlock(blockId);
		var item = GetMenuItem(block, menuItemId);
		if (item is ShowScreenMenuItem)
			return await ActivateScreenAsync(blockId, menuItemId, cancellationToken) != null;
		Guard.Ensure(item is ActionMenuItem, "The menu item is not executable.");
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeToken);
		// Actions may navigate; execute outside the transition gate using this circuit's provider.
		await ((ActionMenuItem)item).ExecuteAsync(_services, cancellation.Token);
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
			}, token);
		}, cancellationToken);

	public override async Task RegisterBlockAsync(string blockId, CancellationToken cancellationToken = default) {
		await RunTransitionAsync(token => {
			var block = Catalog.Get(blockId);
			Guard.Argument(!_blocks.ContainsKey(blockId), nameof(blockId), "The block is already registered.");
			token.ThrowIfCancellationRequested();
			_blocks.Add(blockId, block);
			OnChanged();
			return Task.FromResult(true);
		}, cancellationToken);
	}

	public override async Task AttachScreenAsync(Guid sessionId, IApplicationScreen screen, CancellationToken cancellationToken = default) {
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

	public override void DetachScreen(Guid sessionId, IApplicationScreen screen) {
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
		_singleInstances.Clear();
		_blocks.Clear();
		_lifetime.Dispose();
		// Pending transitions may still release the semaphore after their cancellation is observed.
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}

	private async Task<ApplicationScreenSession> ActivateCoreAsync(IApplicationBlock block, ShowScreenMenuItem item, CancellationToken token) {
		if (item.ActivationMode == ScreenActivationMode.SingleInstance && _singleInstances.TryGetValue(item.ScreenType, out var existingId)) {
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

		var session = new ApplicationScreenSession(block, item);
		_screens.Add(session.Id, session);
		if (item.ActivationMode == ScreenActivationMode.SingleInstance)
			_singleInstances.Add(item.ScreenType, session.Id);
		_activeScreen = session;
		_activeBlock = block;
		OnChanged();
		item.NotifySelect();
		return session;
	}

	private async Task<bool> ShowCoreAsync(ApplicationScreenSession session, CancellationToken token) {
		if (ReferenceEquals(session, _activeScreen))
			return true;
		if (!await CanDeactivateAsync(_activeScreen, token))
			return false;
		await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		_activeScreen = session;
		_activeBlock = session.Block;
		OnChanged();
		// Cancellation is honored before committing the selection, not after the active screen changes.
		if (session.Screen != null)
			await session.Screen.OnActivatedAsync(CancellationToken.None);
		return true;
	}

	private async Task<bool> CloseCoreAsync(ApplicationScreenSession[] closing, Action afterClose, CancellationToken token) {
		if (!await CanDeactivateAllAsync(closing, token))
			return false;
		var activeClosing = closing.Contains(_activeScreen);
		if (activeClosing)
			await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		foreach (var session in closing) {
			_screens.Remove(session.Id);
			if (_singleInstances.TryGetValue(session.ScreenType, out var singleId) && singleId == session.Id)
				_singleInstances.Remove(session.ScreenType);
			// Removing its descriptor causes the renderer to dispose the keyed component.
		}
		if (activeClosing) {
			_activeScreen = _screens.Values.LastOrDefault();
			_activeBlock = _activeScreen?.Block;
		}
		afterClose?.Invoke();
		OnChanged();
		if (activeClosing && _activeScreen?.Screen != null)
			await _activeScreen.Screen.OnActivatedAsync(CancellationToken.None);
		return true;
	}

	private static async Task<bool> CanDeactivateAsync(ApplicationScreenSession session, CancellationToken token) {
		token.ThrowIfCancellationRequested();
		var allowed = session?.Screen == null || await session.Screen.CanDeactivateAsync(token);
		token.ThrowIfCancellationRequested();
		return allowed;
	}

	private static async Task<bool> CanDeactivateAllAsync(IEnumerable<ApplicationScreenSession> sessions, CancellationToken token) {
		foreach (var session in sessions)
			if (!await CanDeactivateAsync(session, token))
				return false;
		return true;
	}

	private static Task NotifyDeactivatedAsync(ApplicationScreenSession session, CancellationToken token) =>
		session?.Screen?.OnDeactivatedAsync(token) ?? Task.CompletedTask;

	private IApplicationBlock GetRegisteredBlock(string blockId) {
		Guard.ArgumentNotNullOrEmpty(blockId, nameof(blockId));
		Guard.Argument(_blocks.TryGetValue(blockId, out var block), nameof(blockId), $"Block '{blockId}' is not registered.");
		return block;
	}

	private static IApplicationMenuItem GetMenuItem(IApplicationBlock block, string menuItemId) {
		Guard.ArgumentNotNullOrEmpty(menuItemId, nameof(menuItemId));
		var item = block.Menus.SelectMany(menu => menu.Items).FirstOrDefault(item => item.Id == menuItemId);
		if (item == null && menuItemId == ApplicationBlockSnapshot.DefaultScreenItemId)
			item = ApplicationBlockSnapshot.GetDefaultScreen(block);
		Guard.Argument(item != null, nameof(menuItemId), $"Unknown menu item '{menuItemId}' in block '{block.Id}'.");
		return item;
	}

	private ApplicationScreenSession GetSession(Guid sessionId) {
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

