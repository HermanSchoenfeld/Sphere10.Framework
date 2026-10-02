// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationScreenHost : WinFormsApplicationScreenHostBase {
	private const int LogicalDockProximity = 8;
	private readonly Dictionary<WinFormsApplicationScreen, ScreenBinding> _screens = new();
	private readonly List<IWinFormsApplicationBlock> _registeredBlocks = new();
	private readonly IScreenActivationPolicyRegistry _activationPolicies = new ScreenActivationPolicyRegistry();
	private readonly WinFormsApplicationScreenTabControl _tabs;
	private readonly Panel _singleView;
	private ScreenMode _screenMode;
	private WinFormsApplicationScreen? _activeScreen;
	private bool _updating;
	private bool _disposing;

	public WinFormsApplicationScreenHost() {
		_tabs = new WinFormsApplicationScreenTabControl { Dock = DockStyle.Fill, Visible = false };
		_singleView = new Panel { Dock = DockStyle.Fill };
		Controls.Add(_tabs);
		Controls.Add(_singleView);
		_tabs.Selecting += TabSelecting;
		_tabs.ScreenCloseRequested += Screen => CloseScreen(Screen);
		_tabs.ScreenUndockRequested += Screen => UndockScreen(Screen);
		_tabs.DragEnter += TabDragEnter;
		_tabs.DragOver += TabDragEnter;
		_tabs.DragLeave += (_, _) => ClearDockPreview();
		_tabs.DragDrop += TabDragDrop;
		AllowDrop = true;
		DragEnter += TabDragEnter;
		DragOver += TabDragEnter;
		DragLeave += (_, _) => ClearDockPreview();
		DragDrop += TabDragDrop;
	}

	public override ScreenMode ScreenMode {
		get => _screenMode;
		set => Guard.Ensure(TrySetScreenMode(value), "A screen cancelled the screen mode change");
	}

	public override WinFormsApplicationScreen? ActiveScreen => _activeScreen;

	public override WinFormsApplicationScreen[] Screens => _screens.Keys.ToArray();

	public override WinFormsApplicationScreen[] OpenScreens => _screens
		.Where(pair => pair.Key.ScreenKind == ScreenKind.Normal && (pair.Value.IsOpen || pair.Key.ActivationMode == ScreenActivationMode.PermanentSingleton))
		.Select(pair => pair.Key).ToArray();

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public WinFormsApplicationScreenTabControl TabControl => _tabs;

	/// <summary>The tab header docking band in desktop coordinates, including a DPI-scaled proximity margin.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Rectangle DockTargetBounds {
		get {
			if (_screenMode != ScreenMode.MultiView || !Visible || IsDisposed || Disposing)
				return Rectangle.Empty;
			var Bounds = _tabs.TabStripBounds;
			if (Bounds.IsEmpty)
				return Rectangle.Empty;
			Bounds.Inflate(0, _tabs.LogicalToDeviceUnits(LogicalDockProximity));
			return _tabs.RectangleToScreen(Bounds);
		}
	}

	public override void RegisterScreenTypes(IWinFormsApplicationBlock Block) {
		Guard.ArgumentNotNull(Block, nameof(Block));
		Guard.Ensure(!_disposing, "The screen host is disposing");
		var blocks = _registeredBlocks.Contains(Block) ? _registeredBlocks.ToArray() : _registeredBlocks.Append(Block).ToArray();
		var definitions = Tools.UI.GetScreenDefinitions(blocks);
		var declarations = definitions.Where(definition => definition.ActivationMode.HasValue)
			.Select(definition => new KeyValuePair<Type, ScreenActivationMode>(definition.ScreenType, definition.ActivationMode.Value));
		foreach (var definition in definitions)
			Tools.UI.ValidateScreenType(definition.ScreenType, typeof(WinFormsApplicationScreen));
		_activationPolicies.RegisterDeclarations(declarations);
		if (!_registeredBlocks.Contains(Block))
			_registeredBlocks.Add(Block);
	}

	public override bool InitializeScreens(IEnumerable<IWinFormsApplicationBlock> blocks) => InitializeScreens(blocks, null);

	internal bool InitializeScreens(IEnumerable<IWinFormsApplicationBlock> blocks, Action registerBlocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var ordered = blocks.Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance).ToArray();
		var allBlocks = ordered.Concat(_registeredBlocks.Where(block => !ordered.Contains(block))).ToArray();
		var definitions = Tools.UI.GetScreenDefinitions(allBlocks);
		var replaceEmpty = _activeScreen?.ScreenKind == ScreenKind.Empty && definitions.Any(definition =>
			definition.ActivationMode == ScreenActivationMode.PermanentSingleton && !_screens.Keys.Any(screen => screen.GetType() == definition.ScreenType));
		if (replaceEmpty && !CanHide(_activeScreen))
			return false;
		// Block chrome and definitions are installed only after the current placeholder accepts the transition.
		registerBlocks?.Invoke();
		foreach (var block in ordered)
			RegisterScreenTypes(block);
		_registeredBlocks.Clear();
		_registeredBlocks.AddRange(allBlocks);
		var previous = _activeScreen;
		EnsurePermanentScreens(definitions);
		var startup = previous == null ? Tools.UI.GetDefaultScreen(definitions) : null;
		if (replaceEmpty)
			ShowScreenCore(_screens.Keys.First(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton), false);
		else if (startup != null)
			ActivateScreen((IWinFormsApplicationBlock)startup.Block, startup.ScreenType, startup.Title);
		else if (previous != null && !ReferenceEquals(previous, _activeScreen) && _screens.ContainsKey(previous))
			ShowScreen(previous);
		SelectRemainingScreen();
		return true;
	}

	private void EnsurePermanentScreens(ApplicationScreenDefinition[] definitions) {
		foreach (var definition in definitions.Where(definition => definition.ActivationMode == ScreenActivationMode.PermanentSingleton)) {
			if (_screens.Keys.Any(screen => screen.GetType() == definition.ScreenType))
				continue;
			var screen = CreateScreen((IWinFormsApplicationBlock)definition.Block, definition.ScreenType);
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
				if (!_screens.ContainsKey(screen))
					screen.Dispose();
			});
			screen.ApplicationBlock = (IWinFormsApplicationBlock)definition.Block;
			screen.ConfigureActivationMode(ScreenActivationMode.PermanentSingleton);
			screen.ScreenKind = definition.ScreenKind;
			screen.Title = definition.Title;
			var binding = RetainScreen(screen);
			if (_screenMode == ScreenMode.MultiView) {
				using var update = EnterUpdateScope();
				AddPresentation(screen, binding);
			}
		}
	}

	public override bool UnregisterScreenTypes(IWinFormsApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		var closing = _screens.Keys.Where(screen => ReferenceEquals(screen.ApplicationBlock, block)).ToArray();
		if (!CanCloseScreens(closing))
			return false;
		// Remove the definitions first so fallback cannot recreate a screen owned by the departing block.
		using var update = EnterUpdateScope();
		_registeredBlocks.Remove(block);
		CloseScreensCore(closing, false);
		var removedPermanentTypes = closing.Where(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton).Select(screen => screen.GetType()).ToHashSet();
		EnsurePermanentScreens(Tools.UI.GetScreenDefinitions(_registeredBlocks).Where(definition => removedPermanentTypes.Contains(definition.ScreenType)).ToArray());
		SelectRemainingScreen();
		return true;
	}

	public override WinFormsApplicationScreen? ActivateScreen(IWinFormsApplicationBlock Block, Type ScreenType, string? Title = null) {
		Guard.ArgumentNotNull(Block, nameof(Block));
		Guard.ArgumentNotNull(ScreenType, nameof(ScreenType));
		Tools.UI.ValidateScreenType(ScreenType, typeof(WinFormsApplicationScreen));
		RegisterScreenTypes(Block);
		var definition = Tools.UI.GetScreenDefinitions(_registeredBlocks).FirstOrDefault(item => item.ScreenType == ScreenType);
		if (definition?.ScreenKind == ScreenKind.Empty && OpenScreens.Length > 0)
			return null;
		var Existing = _screens.Keys.FirstOrDefault(Screen => Screen.GetType() == ScreenType && Tools.UI.IsSingleton(Screen.ActivationMode));
		if (Existing != null)
			return ShowScreen(Existing) ? Existing : null;

		var Created = CreateScreen(Block, ScreenType);
		using var Cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!_screens.ContainsKey(Created))
				Created.Dispose();
		});
		Created.ApplicationBlock = Block;
		Created.ScreenKind = definition?.ScreenKind ?? ScreenKind.Normal;
		if (!string.IsNullOrWhiteSpace(Title))
			Created.Title = Title;
		if (string.IsNullOrWhiteSpace(Created.Title))
			Created.Title = ScreenType.Name;
		return ShowScreen(Created) ? Created : null;
	}

	public override bool ShowScreen(WinFormsApplicationScreen Screen) => ShowScreenCore(Screen, true);

	private bool ShowScreenCore(WinFormsApplicationScreen Screen, bool checkHide) {
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		Guard.Argument(!Screen.IsDisposed, nameof(Screen), "Cannot show a disposed screen");
		Guard.Argument(Screen.ScreenHost == null || ReferenceEquals(Screen.ScreenHost, this), nameof(Screen), "Screen already belongs to another host");
		Guard.Ensure(!_disposing, "The screen host is disposing");
		if (Screen.ApplicationBlock != null)
			RegisterScreenTypes(Screen.ApplicationBlock);
		var ScreenType = Screen.GetType();
		if (Screen.ScreenHost == null && _activationPolicies.IsExplicitlyDeclared(ScreenType) && _activationPolicies.TryGetPolicy(ScreenType, out var activationMode))
			Screen.ConfigureActivationMode(activationMode);
		var definition = Tools.UI.GetScreenDefinitions(_registeredBlocks).FirstOrDefault(item => item.ScreenType == ScreenType);
		if (Screen.ScreenHost == null && definition != null)
			Screen.ScreenKind = definition.ScreenKind;
		Tools.UI.ValidateScreenPolicy(Screen.ActivationMode, Screen.ScreenKind);
		_activationPolicies.Validate(ScreenType, Screen.ActivationMode);
		if (Screen.ScreenKind == ScreenKind.Empty && OpenScreens.Length > 0)
			return false;
		Guard.Argument(!Tools.UI.IsSingleton(Screen.ActivationMode)
			|| !_screens.Keys.Any(Existing => Existing.GetType() == ScreenType && !ReferenceEquals(Existing, Screen)),
			nameof(Screen), "A single-instance screen of this type already exists; use ActivateScreen to select it");
		if (_screens.TryGetValue(Screen, out var Binding) && Binding.Window != null) {
			if (Binding.Window.WindowState == FormWindowState.Minimized)
				Binding.Window.WindowState = FormWindowState.Normal;
			Binding.Window.Activate();
			return true;
		}
		if (ReferenceEquals(Screen, _activeScreen))
			return true;
		if (checkHide && !CanHide(_activeScreen))
			return false;

		using var Update = EnterUpdateScope();
		var Previous = _activeScreen;
		ChangeActiveScreen(null);
		if (Previous != null && (_screenMode == ScreenMode.SingleView || Previous.ScreenKind == ScreenKind.Empty)) {
			RemovePresentation(Previous, _screens[Previous]);
			if (Previous.ActivationMode == ScreenActivationMode.MultiInstance)
				DestroyScreen(Previous);
		}
		if (Binding == null)
			Binding = RetainScreen(Screen);
		if (!Binding.IsOpen)
			AddPresentation(Screen, Binding);
		if (Binding.Tab != null)
			_tabs.SelectedTab = Binding.Tab;
		ChangeActiveScreen(Screen);
		Screen.NotifyShow();
		return true;
	}

	public override bool CloseScreen(WinFormsApplicationScreen Screen) => CloseScreens(new[] { Screen });

	public override bool CloseScreens(IEnumerable<WinFormsApplicationScreen> Screens) {
		Guard.ArgumentNotNull(Screens, nameof(Screens));
		var Closing = Screens.Distinct().ToArray();
		if (Closing.Any(screen => screen?.ActivationMode == ScreenActivationMode.PermanentSingleton) || !CanCloseScreens(Closing))
			return false;
		CloseScreensCore(Closing);
		return true;
	}

	private void CloseScreensCore(WinFormsApplicationScreen[] Closing, bool selectRemaining = true) {
		using var Update = EnterUpdateScope();
		if (_activeScreen != null && Closing.Contains(_activeScreen))
			ChangeActiveScreen(null);
		foreach (var Screen in Closing)
			DestroyScreen(Screen);
		if (selectRemaining)
			SelectRemainingScreen();
	}

	public override bool CanCloseScreens(IEnumerable<WinFormsApplicationScreen> Screens) {
		Guard.ArgumentNotNull(Screens, nameof(Screens));
		var Closing = Screens.Distinct().ToArray();
		foreach (var Screen in Closing) {
			Guard.ArgumentNotNull(Screen, nameof(Screens));
			Guard.Argument(_screens.ContainsKey(Screen), nameof(Screens), "Screen does not belong to this host");
		}
		// Validate the entire operation before removing any screen.
		return Closing.All(CanHide);
	}

	public override bool UndockScreen(WinFormsApplicationScreen Screen) {
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		Guard.Argument(_screens.ContainsKey(Screen), nameof(Screen), "Screen does not belong to this host");
		if (_screenMode != ScreenMode.MultiView || !_screens[Screen].IsOpen || Screen.ScreenKind == ScreenKind.Empty || Screen.ActivationMode == ScreenActivationMode.PermanentSingleton)
			return false;
		if (IsScreenUndocked(Screen))
			return true;
		if (!CanHide(Screen))
			return false;
		using var Update = EnterUpdateScope();
		if (ReferenceEquals(_activeScreen, Screen))
			ChangeActiveScreen(null);
		var Binding = _screens[Screen];
		RemovePresentation(Screen, Binding);
		Binding.Window = CreateScreenForm(Screen);
		Binding.Window.Disposed += ScreenFormDisposed;
		Binding.IsOpen = true;
		SelectRemainingScreen();
		var Owner = FindForm();
		if (Owner != null)
			Binding.Window.Show(Owner);
		else
			Binding.Window.Show();
		Screen.NotifyShow();
		return true;
	}

	public override bool DockScreen(WinFormsApplicationScreen Screen) {
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		Guard.Argument(_screens.ContainsKey(Screen), nameof(Screen), "Screen does not belong to this host");
		var Binding = _screens[Screen];
		if (Binding.Window == null)
			return ShowScreen(Screen);
		if (!CanHide(Screen) || !CanHide(_activeScreen))
			return false;
		using var Update = EnterUpdateScope();
		ChangeActiveScreen(null);
		RemovePresentation(Screen, Binding);
		AddPresentation(Screen, Binding);
		_tabs.SelectedTab = Binding.Tab;
		ChangeActiveScreen(Screen);
		Screen.NotifyShow();
		FindForm()?.Activate();
		return true;
	}

	public override bool IsScreenUndocked(WinFormsApplicationScreen Screen) => _screens.TryGetValue(Screen, out var Binding) && Binding.Window != null;

	/// <summary>Previews a drop near the tab headers. Window drags use the caption's vertical center, rather than the cursor's height.</summary>
	public bool UpdateDockPreview(WinFormsApplicationScreen Screen, Point ScreenLocation, Rectangle? DraggedCaptionBounds = null) {
		var DockLocation = ScreenLocation;
		if (DraggedCaptionBounds is { } Caption) {
			if (Caption.Width <= 0 || Caption.Height <= 0 || ScreenLocation.X < Caption.Left || ScreenLocation.X >= Caption.Right) {
				ClearDockPreview();
				return false;
			}
			DockLocation.Y = Caption.Top + Caption.Height / 2;
		}
		if (!IsScreenUndocked(Screen) || !DockTargetBounds.Contains(DockLocation)) {
			ClearDockPreview();
			return false;
		}
		_tabs.ShowDockPreview(Screen.Title, _tabs.PointToClient(DockLocation));
		return true;
	}

	public void ClearDockPreview() => _tabs.HideDockPreview();

	public bool CompleteScreenDock(WinFormsApplicationScreen Screen, Point ScreenLocation, Rectangle? DraggedCaptionBounds = null) {
		if (!UpdateDockPreview(Screen, ScreenLocation, DraggedCaptionBounds))
			return false;
		var Index = _tabs.DockPreviewIndex;
		ClearDockPreview();
		if (!DockScreen(Screen))
			return false;
		_tabs.MoveTab(_screens[Screen].Tab!, Math.Min(Index, _tabs.TabCount - 1));
		return true;
	}

	public override bool TrySetScreenMode(ScreenMode Mode) {
		Guard.Argument(Mode == ScreenMode.SingleView || Mode == ScreenMode.MultiView, nameof(Mode), "Unknown screen mode");
		if (Mode == _screenMode)
			return true;
		ClearDockPreview();
		var others = OpenScreens.Where(screen => !ReferenceEquals(screen, _activeScreen)).ToArray();
		if (Mode == ScreenMode.SingleView && !CanCloseScreens(others))
			return false;
		using var update = EnterUpdateScope();
		if (Mode == ScreenMode.SingleView) {
			foreach (var screen in others) {
				if (screen.ActivationMode == ScreenActivationMode.PermanentSingleton)
					RemovePresentation(screen, _screens[screen]);
				else
					DestroyScreen(screen);
			}
		}
		var active = _activeScreen;
		if (active != null)
			RemovePresentation(active, _screens[active]);
		_screenMode = Mode;
		if (Mode == ScreenMode.MultiView) {
			foreach (var screen in Screens.Where(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton && !ReferenceEquals(screen, active)))
				AddPresentation(screen, _screens[screen]);
		}
		if (active != null) {
			AddPresentation(active, _screens[active]);
			if (_screens[active].Tab != null)
				_tabs.SelectedTab = _screens[active].Tab;
		}
		UpdatePresentationVisibility();
		return true;
	}

	protected virtual WinFormsApplicationScreen CreateScreen(IWinFormsApplicationBlock Block, Type ScreenType) {
		var Owner = FindForm();
		if (Owner != null && TypeActivator.TryActivateWithCompatibleArgs(ScreenType, new object[] { Block, Owner }, out var Instance))
			return (WinFormsApplicationScreen)Instance;
		if (TypeActivator.TryActivateWithCompatibleArgs(ScreenType, new object[] { Block }, out Instance))
			return (WinFormsApplicationScreen)Instance;
		return TypeActivator.ActivateWithCompatibleArgs<WinFormsApplicationScreen>(ScreenType, Array.Empty<object>());
	}

	protected virtual WinFormsApplicationScreenForm CreateScreenForm(WinFormsApplicationScreen Screen) => new(this, Screen);

	protected override void Dispose(bool Disposing) {
		if (Disposing && !_disposing) {
			_disposing = true;
			ClearDockPreview();
			using var Update = EnterUpdateScope();
			ChangeActiveScreen(null);
			foreach (var Screen in Screens)
				DestroyScreen(Screen);
		}
		base.Dispose(Disposing);
	}

	private IDisposable EnterUpdateScope() {
		var WasUpdating = _updating;
		_updating = true;
		SuspendLayout();
		return Tools.Scope.ExecuteOnDispose(() => {
			_updating = WasUpdating;
			ResumeLayout(true);
		});
	}

	private static bool CanHide(WinFormsApplicationScreen? Screen) {
		var Cancel = false;
		Screen?.NotifyHideScreen(ref Cancel);
		return !Cancel;
	}

	private void ChangeActiveScreen(WinFormsApplicationScreen? Screen) {
		if (ReferenceEquals(Screen, _activeScreen))
			return;
		OnActiveScreenChanging(_activeScreen);
		_activeScreen = Screen;
		UpdatePresentationVisibility();
		OnActiveScreenChanged(Screen);
	}

	private ScreenBinding RetainScreen(WinFormsApplicationScreen screen) {
		_activationPolicies.RegisterInstance(screen.GetType(), screen.ActivationMode);
		var binding = new ScreenBinding();
		_screens.Add(screen, binding);
		screen.ScreenHost = this;
		screen.TextChanged += ScreenTextChanged;
		screen.ScreenDestroyed += ScreenDestroyed;
		return binding;
	}

	private void UpdatePresentationVisibility() {
		var tabless = _screenMode == ScreenMode.SingleView || _activeScreen?.ScreenKind == ScreenKind.Empty;
		_tabs.Visible = !tabless;
		_singleView.Visible = tabless;
		if (tabless)
			_singleView.BringToFront();
		else
			_tabs.BringToFront();
	}

	private void AddPresentation(WinFormsApplicationScreen Screen, ScreenBinding Binding) {
		Screen.Dock = DockStyle.Fill;
		if (_screenMode == ScreenMode.MultiView && Screen.ScreenKind == ScreenKind.Normal) {
			Binding.Tab = new TabPage(Screen.Title) { Tag = Screen, Padding = Padding.Empty };
			Binding.Tab.Controls.Add(Screen);
			_tabs.TabPages.Add(Binding.Tab);
		} else {
			_singleView.Controls.Add(Screen);
		}
		Binding.IsOpen = true;
	}

	private void RemovePresentation(WinFormsApplicationScreen Screen, ScreenBinding Binding) {
		Screen.Parent?.Controls.Remove(Screen);
		if (Binding.Tab != null) {
			_tabs.TabPages.Remove(Binding.Tab);
			Binding.Tab.Dispose();
			Binding.Tab = null;
		}
		if (Binding.Window != null) {
			var Window = Binding.Window;
			Binding.Window = null;
			Window.Disposed -= ScreenFormDisposed;
			Window.ReleaseScreen();
			Window.Dispose();
		}
		Binding.IsOpen = false;
	}

	private void DestroyScreen(WinFormsApplicationScreen Screen) {
		var Binding = _screens[Screen];
		Screen.TextChanged -= ScreenTextChanged;
		Screen.ScreenDestroyed -= ScreenDestroyed;
		RemovePresentation(Screen, Binding);
		_screens.Remove(Screen);
		Screen.ScreenHost = null;
		Screen.Dispose();
	}

	private void SelectRemainingScreen() {
		if (_activeScreen != null || _disposing)
			return;
		var next = _tabs.SelectedTab?.Tag as WinFormsApplicationScreen;
		next ??= _screens.FirstOrDefault(pair => pair.Value.IsOpen && pair.Value.Window == null && pair.Key.ScreenKind == ScreenKind.Normal).Key;
		next ??= _screens.Keys.FirstOrDefault(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton);
		if (next != null) {
			ShowScreen(next);
			return;
		}
		if (OpenScreens.Length > 0)
			return;
		var empty = Tools.UI.GetEmptyScreen(Tools.UI.GetScreenDefinitions(_registeredBlocks));
		if (empty != null)
			ActivateScreen((IWinFormsApplicationBlock)empty.Block, empty.ScreenType, empty.Title);
	}

	private void TabSelecting(object? Sender, TabControlCancelEventArgs Args) {
		if (!_updating && !_tabs.Reordering && Args.TabPage?.Tag is WinFormsApplicationScreen Screen)
			Args.Cancel = !ShowScreen(Screen);
	}

	private void ScreenTextChanged(object? Sender, EventArgs Args) {
		if (Sender is WinFormsApplicationScreen Screen && _screens.TryGetValue(Screen, out var Binding) && Binding.Tab != null)
			Binding.Tab.Text = Screen.Title;
	}

	private void ScreenDestroyed(object? Sender, EventArgs Args) {
		if (Sender is not WinFormsApplicationScreen Screen || !_screens.ContainsKey(Screen))
			return;
		using var Update = EnterUpdateScope();
		if (ReferenceEquals(_activeScreen, Screen))
			ChangeActiveScreen(null);
		Screen.TextChanged -= ScreenTextChanged;
		Screen.ScreenDestroyed -= ScreenDestroyed;
		RemovePresentation(Screen, _screens[Screen]);
		_screens.Remove(Screen);
		Screen.ScreenHost = null;
		SelectRemainingScreen();
	}

	private void ScreenFormDisposed(object? Sender, EventArgs Args) {
		if (Sender is not WinFormsApplicationScreenForm Window || !_screens.TryGetValue(Window.Screen, out var Binding) || !ReferenceEquals(Binding.Window, Window))
			return;
		using var Update = EnterUpdateScope();
		Binding.Window = null;
		DestroyScreen(Window.Screen);
		SelectRemainingScreen();
	}

	private void TabDragEnter(object? Sender, DragEventArgs Args) {
		if ((Args.AllowedEffect & DragDropEffects.Move) != 0 && Args.Data?.GetData(typeof(WinFormsApplicationScreen)) is WinFormsApplicationScreen Screen
			&& UpdateDockPreview(Screen, new Point(Args.X, Args.Y))) {
			Args.Effect = DragDropEffects.Move;
			return;
		}
		Args.Effect = DragDropEffects.None;
		ClearDockPreview();
	}

	private void TabDragDrop(object? Sender, DragEventArgs Args) {
		if (Args.Data?.GetData(typeof(WinFormsApplicationScreen)) is WinFormsApplicationScreen Screen)
			Args.Effect = CompleteScreenDock(Screen, new Point(Args.X, Args.Y)) ? DragDropEffects.Move : DragDropEffects.None;
		ClearDockPreview();
	}

	private sealed class ScreenBinding {
		public bool IsOpen { get; set; }
		public TabPage? Tab { get; set; }
		public WinFormsApplicationScreenForm? Window { get; set; }
	}
}
