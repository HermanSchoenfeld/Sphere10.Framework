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

namespace Sphere10.Framework.Application.UI;

/// <summary>Shared block registration, screen retention and lifecycle using object identity. Adapters own their views and presentation metadata.</summary>
public class ApplicationScreenHost<TBlock, TScreen> : ApplicationScreenHostBase<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	private readonly Dictionary<TBlock, TBlock> _availableBlocks = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<TBlock, TBlock> _blocks = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<ApplicationScreenDefinition, Dictionary<TBlock, TBlock>> _definitionBlocks = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<TBlock, ApplicationScreenDefinition[]> _blockDefinitions = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<TScreen, (ApplicationScreenDefinition Definition, Type ScreenType, ScreenActivationMode ActivationMode, ScreenKind ScreenKind, IApplicationMenu[] OwnedMenus)> _screens = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<Type, TScreen> _singleInstances = new();
	private readonly List<TScreen> _openOrder = new();
	private readonly Dictionary<Type, ApplicationScreenDefinition> _activationDefinitions = new();
	private readonly SemaphoreSlim _transitions = new(1, 1);
	private readonly CancellationTokenSource _lifetime = new();
	private readonly CancellationToken _lifetimeToken;
	private TBlock _activeBlock;
	private TScreen _activeScreen;
	private ScreenMode _screenMode = ScreenMode.MultiView;
	private bool _initialized;
	private bool _disposed;
	private int _transitionDepth;
	private bool _changedPending;
	private bool _selectionPending;
	private IApplicationScreen _pendingSelection;
	private ApplicationUserInterfaceServices _ownedUserInterface;

	public ApplicationScreenHost(IServiceProvider services)
		: this(Array.Empty<TBlock>(), services) {
	}

	public ApplicationScreenHost(IEnumerable<TBlock> blocks, IServiceProvider services, Func<TBlock, TBlock> snapshot = null) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		Guard.ArgumentNotNull(services, nameof(services));
		Services = services;
		_lifetimeToken = _lifetime.Token;
		var definitions = blocks.Select(block => snapshot == null ? block : snapshot(block)).ToArray();
		Guard.Argument(definitions.All(block => block != null), nameof(blocks), "Block definitions cannot be null.");
		Guard.Argument(definitions.Distinct(ReferenceEqualityComparer.Instance).Count() == definitions.Length, nameof(blocks), "A block cannot be registered twice.");
		ValidateDefinitions(Tools.UI.GetScreenDefinitions(definitions));
		foreach (var block in definitions) {
			AddAvailableBlock(block);
			RegisterTrackedBlock(block);
		}
	}

	public override TBlock[] AvailableBlocks => _availableBlocks.Values.OrderBy(block => block.Position).ToArray();

	public override TBlock[] Blocks => _blocks.Values.OrderBy(block => block.Position).ToArray();

	public override TBlock ActiveBlock => _activeBlock;

	public override TScreen ActiveScreen => _activeScreen;

	public override TScreen[] Screens => _screens.Keys.ToArray();

	public override TScreen[] OpenScreens => _openOrder.ToArray();

	public override IUserInterfaceServices UserInterfaceServices {
		get {
			EnsureUsable();
			return Services.GetService<IUserInterfaceServices>() ?? (_ownedUserInterface ??= new ApplicationUserInterfaceServices(this, Services));
		}
	}

	/// <summary>Changes mode synchronously when no screens must close. Use TrySetScreenModeAsync for guarded transitions.</summary>
	public override ScreenMode ScreenMode {
		get => _screenMode;
		set {
			EnsureUsable();
			Guard.Argument(value is ScreenMode.SingleView or ScreenMode.MultiView, nameof(value), "Unknown screen mode.");
			Guard.Ensure(_transitions.Wait(0), "A screen transition is in progress. Use TrySetScreenModeAsync to await it.");
			using var transition = Tools.Scope.ExecuteOnDispose(() => _transitions.Release());
			if (_screenMode == value)
				return;
			Guard.Ensure(value != ScreenMode.SingleView || !OpenScreens.Any(screen => !ReferenceEquals(screen, _activeScreen) && !IsPermanent(screen)),
				"Changing to single view requires closing screens. Await TrySetScreenModeAsync so their deactivation guards can run.");
			_screenMode = value;
			OnChanged();
		}
	}

	public override bool HasUnsavedChanges => _screens.Keys.Any(screen => screen.HasUnsavedChanges);

	protected IServiceProvider Services { get; }

	protected bool IsInitialized => _initialized;

	protected TBlock[] TrackedBlocks => _blocks.Values.ToArray();

	public override Task<bool> ShowScreenUndockedAsync(TScreen screen, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		EnsureUsable();
		Guard.ArgumentNotNull(screen, nameof(screen));
		throw new NotSupportedException("This screen host does not support detached presentation.");
	}

	public override Task InitializeAsync(CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			if (_initialized)
				return true;
			var definitions = GetDefinitions();
			ValidateDefinitions(definitions);
			var replacingEmpty = IsEmpty(_activeScreen) && definitions.Any(definition => ResolveDefinition(definition).ActivationMode == ScreenActivationMode.PermanentSingleton);
			if (replacingEmpty) {
				if (!await CanDeactivateAsync(_activeScreen, token))
					return false;
				await NotifyDeactivatedAsync(_activeScreen, token);
			}
			token.ThrowIfCancellationRequested();
			RegisterTrackedDefinitions(definitions);
			OpenPermanentScreens(GetDefinitions());
			if (replacingEmpty) {
				HideInSingleView(_activeScreen);
				_activeScreen = null;
			}
			_initialized = true;
			if (_activeScreen == null) {
				var startup = GetStartupScreen();
				if (startup != null)
					await ActivateCoreAsync(GetDefinitionBlock(startup), startup, CancellationToken.None);
				if (_activeScreen == null)
					await ShowFallbackAsync();
			}
			if (_activeBlock == null)
				SetTrackedBlockSelection(GetScreenBlock(_activeScreen) ?? Blocks.FirstOrDefault());
			OnChanged();
			return true;
		}, cancellationToken);

	public override Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			token.ThrowIfCancellationRequested();
			SetTrackedBlockSelection(block);
			return Task.FromResult(true);
		}, cancellationToken);

	public override Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			RequireRegisteredBlock(block);
			RegisterTrackedDefinitions(GetDefinitions());
			var definition = GetDefaultScreen(block);
			if (definition != null)
				return await ActivateCoreAsync(block, definition, token);
			if (await ClearSelectionCoreAsync(token))
				SetTrackedBlockSelection(block);
			return null;
		}, cancellationToken);

	public override Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			RegisterTrackedDefinitions(GetDefinitions());
			var definition = GetDefinitions(new[] { block }).FirstOrDefault(item => item.ScreenType == screenType);
			Guard.Argument(definition != null, nameof(screenType), "The screen type is not registered in this block.");
			var registration = definition;
			if (title != null)
				definition = WithTitle(definition, title);
			return ActivateCoreAsync(block, definition, token, registration);
		}, cancellationToken);

	public override Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			Guard.ArgumentNotNull(item, nameof(item));
			RegisterTrackedDefinitions(GetDefinitions());
			var definition = GetDefinitions(new[] { block }).FirstOrDefault(candidate => ReferenceEquals(candidate, item.ScreenDefinition));
			Guard.Argument(definition != null, nameof(item), "The menu item is not a registered screen in this block.");
			return ActivateCoreAsync(block, definition, token);
		}, cancellationToken);

	public override Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			Guard.Argument(definition != null && block.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, definition)),
				nameof(definition), "The screen definition is not owned by this block.");
			return ActivateCoreAsync(block, definition, token);
		}, cancellationToken);

	public override Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			Guard.Argument(block != null && _availableBlocks.ContainsKey(block), nameof(block), "The block is not available.");
			Guard.Argument(!_blocks.ContainsKey(block), nameof(block), "The block is already registered.");
			Guard.Argument(block.Parent == null || ReferenceEquals(block.Parent.ScreenHost, ScreenOwner), nameof(block), "The block belongs to another application's host.");
			var definitions = GetDefinitions(new[] { block });
			ValidateDefinitions(definitions);
			var replacingEmpty = _initialized && IsEmpty(_activeScreen)
				&& definitions.Any(definition => ResolveDefinition(definition).ActivationMode == ScreenActivationMode.PermanentSingleton);
			if (replacingEmpty) {
				if (!await CanDeactivateAsync(_activeScreen, token))
					return false;
				await NotifyDeactivatedAsync(_activeScreen, token);
			}
			token.ThrowIfCancellationRequested();
			RegisterTrackedDefinitions(definitions);
			RegisterTrackedBlock(block);
			if (_initialized) {
				OpenPermanentScreens(GetDefinitions());
				if (replacingEmpty) {
					HideInSingleView(_activeScreen);
					_activeScreen = null;
				}
				if (_activeScreen == null)
					await ShowFallbackAsync();
			}
			OnChanged();
			return true;
		}, cancellationToken);

	public override Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			return CloseCoreAsync(Screens.Where(screen => ReferenceEquals(GetScreenBlock(screen), block)).ToArray(), () => {
				UnregisterTrackedBlock(block);
			}, token, true);
		}, cancellationToken);

	public override TBlock GetScreenBlock(TScreen screen) =>
		screen != null && _screens.ContainsKey(screen) ? screen.Block as TBlock : null;

	public override Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) =>
		RunTransitionAsync(ClearSelectionCoreAsync, cancellationToken);

	public override Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(async token => {
			Guard.Argument(mode is ScreenMode.SingleView or ScreenMode.MultiView, nameof(mode), "Unknown screen mode.");
			if (_screenMode == mode)
				return true;
			if (mode == ScreenMode.SingleView && !await CloseCoreAsync(OpenScreens.Where(screen => !ReferenceEquals(screen, _activeScreen) && !IsPermanent(screen)).ToArray(), null, token))
				return false;
			_screenMode = mode;
			OnChanged();
			return true;
		}, cancellationToken);

	public override Task MoveScreenAsync(TScreen screen, int index, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			token.ThrowIfCancellationRequested();
			MoveTrackedScreen(screen, index);
			OnChanged();
			return Task.FromResult(true);
		}, cancellationToken);

	public override Task<bool> CloseScreensAsync(IEnumerable<TScreen> screens, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		var closing = screens.Distinct<TScreen>(ReferenceEqualityComparer.Instance).ToArray();
		return RunTransitionAsync(token => {
			foreach (var screen in closing)
				RequireRetainedScreen(screen);
			return CloseCoreAsync(closing, null, token);
		}, cancellationToken);
	}

	public override Task<bool> ShowScreenAsync(TScreen screen, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			RequireRetainedScreen(screen);
			return ShowCoreAsync(screen, token);
		}, cancellationToken);

	public override Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => _activeScreen == null ? Task.FromResult(true) : CloseCoreAsync(new[] { _activeScreen }, null, token), cancellationToken);

	public override Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => _activeScreen == null ? Task.FromResult(true) : IsPermanent(_activeScreen) ? Task.FromResult(false) : CanDeactivateAsync(_activeScreen, token), cancellationToken);

	public override Task<bool> CloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default) =>
		CloseScreensAsync(new[] { screen }, cancellationToken);

	public override Task<bool> CanCloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => {
			RequireRetainedScreen(screen);
			return IsPermanent(screen) ? Task.FromResult(false) : CanDeactivateAsync(screen, token);
		}, cancellationToken);

	public override Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) =>
		RunTransitionAsync(token => CanDeactivateAllAsync(Screens, token), cancellationToken);

	public override void NotifyScreenChanged(TScreen screen) {
		EnsureUsable();
		RequireRetainedScreen(screen);
		OnChanged();
	}

	protected virtual ApplicationScreenDefinition[] GetDefinitions() => GetDefinitions(TrackedBlocks);

	protected virtual ApplicationScreenDefinition[] GetDefinitions(TBlock[] blocks) {
		var definitions = Tools.UI.GetScreenDefinitions(blocks);
		foreach (var block in blocks)
			IndexBlockDefinitions(block);
		return definitions;
	}

	protected virtual ApplicationScreenDefinition GetStartupScreen() => Tools.UI.GetDefaultScreen(Blocks)
		?? (Blocks.FirstOrDefault() is { } first ? GetDefaultScreen(first) : null);

	protected virtual ApplicationScreenDefinition GetDefaultScreen(TBlock block) => block.DefaultScreenDefinition
		?? GetDefinitions(new[] { block }).FirstOrDefault(definition => definition.ScreenKind == ScreenKind.Normal)
		?? GetDefinitions(new[] { block }).FirstOrDefault();

	/// <summary>Resolves canonical registration ownership by reference. Activation always carries its explicit block.</summary>
	protected TBlock GetDefinitionBlock(ApplicationScreenDefinition definition) =>
		definition != null && _definitionBlocks.TryGetValue(definition, out var owners)
			? owners.Values.OrderBy(block => block.Position).FirstOrDefault(block => _blocks.ContainsKey(block))
				?? owners.Values.OrderBy(block => block.Position).FirstOrDefault() : null;

	protected virtual TScreen CreateScreen(TBlock block, ApplicationScreenDefinition definition) =>
		Guard.ArgumentCast<TScreen>(ActivatorUtilities.GetServiceOrCreateInstance(Services, definition.ScreenType), nameof(definition));

	/// <summary>Composite platform hosts override this to expose their public host rather than their internal engine.</summary>
	protected virtual IApplicationScreenHost ScreenOwner => this;

	protected virtual TScreen FindReusableScreen(TBlock block, ApplicationScreenDefinition definition) =>
		Tools.UI.IsSingleton(definition.ActivationMode ?? ScreenActivationMode.MultiInstance) && _singleInstances.TryGetValue(definition.ScreenType, out var screen) ? screen : null;

	protected virtual TScreen FindReusableScreen(TBlock block, ApplicationScreenDefinition definition, ApplicationScreenDefinition registration) =>
		FindReusableScreen(block, definition);

	protected virtual void NotifyScreenActivated(TScreen screen) {
	}

	protected virtual void OnScreenRemoved(TScreen screen) {
	}

	protected virtual void ValidateScreenType(Type screenType) =>
		Guard.Argument(screenType != null && typeof(TScreen).IsAssignableFrom(screenType), nameof(screenType), "The screen type is incompatible with this host.");

	protected virtual ApplicationScreenDefinition WithTitle(ApplicationScreenDefinition definition, string title) =>
		definition.WithTitle(title);

	protected ApplicationScreenDefinition ResolveDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		if (definition.ActivationMode.HasValue)
			return definition;
		var mode = GetScreenActivationDefinition(definition.ScreenType)?.ActivationMode ?? ScreenActivationMode.MultiInstance;
		return definition.WithActivationMode(mode);
	}

	protected ApplicationScreenDefinition GetScreenActivationDefinition(Type screenType) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		return _activationDefinitions.TryGetValue(screenType, out var definition) ? definition : null;
	}

	protected void ValidateScreenActivation(Type screenType, ScreenActivationMode activationMode) {
		Tools.UI.ValidateScreenType(screenType);
		Tools.UI.ValidateActivationMode(activationMode);
		var definition = GetScreenActivationDefinition(screenType);
		Guard.Argument(definition?.ActivationMode == null || definition.ActivationMode == activationMode,
			nameof(activationMode), $"The activation mode for {screenType.Name} is already defined and cannot change.");
	}

	protected async Task<TScreen> ActivateCoreAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken token, ApplicationScreenDefinition registration = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		var registeredDefinitions = GetDefinitions();
		ValidateDefinitions(registeredDefinitions.Concat(new[] { definition }).ToArray());
		token.ThrowIfCancellationRequested();
		RegisterTrackedDefinitions(registeredDefinitions);
		token.ThrowIfCancellationRequested();
		RegisterTrackedDefinitions(new[] { definition });
		var resolved = ResolveDefinition(definition);
		if (definition.ScreenKind == ScreenKind.Empty && _openOrder.Count > 0)
			return null;
		var existing = FindReusableScreen(block, resolved, registration ?? definition);
		if (existing != null) {
			if (!await ShowCoreAsync(existing, token))
				return null;
			NotifyScreenActivated(existing);
			return existing;
		}
		if (!await CanDeactivateAsync(_activeScreen, token))
			return null;
		await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		var screen = RetainScreen(block, definition, registration);
		HideInSingleView(_activeScreen);
		SetTrackedSelection(screen);
		OnChanged();
		await screen.OnActivatedAsync(CancellationToken.None);
		NotifyScreenActivated(screen);
		return screen;
	}

	protected virtual async Task<bool> CanDeactivateAsync(TScreen screen, CancellationToken token) {
		token.ThrowIfCancellationRequested();
		var allowed = screen == null || await screen.CanDeactivateAsync(token);
		token.ThrowIfCancellationRequested();
		return allowed;
	}

	protected virtual async Task<bool> CanDeactivateAllAsync(IEnumerable<TScreen> screens, CancellationToken token) {
		foreach (var screen in screens)
			if (!await CanDeactivateAsync(screen, token))
				return false;
		return true;
	}

	protected virtual Task NotifyDeactivatedAsync(TScreen screen, CancellationToken token) => screen?.OnDeactivatedAsync(token) ?? Task.CompletedTask;

	protected void TrackScreen(TScreen screen, TBlock block = null, ApplicationScreenDefinition definition = null, ScreenActivationMode? activationMode = null, ScreenKind? screenKind = null) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		Guard.Ensure(!_screens.ContainsKey(screen), "The screen is already retained.");
		Guard.Argument(screen.Host == null || ReferenceEquals(screen.Host, ScreenOwner), nameof(screen), "The screen belongs to another host.");
		Guard.Argument(screen.Block == null || ReferenceEquals(screen.Block, block), nameof(screen), "The screen belongs to another block.");
		var type = definition?.ScreenType ?? screen.GetType();
		var mode = activationMode ?? (definition == null ? null : ResolveDefinition(definition).ActivationMode) ?? ScreenActivationMode.MultiInstance;
		var kind = screenKind ?? definition?.ScreenKind ?? ScreenKind.Normal;
		Tools.UI.ValidateScreenPolicy(mode, kind);
		if (Tools.UI.IsSingleton(mode))
			Guard.Ensure(!_singleInstances.ContainsKey(type), "A singleton screen of this type is already retained.");
		ValidateScreenActivation(type, mode);
		var previousHost = screen.Host;
		var previousBlock = screen.Block;
		var ownedMenus = Array.Empty<IApplicationMenu>();
		var retained = false;
		using var attachment = Tools.Scope.ExecuteOnDispose(() => {
			if (retained)
				return;
			foreach (var menu in ownedMenus)
				if (ReferenceEquals(menu.Parent, block))
					menu.Parent = null;
			try {
				screen.Block = previousBlock;
			} finally {
				screen.Host = previousHost;
			}
		});
		screen.Host = ScreenOwner;
		screen.Block = block;
		ownedMenus = screen.Menus.Where(menu => menu.Parent == null).Distinct<IApplicationMenu>(ReferenceEqualityComparer.Instance).ToArray();
		foreach (var menu in ownedMenus)
			menu.Parent = block;
		_activationDefinitions[type] = definition == null ? new ApplicationScreenDefinition(type, screen.Title, mode, kind)
			: definition.ActivationMode == mode ? definition : definition.WithActivationMode(mode);
		_screens.Add(screen, (definition, type, mode, kind, ownedMenus));
		if (kind == ScreenKind.Normal)
			_openOrder.Add(screen);
		if (Tools.UI.IsSingleton(mode))
			_singleInstances.Add(type, screen);
		retained = true;
	}

	protected void RemoveTrackedScreen(TScreen screen) {
		RequireRetainedScreen(screen);
		var type = GetScreenType(screen);
		var ownedMenus = _screens[screen].OwnedMenus;
		_screens.Remove(screen);
		RemoveOpenScreen(screen);
		if (_singleInstances.TryGetValue(type, out var existing) && ReferenceEquals(existing, screen))
			_singleInstances.Remove(type);
		using var ownership = Tools.Scope.ExecuteOnDispose(() => DetachScreenOwnership(screen, ownedMenus));
		OnScreenRemoved(screen);
	}

	protected void SetTrackedSelection(TScreen screen) {
		if (screen != null)
			RequireRetainedScreen(screen);
		_activeScreen = screen;
		var owner = GetScreenBlock(screen);
		if (owner != null && _blocks.ContainsKey(owner))
			SetTrackedBlockSelection(owner);
		OnScreenSelected(screen);
	}

	protected void SetTrackedOpen(TScreen screen, bool open) {
		RequireRetainedScreen(screen);
		if (open && GetScreenKind(screen) == ScreenKind.Normal && IndexOfOpenScreen(screen) < 0)
			_openOrder.Add(screen);
		else if (!open && !IsPermanent(screen))
			RemoveOpenScreen(screen);
	}

	protected void MoveTrackedScreen(TScreen screen, int index) {
		Guard.Argument(screen != null && IndexOfOpenScreen(screen) >= 0, nameof(screen), "The screen has no open presentation.");
		Guard.ArgumentInRange(index, 0, _openOrder.Count - 1, nameof(index));
		RemoveOpenScreen(screen);
		_openOrder.Insert(index, screen);
	}

	protected void SetTrackedScreenMode(ScreenMode mode) {
		Guard.Argument(mode is ScreenMode.SingleView or ScreenMode.MultiView, nameof(mode), "Unknown screen mode.");
		_screenMode = mode;
	}

	protected void MarkInitialized() => _initialized = true;

	protected ApplicationScreenDefinition GetScreenDefinition(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].Definition;
	}

	protected Type GetScreenType(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].ScreenType;
	}

	protected ScreenActivationMode GetScreenActivationMode(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].ActivationMode;
	}

	protected ScreenKind GetScreenKind(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].ScreenKind;
	}

	protected void RegisterTrackedDefinitions(IEnumerable<ApplicationScreenDefinition> definitions) {
		Guard.ArgumentNotNull(definitions, nameof(definitions));
		var pending = definitions.ToArray();
		ValidateDefinitions(pending);
		foreach (var definition in pending)
			if (definition.ActivationMode.HasValue)
				_activationDefinitions[definition.ScreenType] = definition;
	}

	protected void AddAvailableBlock(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.Argument(block.Parent == null || ReferenceEquals(block.Parent.ScreenHost, ScreenOwner), nameof(block), "The block belongs to another application's host.");
		_availableBlocks[block] = block;
		IndexBlockDefinitions(block);
	}

	protected void RegisterTrackedBlock(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.Argument(block.Parent == null || ReferenceEquals(block.Parent.ScreenHost, ScreenOwner), nameof(block), "The block belongs to another application's host.");
		_blocks[block] = block;
		IndexBlockDefinitions(block);
		foreach (var definition in _blockDefinitions[block])
			OrderDefinitionOwners(_definitionBlocks[definition]);
	}

	protected void UnregisterTrackedBlock(TBlock block) {
		_blocks.Remove(block);
		foreach (var definition in _blockDefinitions[block])
			OrderDefinitionOwners(_definitionBlocks[definition]);
		if (ReferenceEquals(_activeBlock, block))
			SetTrackedBlockSelection(GetScreenBlock(_activeScreen) is { } owner && _blocks.ContainsKey(owner) ? owner : Blocks.FirstOrDefault());
	}

	protected void ReorderTrackedBlocks(IEnumerable<TBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var ordered = blocks.ToArray();
		Guard.Argument(ordered.Length == _blocks.Count && ordered.Distinct(ReferenceEqualityComparer.Instance).Count() == ordered.Length
			&& ordered.All(block => _blocks.ContainsKey(block)), nameof(blocks), "Reordering must retain every registered block.");
		_blocks.Clear();
		foreach (var block in ordered)
			RegisterTrackedBlock(block);
		foreach (var owners in _definitionBlocks.Values)
			OrderDefinitionOwners(owners);
	}

	protected void SetTrackedBlockSelection(TBlock block) {
		if (ReferenceEquals(_activeBlock, block))
			return;
		_activeBlock = block;
		OnChanged();
	}

	protected async Task<TResult> RunTransitionAsync<TResult>(Func<CancellationToken, Task<TResult>> action, CancellationToken cancellationToken) {
		EnsureUsable();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeToken);
		await _transitions.WaitAsync(cancellation.Token);
		using var transition = Tools.Scope.ExecuteOnDispose(() => _transitions.Release());
		EnsureUsable();
		using var notifications = DeferNotifications();
		return await action(cancellation.Token);
	}

	/// <summary>Groups synchronous platform changes into one notification after membership and selection have committed.</summary>
	protected IDisposable DeferNotifications() {
		EnsureUsable();
		_transitionDepth++;
		return Tools.Scope.ExecuteOnDispose(() => {
			_transitionDepth--;
			if (_transitionDepth != 0)
				return;
			var selectionPending = _selectionPending;
			var selection = _pendingSelection;
			var changedPending = _changedPending;
			_selectionPending = false;
			_pendingSelection = null;
			_changedPending = false;
			if (selectionPending)
				OnScreenSelected(selection);
			if (changedPending)
				OnChanged();
		});
	}

	protected void EnsureUsable() => Guard.Ensure(!_disposed, "The screen host has been disposed.");

	protected override void OnScreenSelected(IApplicationScreen screen) {
		if (_disposed)
			return;
		if (_transitionDepth > 0) {
			_selectionPending = true;
			_pendingSelection = screen;
			return;
		}
		base.OnScreenSelected(screen);
	}

	protected override void OnChanged() {
		if (_disposed)
			return;
		if (_transitionDepth > 0) {
			_changedPending = true;
			return;
		}
		base.OnChanged();
	}

	protected override void FreeManagedResources() {
		if (_disposed)
			return;
		_disposed = true;
		_lifetime.Cancel();
		_activeScreen = null;
		_activeBlock = null;
		foreach (var (screen, metadata) in _screens)
			DetachScreenOwnership(screen, metadata.OwnedMenus);
		foreach (var block in AvailableBlocks)
			if (ReferenceEquals(block.Parent?.ScreenHost, ScreenOwner))
				block.Parent = null;
		_screens.Clear();
		_openOrder.Clear();
		_singleInstances.Clear();
		_blocks.Clear();
		_availableBlocks.Clear();
		_definitionBlocks.Clear();
		_blockDefinitions.Clear();
		_activationDefinitions.Clear();
		_lifetime.Dispose();
		_ownedUserInterface?.Dispose();
		base.FreeManagedResources();
	}

	private void DetachScreenOwnership(TScreen screen, IApplicationMenu[] ownedMenus) {
		foreach (var menu in ownedMenus)
			if (ReferenceEquals(menu.Parent, screen.Block))
				menu.Parent = null;
		screen.Block = null;
		screen.Host = null;
	}

	private void IndexBlockDefinitions(TBlock block) {
		var definitions = block.ScreenDefinitions;
		if (_blockDefinitions.TryGetValue(block, out var previous)) {
			if (previous.SequenceEqual(definitions, ReferenceEqualityComparer.Instance))
				return;
			foreach (var definition in previous)
				if (!definitions.Contains(definition, ReferenceEqualityComparer.Instance)
					&& _definitionBlocks.TryGetValue(definition, out var owners) && owners.Remove(block) && owners.Count == 0)
					_definitionBlocks.Remove(definition);
		}
		_blockDefinitions[block] = definitions;
		foreach (var definition in definitions) {
			if (!_definitionBlocks.TryGetValue(definition, out var owners)) {
				owners = new Dictionary<TBlock, TBlock>(ReferenceEqualityComparer.Instance);
				_definitionBlocks.Add(definition, owners);
			}
			owners[block] = block;
			OrderDefinitionOwners(owners);
		}
	}

	private void OrderDefinitionOwners(Dictionary<TBlock, TBlock> owners) {
		var ordered = _blocks.Values.Concat(_availableBlocks.Values).Distinct<TBlock>(ReferenceEqualityComparer.Instance).Where(owners.ContainsKey).ToArray();
		owners.Clear();
		foreach (var block in ordered)
			owners.Add(block, block);
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}

	private async Task<bool> ClearSelectionCoreAsync(CancellationToken token) {
		if (!await CanDeactivateAsync(_activeScreen, token))
			return false;
		await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		HideInSingleView(_activeScreen);
		SetTrackedSelection(null);
		if (_openOrder.Count == 0)
			await ShowFallbackAsync();
		OnChanged();
		return true;
	}

	private async Task<bool> ShowCoreAsync(TScreen screen, CancellationToken token) {
		if (IsEmpty(screen) && _openOrder.Count > 0)
			return false;
		if (ReferenceEquals(screen, _activeScreen)) {
			token.ThrowIfCancellationRequested();
			SetTrackedSelection(screen);
			return true;
		}
		if (!await CanDeactivateAsync(_activeScreen, token))
			return false;
		await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		HideInSingleView(_activeScreen);
		SetTrackedOpen(screen, true);
		SetTrackedSelection(screen);
		OnChanged();
		await screen.OnActivatedAsync(CancellationToken.None);
		return true;
	}

	private async Task<bool> CloseCoreAsync(TScreen[] closing, Action afterClose, CancellationToken token, bool unregistering = false) {
		if (!unregistering && closing.Any(IsPermanent))
			return false;
		if (!await CanDeactivateAllAsync(closing, token))
			return false;
		var activeClosing = closing.Any(screen => ReferenceEquals(screen, _activeScreen));
		var activeIndex = _activeScreen == null ? -1 : IndexOfOpenScreen(_activeScreen);
		var removedPermanentTypes = closing.Where(IsPermanent).Select(GetScreenType).ToHashSet();
		if (activeClosing)
			await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		foreach (var screen in closing)
			RemoveTrackedScreen(screen);
		if (activeClosing)
			_activeScreen = _openOrder.Count == 0 ? null : _openOrder[Math.Min(Math.Max(activeIndex, 0), _openOrder.Count - 1)];
		afterClose?.Invoke();
		if (unregistering)
			OpenPermanentScreens(GetDefinitions().Where(definition => removedPermanentTypes.Contains(definition.ScreenType)));
		if (_activeScreen == null)
			await ShowFallbackAsync();
		else if (activeClosing) {
			SetTrackedSelection(_activeScreen);
			await _activeScreen.OnActivatedAsync(CancellationToken.None);
		}
		OnChanged();
		return true;
	}

	private void HideInSingleView(TScreen screen) {
		if (screen == null || IsPermanent(screen) || GetScreenKind(screen) == ScreenKind.Normal && _screenMode != ScreenMode.SingleView)
			return;
		RemoveOpenScreen(screen);
		if (GetScreenActivationMode(screen) == ScreenActivationMode.MultiInstance)
			RemoveTrackedScreen(screen);
	}

	private TScreen RetainScreen(TBlock block, ApplicationScreenDefinition definition, ApplicationScreenDefinition registration = null) {
		var screen = CreateScreen(block, definition);
		TrackScreen(screen, block, registration ?? definition, ResolveDefinition(definition).ActivationMode, definition.ScreenKind);
		return screen;
	}

	private void OpenPermanentScreens(IEnumerable<ApplicationScreenDefinition> definitions) {
		foreach (var definition in definitions.Where(item => ResolveDefinition(item).ActivationMode == ScreenActivationMode.PermanentSingleton))
			if (!_singleInstances.ContainsKey(definition.ScreenType))
				RetainScreen(GetDefinitionBlock(definition), definition);
	}

	private async Task ShowFallbackAsync() {
		if (_openOrder.Count > 0) {
			SetTrackedSelection(_openOrder[0]);
			await _activeScreen.OnActivatedAsync(CancellationToken.None);
			return;
		}
		var empty = Tools.UI.GetEmptyScreen(Blocks);
		if (empty != null)
			await ActivateCoreAsync(GetDefinitionBlock(empty), empty, CancellationToken.None);
	}

	private void ValidateDefinitions(ApplicationScreenDefinition[] definitions) {
		Guard.ArgumentNotNull(definitions, nameof(definitions));
		Guard.Argument(definitions.All(definition => definition != null), nameof(definitions), "Definitions cannot be null.");
		foreach (var definition in definitions)
			ValidateScreenType(definition.ScreenType);
		var allDefinitions = GetDefinitions().Concat(definitions).ToArray();
		ApplicationScreenDefinition.ValidateDefinitions(allDefinitions);
		foreach (var definition in allDefinitions)
			if (definition.ActivationMode.HasValue)
				ValidateScreenActivation(definition.ScreenType, definition.ActivationMode.Value);
		foreach (var metadata in _screens.Values)
			Guard.Argument(!allDefinitions.Any(definition => definition.ScreenType == metadata.ScreenType && definition.ScreenKind != metadata.ScreenKind),
				nameof(definitions), "A retained screen's hosting role cannot change.");
	}

	private int IndexOfOpenScreen(TScreen screen) => _openOrder.FindIndex(candidate => ReferenceEquals(candidate, screen));

	private void RemoveOpenScreen(TScreen screen) {
		var index = IndexOfOpenScreen(screen);
		if (index >= 0)
			_openOrder.RemoveAt(index);
	}

	private bool IsPermanent(TScreen screen) => screen != null && GetScreenActivationMode(screen) == ScreenActivationMode.PermanentSingleton;

	private bool IsEmpty(TScreen screen) => screen != null && GetScreenKind(screen) == ScreenKind.Empty;

	private void RequireRetainedScreen(TScreen screen) => Guard.Argument(screen != null && _screens.ContainsKey(screen), nameof(screen), "The screen is not retained by this host.");

	private void RequireRegisteredBlock(TBlock block) => Guard.Argument(block != null && _blocks.ContainsKey(block), nameof(block), "The block is not registered.");

}

public class ApplicationScreenHost : ApplicationScreenHost<IApplicationBlock, IApplicationScreen> {
	public ApplicationScreenHost(IServiceProvider services)
		: base(services) {
	}

	public ApplicationScreenHost(IEnumerable<IApplicationBlock> blocks, IServiceProvider services, Func<IApplicationBlock, IApplicationBlock> snapshot = null)
		: base(blocks, services, snapshot) {
	}
}
