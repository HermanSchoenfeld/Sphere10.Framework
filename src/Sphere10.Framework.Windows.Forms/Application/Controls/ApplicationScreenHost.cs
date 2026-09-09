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

namespace Sphere10.Framework.Windows.Forms;

public class ApplicationScreenHost : ApplicationScreenHostBase {
	private const int LogicalDockProximity = 8;
	private readonly Dictionary<ApplicationScreen, ScreenBinding> _screens = new();
	private readonly Dictionary<Type, ScreenActivationMode> _activationModes = new();
	private readonly HashSet<Type> _declaredScreenTypes = new();
	private readonly ApplicationScreenTabControl _tabs;
	private readonly Panel _singleView;
	private ScreenMode _screenMode;
	private ApplicationScreen? _activeScreen;
	private bool _updating;
	private bool _disposing;

	public ApplicationScreenHost() {
		_tabs = new ApplicationScreenTabControl { Dock = DockStyle.Fill, Visible = false };
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

	public override ApplicationScreen? ActiveScreen => _activeScreen;

	public override IReadOnlyCollection<ApplicationScreen> Screens => _screens.Keys.ToArray();

	public override IReadOnlyCollection<ApplicationScreen> OpenScreens => _screens.Where(Pair => Pair.Value.IsOpen).Select(Pair => Pair.Key).ToArray();

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ApplicationScreenTabControl TabControl => _tabs;

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

	public override void RegisterScreenTypes(IApplicationBlock Block) {
		Guard.ArgumentNotNull(Block, nameof(Block));
		Guard.Ensure(!_disposing, "The screen host is disposing");
		var Declarations = new Dictionary<Type, ScreenActivationMode>();
		foreach (var Item in Block.Menus.SelectMany(Menu => Menu.Items).OfType<IScreenMenuItem>()) {
			if (!Item.ActivationMode.HasValue)
				continue;
			var ScreenType = Item.Screen;
			var Mode = Item.ActivationMode.Value;
			Guard.Argument(ScreenType != null && typeof(ApplicationScreen).IsAssignableFrom(ScreenType) && !ScreenType.IsAbstract,
				nameof(Block), "A concrete ApplicationScreen type is required");
			Guard.Argument(Mode == ScreenActivationMode.SingleInstance || Mode == ScreenActivationMode.MultiInstance, nameof(Block), "Unknown activation mode");
			Guard.Argument(!Declarations.TryGetValue(ScreenType, out var DeclaredMode) || DeclaredMode == Mode,
				nameof(Block), $"Menu entries for {ScreenType.Name} declare conflicting activation modes");
			Guard.Argument(!_activationModes.TryGetValue(ScreenType, out var RegisteredMode) || RegisteredMode == Mode,
				nameof(Block), $"The activation mode for {ScreenType.Name} is already registered and cannot change");
			Declarations[ScreenType] = Mode;
		}
		// Validate the complete block before installing any declaration, including types not yet instantiated.
		foreach (var Declaration in Declarations) {
			_activationModes[Declaration.Key] = Declaration.Value;
			_declaredScreenTypes.Add(Declaration.Key);
		}
	}

	public override ApplicationScreen? ActivateScreen(IApplicationBlock Block, Type ScreenType, string? Title = null) {
		Guard.ArgumentNotNull(Block, nameof(Block));
		Guard.ArgumentNotNull(ScreenType, nameof(ScreenType));
		Guard.Argument(typeof(ApplicationScreen).IsAssignableFrom(ScreenType) && !ScreenType.IsAbstract, nameof(ScreenType), "A concrete ApplicationScreen type is required");
		RegisterScreenTypes(Block);
		var Existing = _screens.Keys.FirstOrDefault(Screen => Screen.GetType() == ScreenType && Screen.ActivationMode == ScreenActivationMode.SingleInstance);
		if (Existing != null)
			return ShowScreen(Existing) ? Existing : null;

		var Created = CreateScreen(Block, ScreenType);
		using var Cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!_screens.ContainsKey(Created))
				Created.Dispose();
		});
		Created.ApplicationBlock = Block;
		if (!string.IsNullOrWhiteSpace(Title))
			Created.Title = Title;
		if (string.IsNullOrWhiteSpace(Created.Title))
			Created.Title = ScreenType.Name;
		return ShowScreen(Created) ? Created : null;
	}

	public override bool ShowScreen(ApplicationScreen Screen) {
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		Guard.Argument(!Screen.IsDisposed, nameof(Screen), "Cannot show a disposed screen");
		Guard.Argument(Screen.ScreenHost == null || ReferenceEquals(Screen.ScreenHost, this), nameof(Screen), "Screen already belongs to another host");
		Guard.Ensure(!_disposing, "The screen host is disposing");
		if (Screen.ApplicationBlock != null)
			RegisterScreenTypes(Screen.ApplicationBlock);
		var ScreenType = Screen.GetType();
		if (Screen.ScreenHost == null && _declaredScreenTypes.Contains(ScreenType))
			Screen.ConfigureActivationMode(_activationModes[ScreenType]);
		Guard.Argument(!_activationModes.TryGetValue(ScreenType, out var ActivationMode) || ActivationMode == Screen.ActivationMode,
			nameof(Screen), "All instances of a screen type must declare the same activation mode");
		Guard.Argument(Screen.ActivationMode != ScreenActivationMode.SingleInstance
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
		if (!CanHide(_activeScreen))
			return false;

		using var Update = EnterUpdateScope();
		var Previous = _activeScreen;
		ChangeActiveScreen(null);
		if (_screenMode == ScreenMode.SingleView && Previous != null) {
			RemovePresentation(Previous, _screens[Previous]);
			if (Previous.ActivationMode == ScreenActivationMode.MultiInstance)
				DestroyScreen(Previous);
		}
		if (Binding == null) {
			Binding = new ScreenBinding();
			_screens.Add(Screen, Binding);
			_activationModes[ScreenType] = Screen.ActivationMode;
			Screen.ScreenHost = this;
			Screen.TextChanged += ScreenTextChanged;
			Screen.ScreenDestroyed += ScreenDestroyed;
		}
		if (!Binding.IsOpen)
			AddPresentation(Screen, Binding);
		if (Binding.Tab != null)
			_tabs.SelectedTab = Binding.Tab;
		ChangeActiveScreen(Screen);
		Screen.NotifyShow();
		return true;
	}

	public override bool CloseScreen(ApplicationScreen Screen) => CloseScreens(new[] { Screen });

	public override bool CloseScreens(IEnumerable<ApplicationScreen> Screens) {
		Guard.ArgumentNotNull(Screens, nameof(Screens));
		var Closing = Screens.Distinct().ToArray();
		if (!CanCloseScreens(Closing))
			return false;
		using var Update = EnterUpdateScope();
		if (_activeScreen != null && Closing.Contains(_activeScreen))
			ChangeActiveScreen(null);
		foreach (var Screen in Closing)
			DestroyScreen(Screen);
		SelectRemainingScreen();
		return true;
	}

	public override bool CanCloseScreens(IEnumerable<ApplicationScreen> Screens) {
		Guard.ArgumentNotNull(Screens, nameof(Screens));
		var Closing = Screens.Distinct().ToArray();
		foreach (var Screen in Closing) {
			Guard.ArgumentNotNull(Screen, nameof(Screens));
			Guard.Argument(_screens.ContainsKey(Screen), nameof(Screens), "Screen does not belong to this host");
		}
		// Validate the entire operation before removing any screen.
		return Closing.All(CanHide);
	}

	public override bool UndockScreen(ApplicationScreen Screen) {
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		Guard.Argument(_screens.ContainsKey(Screen), nameof(Screen), "Screen does not belong to this host");
		if (_screenMode != ScreenMode.MultiView || !_screens[Screen].IsOpen)
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

	public override bool DockScreen(ApplicationScreen Screen) {
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

	public override bool IsScreenUndocked(ApplicationScreen Screen) => _screens.TryGetValue(Screen, out var Binding) && Binding.Window != null;

	/// <summary>Previews a drop near the tab headers. Window drags use the caption's vertical center, rather than the cursor's height.</summary>
	public bool UpdateDockPreview(ApplicationScreen Screen, Point ScreenLocation, Rectangle? DraggedCaptionBounds = null) {
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

	public bool CompleteScreenDock(ApplicationScreen Screen, Point ScreenLocation, Rectangle? DraggedCaptionBounds = null) {
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
		using var Update = EnterUpdateScope();
		// Returning to SingleView keeps the selected tab and closes all other open views.
		if (Mode == ScreenMode.SingleView && !CloseScreens(OpenScreens.Where(Screen => !ReferenceEquals(Screen, _activeScreen))))
			return false;
		var Active = _activeScreen;
		if (Active != null)
			RemovePresentation(Active, _screens[Active]);
		_screenMode = Mode;
		_tabs.Visible = Mode == ScreenMode.MultiView;
		_singleView.Visible = Mode == ScreenMode.SingleView;
		if (Active != null) {
			AddPresentation(Active, _screens[Active]);
			if (_screens[Active].Tab != null)
				_tabs.SelectedTab = _screens[Active].Tab;
		}
		return true;
	}

	protected virtual ApplicationScreen CreateScreen(IApplicationBlock Block, Type ScreenType) {
		var Owner = FindForm();
		if (Owner != null && TypeActivator.TryActivateWithCompatibleArgs(ScreenType, new object[] { Block, Owner }, out var Instance))
			return (ApplicationScreen)Instance;
		if (TypeActivator.TryActivateWithCompatibleArgs(ScreenType, new object[] { Block }, out Instance))
			return (ApplicationScreen)Instance;
		return TypeActivator.ActivateWithCompatibleArgs<ApplicationScreen>(ScreenType, Array.Empty<object>());
	}

	protected virtual ApplicationScreenForm CreateScreenForm(ApplicationScreen Screen) => new(this, Screen);

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

	private static bool CanHide(ApplicationScreen? Screen) {
		var Cancel = false;
		Screen?.NotifyHideScreen(ref Cancel);
		return !Cancel;
	}

	private void ChangeActiveScreen(ApplicationScreen? Screen) {
		if (ReferenceEquals(Screen, _activeScreen))
			return;
		OnActiveScreenChanging(_activeScreen);
		_activeScreen = Screen;
		OnActiveScreenChanged(Screen);
	}

	private void AddPresentation(ApplicationScreen Screen, ScreenBinding Binding) {
		Screen.Dock = DockStyle.Fill;
		if (_screenMode == ScreenMode.MultiView) {
			Binding.Tab = new TabPage(Screen.Title) { Tag = Screen, Padding = Padding.Empty };
			Binding.Tab.Controls.Add(Screen);
			_tabs.TabPages.Add(Binding.Tab);
		} else {
			_singleView.Controls.Add(Screen);
		}
		Binding.IsOpen = true;
	}

	private void RemovePresentation(ApplicationScreen Screen, ScreenBinding Binding) {
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

	private void DestroyScreen(ApplicationScreen Screen) {
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
		var Next = _tabs.SelectedTab?.Tag as ApplicationScreen;
		Next ??= _screens.FirstOrDefault(Pair => Pair.Value.IsOpen && Pair.Value.Window == null).Key;
		if (Next != null) {
			ChangeActiveScreen(Next);
			Next.NotifyShow();
		}
	}

	private void TabSelecting(object? Sender, TabControlCancelEventArgs Args) {
		if (!_updating && !_tabs.Reordering && Args.TabPage?.Tag is ApplicationScreen Screen)
			Args.Cancel = !ShowScreen(Screen);
	}

	private void ScreenTextChanged(object? Sender, EventArgs Args) {
		if (Sender is ApplicationScreen Screen && _screens.TryGetValue(Screen, out var Binding) && Binding.Tab != null)
			Binding.Tab.Text = Screen.Title;
	}

	private void ScreenDestroyed(object? Sender, EventArgs Args) {
		if (Sender is not ApplicationScreen Screen || !_screens.ContainsKey(Screen))
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
		if (Sender is not ApplicationScreenForm Window || !_screens.TryGetValue(Window.Screen, out var Binding) || !ReferenceEquals(Binding.Window, Window))
			return;
		using var Update = EnterUpdateScope();
		Binding.Window = null;
		DestroyScreen(Window.Screen);
		SelectRemainingScreen();
	}

	private void TabDragEnter(object? Sender, DragEventArgs Args) {
		if ((Args.AllowedEffect & DragDropEffects.Move) != 0 && Args.Data?.GetData(typeof(ApplicationScreen)) is ApplicationScreen Screen
			&& UpdateDockPreview(Screen, new Point(Args.X, Args.Y))) {
			Args.Effect = DragDropEffects.Move;
			return;
		}
		Args.Effect = DragDropEffects.None;
		ClearDockPreview();
	}

	private void TabDragDrop(object? Sender, DragEventArgs Args) {
		if (Args.Data?.GetData(typeof(ApplicationScreen)) is ApplicationScreen Screen)
			Args.Effect = CompleteScreenDock(Screen, new Point(Args.X, Args.Y)) ? DragDropEffects.Move : DragDropEffects.None;
		ClearDockPreview();
	}

	private sealed class ScreenBinding {
		public bool IsOpen { get; set; }
		public TabPage? Tab { get; set; }
		public ApplicationScreenForm? Window { get; set; }
	}
}
