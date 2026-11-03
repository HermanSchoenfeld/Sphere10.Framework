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
	private readonly Dictionary<TScreen, ScreenState> _screens = new(ReferenceEqualityComparer.Instance);
	private readonly Dictionary<object, Dictionary<object, TScreen>> _singleInstances = new(ReferenceEqualityComparer.Instance);
	private readonly List<TScreen> _openOrder = new();
	private readonly SemaphoreSlim _transitions = new(1, 1);
	private readonly CancellationTokenSource _lifetime = new();
	private readonly CancellationToken _lifetimeToken;
	private TBlock _selectedBlock;
	private TScreen _activeScreen;
	private ScreenMode _screenMode = ScreenMode.MultiView;
	private bool _initialized;
	private bool _disposed;
	private int _transitionDepth;
	private bool _changedPending;
	private bool _selectionPending;
	private IApplicationScreen _pendingSelection;
	private ApplicationUserInterfaceServices _ownedUserInterface;

	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
	public ApplicationScreenHost(IServiceProvider services)
		: this(Array.Empty<TBlock>(), services) {
	}

	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
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

	/// <inheritdoc />
	public override TBlock[] AvailableBlocks {
		get {
			return _availableBlocks.Values.OrderBy(block => block.Position).ToArray();
		}
	}

	/// <inheritdoc />
	public override TBlock[] Blocks {
		get {
			return _blocks.Values.OrderBy(block => block.Position).ToArray();
		}
	}

	/// <summary>The canonical owner of the currently active screen.</summary>
	public override TBlock ActiveBlock {
		get {
			return _activeScreen?.Block as TBlock;
		}
	}

	/// <summary>The independently browsed navigation block.</summary>
	public override TBlock SelectedBlock {
		get {
			return _selectedBlock;
		}
	}

	/// <inheritdoc />
	public override TScreen ActiveScreen {
		get {
			return _activeScreen;
		}
	}

	/// <inheritdoc />
	public override TScreen[] Screens {
		get {
			return _screens.Keys.ToArray();
		}
	}

	/// <inheritdoc />
	public override TScreen[] OpenScreens {
		get {
			return _openOrder.ToArray();
		}
	}

	/// <inheritdoc />
	public override IUserInterfaceServices UserInterfaceServices {
		get {
			EnsureUsable();
			return Services.GetService<IUserInterfaceServices>() ?? (_ownedUserInterface ??= new ApplicationUserInterfaceServices(this, Services));
		}
	}

	/// <summary>Changes mode synchronously when no screens must close. Use TrySetScreenModeAsync for guarded transitions.</summary>
	public override ScreenMode ScreenMode {
		get {
			return _screenMode;
		}
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

	/// <inheritdoc />
	public override bool HasUnsavedChanges {
		get {
			return _screens.Keys.Any(screen => screen.HasUnsavedChanges);
		}
	}

	/// <summary>Gets the borrowed service provider used for screen activation.</summary>
	protected IServiceProvider Services { get; }

	/// <summary>Indicates whether this host's initialization has completed.</summary>
	protected bool IsInitialized {
		get {
			return _initialized;
		}
	}

	/// <summary>Gets the registered block membership retained by this host.</summary>
	protected TBlock[] TrackedBlocks {
		get {
			return _blocks.Values.ToArray();
		}
	}

	/// <inheritdoc />
	public override Task<bool> ShowScreenUndockedAsync(TScreen screen, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		EnsureUsable();
		Guard.ArgumentNotNull(screen, nameof(screen));
		throw new NotSupportedException("This screen host does not support detached presentation.");
	}

	/// <summary>Composite platform hosts override this to expose their public host rather than their internal engine.</summary>
	protected virtual IApplicationScreenHost ScreenOwner {
		get {
			return this;
		}
	}

	/// <inheritdoc />
	public override Task InitializeAsync(CancellationToken cancellationToken = default) {
		return RunTransitionAsync(async token => {
			if (_initialized)
				return true;
			var definitions = GetDefinitions();
			ValidateDefinitions(definitions);
			var replacingEmpty = IsEmpty(_activeScreen) && definitions.Any(definition => ResolveDefinition(definition).ActivationMode == ScreenActivationMode.PermanentSingleton);
			if (replacingEmpty) {
				if (!await CanDeactivateAsync(_activeScreen, token))
					return false;
			}
			token.ThrowIfCancellationRequested();
			RegisterTrackedDefinitions(definitions);

			// A failed replacement must leave the current empty screen's lifecycle untouched.
			OpenPermanentScreens(GetDefinitions(), token);
			if (replacingEmpty) {
				await NotifyDeactivatedAsync(_activeScreen, token);
				token.ThrowIfCancellationRequested();
				HideInSingleView(_activeScreen);
				_activeScreen = null;
			}
			if (_activeScreen == null) {
				var startup = GetStartupScreen();
				if (startup != null)
					await ActivateCoreAsync(GetDefinitionBlock(startup), startup, token);
				if (_activeScreen == null)
					await ShowFallbackAsync(token);
			}
			if (_selectedBlock == null)
				SetTrackedBlockSelection(_activeScreen?.Block as TBlock ?? Blocks.FirstOrDefault());
			_initialized = true;
			OnChanged();
			return true;
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task SelectBlockAsync(TBlock block, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			token.ThrowIfCancellationRequested();
			SetTrackedBlockSelection(block);
			return Task.FromResult(true);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<TScreen> ActivateBlockAsync(TBlock block, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(async token => {
			RequireRegisteredBlock(block);
			RegisterTrackedDefinitions(GetDefinitions());
			var definition = GetDefaultScreen(block);
			if (definition != null)
				return await ActivateCoreAsync(block, definition, token);
			if (await ClearSelectionCoreAsync(token))
				SetTrackedBlockSelection(block);
			return null;
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<TScreen> ActivateScreenAsync(TBlock block, Type screenType, string title = null, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			RegisterTrackedDefinitions(GetDefinitions());
			var definition = Tools.UI.ResolveScreenRegistration(GetDefinitions(new[] { block }), ApplicationScreenRegistration.ForType(screenType));
			Guard.Argument(definition != null, nameof(screenType), "The screen type is not registered in this block.");
			var registration = definition;
			if (title != null)
				definition = WithTitle(definition, title);
			return ActivateCoreAsync(block, definition, token, registration);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<TScreen> ActivateScreenAsync(TBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			Guard.ArgumentNotNull(item, nameof(item));
			RegisterTrackedDefinitions(GetDefinitions());
			var definition = GetDefinitions(new[] { block }).FirstOrDefault(candidate => ReferenceEquals(candidate, item.ScreenDefinition));
			Guard.Argument(definition != null, nameof(item), "The menu item is not a registered screen in this block.");
			return ActivateCoreAsync(block, definition, token);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<TScreen> OpenScreenAsync(TBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			Guard.Argument(definition != null && block.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, definition)),
				nameof(definition), "The screen definition is not owned by this block.");
			return ActivateCoreAsync(block, definition, token);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task RegisterBlockAsync(TBlock block, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(async token => {
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
			}
			token.ThrowIfCancellationRequested();
			RegisterTrackedDefinitions(definitions);
			RegisterTrackedBlock(block);
			if (_initialized) {
				OpenPermanentScreens(GetDefinitions(), token);
				if (replacingEmpty) {
					await NotifyDeactivatedAsync(_activeScreen, token);
					token.ThrowIfCancellationRequested();
					HideInSingleView(_activeScreen);
					_activeScreen = null;
				}
				if (_activeScreen == null)
					await ShowFallbackAsync(token);
			}
			OnChanged();
			return true;
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> UnregisterBlockAsync(TBlock block, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			RequireRegisteredBlock(block);
			return CloseCoreAsync(Screens.Where(screen => ReferenceEquals(screen.Block, block)).ToArray(), () => {
				UnregisterTrackedBlock(block);
			}, token, true);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) {
		return RunTransitionAsync(ClearSelectionCoreAsync, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(async token => {
			Guard.Argument(mode is ScreenMode.SingleView or ScreenMode.MultiView, nameof(mode), "Unknown screen mode.");
			if (_screenMode == mode)
				return true;
			if (mode == ScreenMode.SingleView && !await CloseCoreAsync(OpenScreens.Where(screen => !ReferenceEquals(screen, _activeScreen) && !IsPermanent(screen)).ToArray(), null, token))
				return false;
			_screenMode = mode;
			OnChanged();
			return true;
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task MoveScreenAsync(TScreen screen, int index, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			token.ThrowIfCancellationRequested();
			MoveTrackedScreen(screen, index);
			OnChanged();
			return Task.FromResult(true);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> CloseScreensAsync(IEnumerable<TScreen> screens, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		var closing = screens.Distinct<TScreen>(ReferenceEqualityComparer.Instance).ToArray();
		return RunTransitionAsync(token => {
			foreach (var screen in closing)
				RequireRetainedScreen(screen);
			return CloseCoreAsync(closing, null, token);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> ShowScreenAsync(TScreen screen, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			RequireRetainedScreen(screen);
			return ShowCoreAsync(screen, token);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => _activeScreen == null ? Task.FromResult(true) : CloseCoreAsync(new[] { _activeScreen }, null, token), cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => _activeScreen == null ? Task.FromResult(true) : IsPermanent(_activeScreen) ? Task.FromResult(false) : CanDeactivateAsync(_activeScreen, token), cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> CloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default) {
		return CloseScreensAsync(new[] { screen }, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> CanCloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => {
			RequireRetainedScreen(screen);
			return IsPermanent(screen) ? Task.FromResult(false) : CanDeactivateAsync(screen, token);
		}, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) {
		return RunTransitionAsync(token => CanDeactivateAllAsync(Screens, token), cancellationToken);
	}

	/// <inheritdoc />
	public override void NotifyScreenChanged(TScreen screen) {
		EnsureUsable();
		RequireRetainedScreen(screen);
		OnChanged();
	}

	/// <summary>Returns canonical definitions in their existing host or supplied-block declaration order.</summary>
	protected virtual ApplicationScreenDefinition[] GetDefinitions() {
		return GetDefinitions(TrackedBlocks);
	}

	/// <summary>Returns canonical definitions in their existing host or supplied-block declaration order.</summary>
	protected virtual ApplicationScreenDefinition[] GetDefinitions(TBlock[] blocks) {
		var definitions = Tools.UI.GetScreenDefinitions(blocks);
		foreach (var block in blocks)
			IndexBlockDefinitions(block);
		return definitions;
	}

	/// <summary>Returns the screen selected for startup when a platform overrides the default policy.</summary>
	protected virtual ApplicationScreenDefinition GetStartupScreen() {
		return Tools.UI.GetDefaultScreen(Blocks)
		?? (Blocks.FirstOrDefault() is { } first ? GetDefaultScreen(first) : null);
	}

	/// <summary>Returns the block's canonical selected default definition.</summary>
	protected virtual ApplicationScreenDefinition GetDefaultScreen(TBlock block) {
		return block.DefaultScreenDefinition
		?? GetDefinitions(new[] { block }).FirstOrDefault(definition => definition.ScreenKind == ScreenKind.Normal)
		?? GetDefinitions(new[] { block }).FirstOrDefault();
	}

	/// <summary>Resolves canonical registration ownership by reference. Activation always carries its explicit block.</summary>
	protected TBlock GetDefinitionBlock(ApplicationScreenDefinition definition) {
		return definition != null && _definitionBlocks.TryGetValue(definition, out var owners)
			? owners.Values.OrderBy(block => block.Position).FirstOrDefault(block => _blocks.ContainsKey(block))
				?? owners.Values.OrderBy(block => block.Position).FirstOrDefault() : null;
	}

	/// <summary>Constructs a screen using the existing service activation owner.</summary>
	protected virtual TScreen CreateScreen(TBlock block, ApplicationScreenDefinition definition) {
		return Guard.ArgumentCast<TScreen>(ActivatorUtilities.GetServiceOrCreateInstance(Services, definition.ScreenType), nameof(definition));
	}

	/// <summary>Finds a retained instance matching this block, registration and lifetime policy.</summary>
	protected virtual TScreen FindReusableScreen(TBlock block, ApplicationScreenDefinition definition) {
		return Tools.UI.IsSingleton(definition.ActivationMode ?? ScreenActivationMode.MultiInstance)
			&& _singleInstances.TryGetValue((object)block ?? this, out var registrations)
			&& registrations.TryGetValue(definition.Registration.Token, out var screen) ? screen : null;
	}

	/// <summary>Finds a retained instance matching this block, registration and lifetime policy.</summary>
	protected virtual TScreen FindReusableScreen(TBlock block, ApplicationScreenDefinition definition, ApplicationScreenDefinition registration) {
		return FindReusableScreen(block, definition);
	}

	/// <summary>Publishes accepted activation of a retained screen.</summary>
	protected virtual void NotifyScreenActivated(TScreen screen) {
	}

	/// <summary>Prepares an approved close before any screen is deactivated or removed.</summary>
	protected virtual Task OnClosingScreensAsync(TScreen[] screens, CancellationToken cancellationToken) {
		return Task.CompletedTask;
	}

	/// <summary>Provides the platform removal hook after a screen leaves the retained graph.</summary>
	protected virtual void OnScreenRemoved(TScreen screen) {
	}

	/// <summary>Validates that a declared type can be hosted by this typed screen host.</summary>
	protected virtual void ValidateScreenType(Type screenType) {
		Guard.Argument(screenType != null && typeof(TScreen).IsAssignableFrom(screenType), nameof(screenType), "The screen type is incompatible with this host.");
	}

	/// <summary>Creates a registration metadata variant with the requested presentation title.</summary>
	protected virtual ApplicationScreenDefinition WithTitle(ApplicationScreenDefinition definition, string title) {
		return definition.WithTitle(title);
	}

	/// <summary>Resolves the effective registered metadata used for retention and validation.</summary>
	protected ApplicationScreenDefinition ResolveDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		if (definition.ActivationMode.HasValue)
			return definition;
		var mode = ScreenActivationMode.MultiInstance;
		return definition.WithActivationMode(mode);
	}

	/// <summary>Checks activation policy compatibility for the supplied screen type.</summary>
	protected void ValidateScreenActivation(Type screenType, ScreenActivationMode activationMode) {
		Tools.UI.ValidateScreenType(screenType);
		Tools.UI.ValidateActivationMode(activationMode);
	}

	/// <summary>Resolves lifetime reuse and commits activation only after the current screen accepts navigation.</summary>
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

		// Prepare through the existing adapter hook before changing the current screen's lifecycle.
		var screen = CreateScreen(block, definition);
		ValidateScreenAttachment(screen, block);
		token.ThrowIfCancellationRequested();

		await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		TrackScreen(screen, block, registration ?? definition, resolved.ActivationMode, definition.ScreenKind);
		HideInSingleView(_activeScreen);
		SetTrackedSelection(screen);
		OnChanged();
		await screen.OnActivatedAsync(CancellationToken.None);
		NotifyScreenActivated(screen);
		return screen;
	}

	/// <summary>Checks a retained screen's navigation guard before a transition may commit.</summary>
	protected virtual async Task<bool> CanDeactivateAsync(TScreen screen, CancellationToken token) {
		token.ThrowIfCancellationRequested();
		var allowed = screen == null || await screen.CanDeactivateAsync(token);
		token.ThrowIfCancellationRequested();
		return allowed;
	}

	/// <summary>Checks every affected screen before a batch transition changes ownership.</summary>
	protected virtual async Task<bool> CanDeactivateAllAsync(IEnumerable<TScreen> screens, CancellationToken token) {
		foreach (var screen in screens)
			if (!await CanDeactivateAsync(screen, token))
				return false;
		return true;
	}

	/// <summary>Awaits the retained screen's accepted deactivation callback.</summary>
	protected virtual Task NotifyDeactivatedAsync(TScreen screen, CancellationToken token) {
		return screen?.OnDeactivatedAsync(token) ?? Task.CompletedTask;
	}

	/// <summary>Captures canonical registration policy and attaches the screen to this host and block.</summary>
	protected void TrackScreen(TScreen screen, TBlock block = null, ApplicationScreenDefinition definition = null, ScreenActivationMode? activationMode = null, ScreenKind? screenKind = null) {
		ValidateScreenAttachment(screen, block);
		var type = definition?.ScreenType ?? screen.GetType();
		var mode = activationMode ?? (definition == null ? null : ResolveDefinition(definition).ActivationMode) ?? ScreenActivationMode.MultiInstance;
		var kind = screenKind ?? definition?.ScreenKind ?? ScreenKind.Normal;
		Tools.UI.ValidateScreenPolicy(mode, kind);
		var registrationKey = (object)definition?.Registration.Token ?? type;
		var ownerKey = (object)block ?? this;
		if (Tools.UI.IsSingleton(mode))
			Guard.Ensure(!_singleInstances.TryGetValue(ownerKey, out var existingRegistrations) || !existingRegistrations.ContainsKey(registrationKey),
				"A singleton screen for this registration is already retained in this block.");
		ValidateScreenActivation(type, mode);
		var previousHost = screen.Host;
		var previousBlock = screen.Block;
		var retained = false;
		using var attachment = Tools.Scope.ExecuteOnDispose(() => {
			if (retained)
				return;
			try {
				screen.Block = previousBlock;
			} finally {
				screen.Host = previousHost;
			}
		});
		screen.Host = ScreenOwner;
		screen.Block = block;
		_screens.Add(screen, new ScreenState(definition, type, mode, kind));
		if (kind == ScreenKind.Normal)
			_openOrder.Add(screen);
		if (Tools.UI.IsSingleton(mode)) {
			if (!_singleInstances.TryGetValue(ownerKey, out var registrations)) {
				registrations = new Dictionary<object, TScreen>();
				_singleInstances.Add(ownerKey, registrations);
			}
			registrations.Add(registrationKey, screen);
		}
		retained = true;
	}

	/// <summary>Removes a retained screen and releases its ownership links.</summary>
	protected void RemoveTrackedScreen(TScreen screen) {
		RequireRetainedScreen(screen);
		var registrationKey = (object)GetScreenDefinition(screen)?.Registration.Token ?? GetScreenType(screen);
		var ownerKey = (object)screen.Block ?? this;
		_screens.Remove(screen);
		RemoveOpenScreen(screen);
		if (_singleInstances.TryGetValue(ownerKey, out var registrations) && registrations.TryGetValue(registrationKey, out var existing) && ReferenceEquals(existing, screen)) {
			registrations.Remove(registrationKey);
			if (registrations.Count == 0)
				_singleInstances.Remove(ownerKey);
		}
		using var ownership = Tools.Scope.ExecuteOnDispose(() => DetachScreenOwnership(screen));
		OnScreenRemoved(screen);
	}

	/// <summary>Updates the retained active screen and its active block projection.</summary>
	protected void SetTrackedSelection(TScreen screen) {
		if (screen != null)
			RequireRetainedScreen(screen);
		_activeScreen = screen;
		var owner = screen?.Block as TBlock;
		if (owner != null && _blocks.ContainsKey(owner))
			SetTrackedBlockSelection(owner);
		OnScreenSelected(screen);
	}

	/// <summary>Updates whether a retained screen participates in the visible presentation order.</summary>
	protected void SetTrackedOpen(TScreen screen, bool open) {
		RequireRetainedScreen(screen);
		if (open && GetScreenKind(screen) == ScreenKind.Normal && IndexOfOpenScreen(screen) < 0)
			_openOrder.Add(screen);
		else if (!open && !IsPermanent(screen))
			RemoveOpenScreen(screen);
	}

	/// <summary>Moves a retained open screen without changing registration identity.</summary>
	protected void MoveTrackedScreen(TScreen screen, int index) {
		Guard.Argument(screen != null && IndexOfOpenScreen(screen) >= 0, nameof(screen), "The screen has no open presentation.");
		Guard.ArgumentInRange(index, 0, _openOrder.Count - 1, nameof(index));
		RemoveOpenScreen(screen);
		_openOrder.Insert(index, screen);
	}

	/// <summary>Updates the accepted presentation mode.</summary>
	protected void SetTrackedScreenMode(ScreenMode mode) {
		Guard.Argument(mode is ScreenMode.SingleView or ScreenMode.MultiView, nameof(mode), "Unknown screen mode.");
		_screenMode = mode;
	}

	/// <summary>Marks initialization complete for the existing host lifecycle.</summary>
	protected void MarkInitialized() {
		_initialized = true;
	}

	/// <summary>Returns the canonical definition captured for this retained screen.</summary>
	protected ApplicationScreenDefinition GetScreenDefinition(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].Definition;
	}

	/// <summary>Returns the type captured when this screen was retained.</summary>
	protected Type GetScreenType(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].ScreenType;
	}

	/// <summary>Returns the activation policy captured for the retained screen.</summary>
	protected ScreenActivationMode GetScreenActivationMode(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].ActivationMode;
	}

	/// <summary>Returns the presentation kind captured for the retained screen.</summary>
	protected ScreenKind GetScreenKind(TScreen screen) {
		RequireRetainedScreen(screen);
		return _screens[screen].ScreenKind;
	}

	/// <summary>Validates the current definition set and records its block ownership.</summary>
	protected void RegisterTrackedDefinitions(IEnumerable<ApplicationScreenDefinition> definitions) {
		Guard.ArgumentNotNull(definitions, nameof(definitions));
		var pending = definitions.ToArray();
		ValidateDefinitions(pending);
	}

	/// <summary>Captures a block in the available registration set.</summary>
	protected void AddAvailableBlock(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.Argument(block.Parent == null || ReferenceEquals(block.Parent.ScreenHost, ScreenOwner), nameof(block), "The block belongs to another application's host.");
		_availableBlocks[block] = block;
		IndexBlockDefinitions(block);
	}

	/// <summary>Indexes a block's canonical definitions and registers its live membership.</summary>
	protected void RegisterTrackedBlock(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.Argument(block.Parent == null || ReferenceEquals(block.Parent.ScreenHost, ScreenOwner), nameof(block), "The block belongs to another application's host.");
		_blocks[block] = block;
		IndexBlockDefinitions(block);
		foreach (var definition in _blockDefinitions[block])
			OrderDefinitionOwners(_definitionBlocks[definition]);
	}

	/// <summary>Removes a block's live membership after its screens have been released.</summary>
	protected void UnregisterTrackedBlock(TBlock block) {
		_blocks.Remove(block);
		foreach (var definition in _blockDefinitions[block])
			OrderDefinitionOwners(_definitionBlocks[definition]);
		if (ReferenceEquals(_selectedBlock, block))
			SetTrackedBlockSelection(_activeScreen?.Block is TBlock owner && _blocks.ContainsKey(owner) ? owner : Blocks.FirstOrDefault());
	}

	/// <summary>Applies a validated block ordering without changing retained membership.</summary>
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

	/// <summary>Updates the independently browsed block without changing the active screen.</summary>
	protected void SetTrackedBlockSelection(TBlock block) {
		if (ReferenceEquals(_selectedBlock, block))
			return;
		_selectedBlock = block;
		OnChanged();
	}

	/// <summary>Serializes guarded transitions and publishes their completed state changes.</summary>
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
	protected ActionScope DeferNotifications() {
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

	/// <summary>Rejects operations after this host has been disposed.</summary>
	protected void EnsureUsable() {
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	protected override void OnChanged() {
		if (_disposed)
			return;
		if (_transitionDepth > 0) {
			_changedPending = true;
			return;
		}
		base.OnChanged();
	}

	/// <inheritdoc />
	protected override void DisposeInternal() {
		if (_disposed)
			return;
		_disposed = true;
		_lifetime.Cancel();
		_activeScreen = null;
		_selectedBlock = null;
		foreach (var screen in _screens.Keys)
			DetachScreenOwnership(screen);
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
		_lifetime.Dispose();
		_ownedUserInterface?.Dispose();
		base.DisposeInternal();
	}

	/// <summary>Clears the removed screen's host and block references.</summary>
	private void DetachScreenOwnership(TScreen screen) {
		screen.Block = null;
		screen.Host = null;
	}

	/// <summary>Adds the block's canonical definitions to the existing owner index.</summary>
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

	/// <summary>Orders canonical definition owners using the existing block position policy.</summary>
	private void OrderDefinitionOwners(Dictionary<TBlock, TBlock> owners) {
		var ordered = _blocks.Values.Concat(_availableBlocks.Values).Distinct<TBlock>(ReferenceEqualityComparer.Instance).Where(owners.ContainsKey).ToArray();
		owners.Clear();
		foreach (var block in ordered)
			owners.Add(block, block);
	}

	/// <inheritdoc />
	protected override ValueTask DisposeInternalAsync() {
		DisposeInternal();
		return ValueTask.CompletedTask;
	}

	/// <summary>Releases the current selection through its guard and existing fallback policy.</summary>
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

	/// <summary>Selects a retained screen after the current screen accepts navigation.</summary>
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

	/// <summary>Commits an accepted batch close before choosing the next retained presentation.</summary>
	private async Task<bool> CloseCoreAsync(TScreen[] closing, Action afterClose, CancellationToken token, bool unregistering = false) {
		if (!unregistering && closing.Any(IsPermanent))
			return false;
		if (!await CanDeactivateAllAsync(closing, token))
			return false;
		token.ThrowIfCancellationRequested();

		// Platform close preparation can fail while the complete screen graph is still retained.
		await OnClosingScreensAsync(closing, token);
		token.ThrowIfCancellationRequested();

		var activeClosing = closing.Any(screen => ReferenceEquals(screen, _activeScreen));
		var activeIndex = _activeScreen == null ? -1 : IndexOfOpenScreen(_activeScreen);
		var nextSelection = _activeScreen;
		if (activeClosing) {
			// Resolve the neighbor before removal so a batch closing earlier tabs cannot skip the next surviving tab.
			var closingSet = new HashSet<TScreen>(closing, ReferenceEqualityComparer.Instance);
			nextSelection = _openOrder.Skip(activeIndex + 1).FirstOrDefault(screen => !closingSet.Contains(screen))
				?? _openOrder.Take(activeIndex).LastOrDefault(screen => !closingSet.Contains(screen));
		}
		var removedPermanentRegistrations = closing.Where(IsPermanent).Select(GetScreenDefinition).Where(definition => definition != null).Select(definition => definition.Registration.Token).ToHashSet();
		if (activeClosing)
			await NotifyDeactivatedAsync(_activeScreen, token);
		token.ThrowIfCancellationRequested();
		foreach (var screen in closing)
			RemoveTrackedScreen(screen);
		if (activeClosing)
			_activeScreen = nextSelection;
		afterClose?.Invoke();
		if (unregistering)
			OpenPermanentScreens(GetDefinitions().Where(definition => removedPermanentRegistrations.Contains(definition.Registration.Token)));
		if (_activeScreen == null && unregistering)
			await ShowFallbackAsync();
		else if (activeClosing) {
			SetTrackedSelection(_activeScreen);
			if (_activeScreen != null)
				await _activeScreen.OnActivatedAsync(CancellationToken.None);
		}
		OnChanged();
		return true;
	}

	/// <summary>Hides or releases the previous presentation according to its existing lifetime policy.</summary>
	private void HideInSingleView(TScreen screen) {
		if (screen == null || IsPermanent(screen) || GetScreenKind(screen) == ScreenKind.Normal && _screenMode != ScreenMode.SingleView)
			return;
		RemoveOpenScreen(screen);
		if (GetScreenActivationMode(screen) == ScreenActivationMode.MultiInstance)
			RemoveTrackedScreen(screen);
	}

	// Creation does not transfer ownership until the caller's transition is still allowed to commit.
	private TScreen RetainScreen(TBlock block, ApplicationScreenDefinition definition, CancellationToken token) {
		token.ThrowIfCancellationRequested();
		var screen = CreateScreen(block, definition);
		token.ThrowIfCancellationRequested();
		TrackScreen(screen, block, definition, ResolveDefinition(definition).ActivationMode, definition.ScreenKind);
		return screen;
	}

	/// <summary>Retains the permanent registrations owned by the supplied canonical definition set.</summary>
	private void OpenPermanentScreens(IEnumerable<ApplicationScreenDefinition> definitions, CancellationToken token = default) {
		// A shared prototype still represents a separate registration inside each owning block.
		var candidates = new HashSet<ApplicationScreenDefinition>(definitions, ReferenceEqualityComparer.Instance);
		foreach (var block in Blocks)
			foreach (var definition in block.ScreenDefinitions.Where(definition => candidates.Contains(definition)
				&& ResolveDefinition(definition).ActivationMode == ScreenActivationMode.PermanentSingleton))
				if (FindReusableScreen(block, ResolveDefinition(definition), definition) == null)
					RetainScreen(block, definition, token);
	}

	/// <summary>Selects an existing open screen or the registered empty fallback.</summary>
	private async Task ShowFallbackAsync(CancellationToken token = default) {
		token.ThrowIfCancellationRequested();
		if (_openOrder.Count > 0) {
			SetTrackedSelection(_openOrder[0]);
			await _activeScreen.OnActivatedAsync(CancellationToken.None);
			return;
		}
		var empty = Tools.UI.GetEmptyScreen(Blocks);
		if (empty != null)
			await ActivateCoreAsync(GetDefinitionBlock(empty), empty, token);
	}

	// Reject invalid adapter results before any existing screen is deactivated or a new screen is attached.
	private void ValidateScreenAttachment(TScreen screen, TBlock block) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		Guard.Ensure(!_screens.ContainsKey(screen), "The screen is already retained.");
		Guard.Argument(screen.Host == null || ReferenceEquals(screen.Host, ScreenOwner), nameof(screen), "The screen belongs to another host.");
		Guard.Argument(screen.Block == null || ReferenceEquals(screen.Block, block), nameof(screen), "The screen belongs to another block.");
	}

	/// <summary>Validates canonical definitions and their captured screen policies before host state changes.</summary>
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
		foreach (var (screen, metadata) in _screens)
			Guard.Argument(!allDefinitions.Any(definition => definition.Registration.Token == metadata.Definition?.Registration.Token && definition.ScreenKind != metadata.ScreenKind
				&& screen.Block?.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, definition)) == true),
				nameof(definitions), "A retained screen's hosting role cannot change.");
	}

	/// <summary>Finds an open screen using reference identity.</summary>
	private int IndexOfOpenScreen(TScreen screen) {
		return _openOrder.FindIndex(candidate => ReferenceEquals(candidate, screen));
	}

	/// <summary>Removes the retained screen from the current presentation order.</summary>
	private void RemoveOpenScreen(TScreen screen) {
		var index = IndexOfOpenScreen(screen);
		if (index >= 0)
			_openOrder.RemoveAt(index);
	}

	/// <summary>Checks the activation policy captured for the retained screen.</summary>
	private bool IsPermanent(TScreen screen) {
		return screen != null && GetScreenActivationMode(screen) == ScreenActivationMode.PermanentSingleton;
	}

	/// <summary>Checks whether the retained screen is an empty presentation.</summary>
	private bool IsEmpty(TScreen screen) {
		return screen != null && GetScreenKind(screen) == ScreenKind.Empty;
	}

	/// <summary>Requires the supplied instance to belong to this host by reference identity.</summary>
	private void RequireRetainedScreen(TScreen screen) {
		Guard.Argument(screen != null && _screens.ContainsKey(screen), nameof(screen), "The screen is not retained by this host.");
	}

	/// <summary>Requires the supplied block to be registered by reference identity.</summary>
	private void RequireRegisteredBlock(TBlock block) {
		Guard.Argument(block != null && _blocks.ContainsKey(block), nameof(block), "The block is not registered.");
	}

	#region Inner Types

	// Retain one screen's resolved registration and activation policy for its host lifetime.
	private sealed class ScreenState {

		// Capture the registration's policy when the screen is retained.
		public ScreenState(ApplicationScreenDefinition definition, Type screenType, ScreenActivationMode activationMode, ScreenKind screenKind) {
			Definition = definition;
			ScreenType = screenType;
			ActivationMode = activationMode;
			ScreenKind = screenKind;
		}

		/// <summary>The registration that created this screen.</summary>
		public ApplicationScreenDefinition Definition { get; }

		/// <summary>The resolved concrete screen type.</summary>
		public Type ScreenType { get; }

		/// <summary>The retained screen's activation policy.</summary>
		public ScreenActivationMode ActivationMode { get; }

		/// <summary>The retained screen's workspace role.</summary>
		public ScreenKind ScreenKind { get; }
	}

	#endregion
}

/// <summary>Hosts neutral application blocks and screens through the shared retention lifecycle.</summary>
public class ApplicationScreenHost : ApplicationScreenHost<IApplicationBlock, IApplicationScreen> {
	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
	public ApplicationScreenHost(IServiceProvider services)
		: base(services) {
	}

	/// <summary>Initializes the existing host or test adapter without changing its registration ownership.</summary>
	public ApplicationScreenHost(IEnumerable<IApplicationBlock> blocks, IServiceProvider services, Func<IApplicationBlock, IApplicationBlock> snapshot = null)
		: base(blocks, services, snapshot) {
	}
}
