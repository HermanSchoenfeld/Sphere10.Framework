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
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationScreenHost : WinFormsApplicationScreenHostBase {
	private const int LogicalDockProximity = 8;
	private readonly Dictionary<WinFormsApplicationScreen, ScreenBinding> _screens = new(ReferenceEqualityComparer.Instance);
	private readonly NativeScreenHostEngine _engine;
	private readonly WinFormsApplicationScreenTabControl _tabs;
	private readonly ImageList _tabImages;
	private readonly Panel _singleView;
	private bool _updating;
	private int _updateDepth;
	private bool _activeSelectionChanged;
	private bool _disposing;

	public WinFormsApplicationScreenHost() {
		_engine = new NativeScreenHostEngine(this);
		_tabImages = new ImageList { ImageSize = new Size(16, 16), ColorDepth = ColorDepth.Depth32Bit };
		_tabs = new WinFormsApplicationScreenTabControl { Dock = DockStyle.Fill, Visible = false, ImageList = _tabImages };
		_singleView = new Panel { Dock = DockStyle.Fill };
		Controls.Add(_tabs);
		Controls.Add(_singleView);
		_tabs.Selecting += TabSelecting;
		_tabs.ScreenCloseRequested += Screen => CloseScreen(Screen);
		_tabs.ScreenUndockRequested += Screen => UndockScreen(Screen);
		_tabs.ScreenCloseAllRequested += CloseAllRequested;
		_tabs.ScreenCloseOthersRequested += CloseOthersRequested;
		_tabs.TabOrderChanged += SynchronizeTabOrder;
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
		get => _engine.ScreenMode;
		set => Guard.Ensure(TrySetScreenMode(value), "A screen cancelled the screen mode change");
	}

	public override WinFormsApplicationScreen ActiveScreen => _engine.ActiveScreen;

	public override WinFormsApplicationScreen[] Screens => _engine.Screens;

	public override WinFormsApplicationScreen[] OpenScreens => _engine.OpenScreens;

	public override IWinFormsApplicationBlock[] AvailableBlocks => _engine.AvailableBlocks;

	public override IWinFormsApplicationBlock[] Blocks => _engine.Blocks;

	public override IWinFormsApplicationBlock ActiveBlock => _engine.ActiveBlock;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override IUserInterfaceServices UserInterfaceServices => FindForm() as IUserInterfaceServices;

	protected override ApplicationScreenHostBase<IWinFormsApplicationBlock, WinFormsApplicationScreen> ApplicationHost => _engine;

	internal IWinFormsApplicationBlock[] StartupBlocks => _engine.RegistrationBlocks;

	internal IDisposable EnterPresentationScope() => EnterUpdateScope();

	public override void SelectBlock(IWinFormsApplicationBlock block) {
		Guard.Ensure(!_disposing, "The screen host is disposing.");
		Guard.Argument(block == null || _engine.RegistrationBlocks.Contains(block, ReferenceEqualityComparer.Instance), nameof(block), "Block is not registered.");
		_engine.SelectBlock(block);
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public WinFormsApplicationScreenTabControl TabControl => _tabs;

	/// <summary>The tab header docking band in desktop coordinates, including a DPI-scaled proximity margin.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Rectangle DockTargetBounds {
		get {
			if (ScreenMode != ScreenMode.MultiView || !Visible || IsDisposed || Disposing)
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
		using var update = EnterUpdateScope();
		var blocks = _engine.RegistrationBlocks.Contains(Block, ReferenceEqualityComparer.Instance) ? _engine.RegistrationBlocks : _engine.RegistrationBlocks.Append(Block).ToArray();
		var definitions = Tools.UI.GetScreenDefinitions(blocks);
		foreach (var definition in definitions)
			Tools.UI.ValidateScreenType(definition.ScreenType, typeof(WinFormsApplicationScreen));
		_engine.RegisterDefinitions(definitions);
		_engine.RegisterBlock(Block);
	}

	public override bool InitializeScreens(IEnumerable<IWinFormsApplicationBlock> blocks) => InitializeScreens(blocks, null);

	internal bool InitializeScreens(IEnumerable<IWinFormsApplicationBlock> blocks, Action registerBlocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var ordered = blocks.Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance).ToArray();
		var allBlocks = ordered.Concat(_engine.RegistrationBlocks.Where(block => !ordered.Contains(block, ReferenceEqualityComparer.Instance))).ToArray();
		var definitions = Tools.UI.GetScreenDefinitions(allBlocks);
		var replaceEmpty = ActiveScreen?.ScreenKind == ScreenKind.Empty && definitions.Any(definition =>
			IsPermanentDefinition(definition) && !Screens.Any(screen => screen.GetType() == definition.ScreenType));
		if (replaceEmpty && !CanHide(ActiveScreen))
			return false;
		using var update = EnterUpdateScope();
		// Block chrome and definitions are installed only after the current placeholder accepts the transition.
		registerBlocks?.Invoke();
		foreach (var block in ordered)
			RegisterScreenTypes(block);
		_engine.ReorderBlocks(allBlocks);
		_engine.InitializeState();
		var previous = ActiveScreen;
		EnsurePermanentScreens(definitions);
		var startup = previous == null ? Tools.UI.GetDefaultScreen(allBlocks) ?? (allBlocks.FirstOrDefault() is { } first ? _engine.GetBlockDefault(first) : null) : null;
		if (replaceEmpty)
			ShowScreenCore(Screens.First(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton), false);
		else if (startup != null)
			ActivateScreenCore(_engine.GetDefinitionOwner(startup), startup.ScreenType, startup.Title, requestedDefinition: startup);
		else if (previous != null && !ReferenceEquals(previous, ActiveScreen) && ContainsScreen(previous))
			ShowScreen(previous);
		SelectRemainingScreen();
		return true;
	}

	private bool IsPermanentDefinition(ApplicationScreenDefinition definition) => definition.ActivationMode == ScreenActivationMode.PermanentSingleton
		|| !definition.ActivationMode.HasValue && _engine.GetActivationDefinition(definition.ScreenType)?.ActivationMode == ScreenActivationMode.PermanentSingleton;

	private void EnsurePermanentScreens(ApplicationScreenDefinition[] definitions) {
		foreach (var definition in definitions.Where(IsPermanentDefinition)) {
			if (Screens.Any(screen => screen.GetType() == definition.ScreenType))
				continue;
			var block = _engine.GetDefinitionOwner(definition);
			var screen = CreateScreen(block, definition.ScreenType);
			using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
				if (!ContainsScreen(screen))
					screen.Dispose();
			});
			screen.Block = block;
			screen.ConfigureActivationMode(ScreenActivationMode.PermanentSingleton);
			screen.ScreenKind = definition.ScreenKind;
			screen.Title = definition.Title;
			var binding = RetainScreen(screen, definition);
			if (ScreenMode == ScreenMode.MultiView) {
				using var update = EnterUpdateScope();
				AddPresentation(screen, binding);
			}
		}
	}

	public override bool UnregisterScreenTypes(IWinFormsApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		if (!UnregisterScreensCore(block))
			return false;
		return true;
	}

	private bool UnregisterScreensCore(IApplicationBlock block) {
		var closing = Screens.Where(screen => ReferenceEquals(screen.Block, block)).ToArray();
		if (!CanCloseScreens(closing))
			return false;
		// Remove the definitions first so fallback cannot recreate a screen owned by the departing block.
		using var update = EnterUpdateScope();
		_engine.UnregisterBlock((IWinFormsApplicationBlock)block);
		CloseScreensCore(closing, false);
		var removedPermanentTypes = closing.Where(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton).Select(screen => screen.GetType()).ToHashSet();
		EnsurePermanentScreens(_engine.Definitions.Where(definition => removedPermanentTypes.Contains(definition.ScreenType)).ToArray());
		SelectRemainingScreen();
		return true;
	}

	public override WinFormsApplicationScreen ActivateScreen(IWinFormsApplicationBlock block, Type screenType, string title = null)
		=> ActivateScreenCore(block, screenType, title);

	public override WinFormsApplicationScreen ActivateScreen(IWinFormsApplicationBlock block, ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNull(definition, nameof(definition));
		Guard.Argument(block.ScreenDefinitions.Contains(definition, ReferenceEqualityComparer.Instance), nameof(definition), "The screen definition is not owned by this block.");
		return ActivateScreenCore(block, definition.ScreenType, definition.Title, definition);
	}

	private WinFormsApplicationScreen ActivateScreenCore(IWinFormsApplicationBlock Block, Type ScreenType, string Title,
		ApplicationScreenDefinition requestedDefinition = null, bool registerBlock = true) {
		Guard.ArgumentNotNull(Block, nameof(Block));
		Guard.ArgumentNotNull(ScreenType, nameof(ScreenType));
		Tools.UI.ValidateScreenType(ScreenType, typeof(WinFormsApplicationScreen));
		using var update = EnterUpdateScope();
		if (registerBlock)
			RegisterScreenTypes(Block);
		var definition = requestedDefinition ?? Block.ScreenDefinitions.FirstOrDefault(item => item.ScreenType == ScreenType);
		if (definition?.ScreenKind == ScreenKind.Empty && OpenScreens.Length > 0)
			return null;
		var Existing = Screens.FirstOrDefault(Screen => Screen.GetType() == ScreenType && Tools.UI.IsSingleton(Screen.ActivationMode));
		if (Existing != null)
			return ShowScreen(Existing) ? Existing : null;

		var Created = CreateScreen(Block, ScreenType);
		using var Cleanup = Tools.Scope.ExecuteOnDispose(() => {
			if (!ContainsScreen(Created))
				Created.Dispose();
		});
		Created.Block = Block;
		Created.ScreenKind = definition?.ScreenKind ?? ScreenKind.Normal;
		if (!string.IsNullOrWhiteSpace(Title))
			Created.Title = Title;
		if (string.IsNullOrWhiteSpace(Created.Title))
			Created.Title = ScreenType.Name;
		return ShowScreenCore(Created, true, definition, registerBlock) ? Created : null;
	}

	public override bool ShowScreen(WinFormsApplicationScreen Screen) => ShowScreenCore(Screen, true);

	public override bool ShowScreenUndocked(WinFormsApplicationScreen screen) => ShowScreenCore(screen, false, undocked: true);

	private bool ShowScreenCore(WinFormsApplicationScreen Screen, bool checkHide, ApplicationScreenDefinition requestedDefinition = null,
		bool registerBlock = true, bool selectTab = true, bool undocked = false) {
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		Guard.Argument(!Screen.IsDisposed, nameof(Screen), "Cannot show a disposed screen");
		Guard.Argument(Screen.Host == null || ReferenceEquals(Screen.Host, this), nameof(Screen), "Screen already belongs to another host");
		Guard.Ensure(!_disposing, "The screen host is disposing");
		using var update = EnterUpdateScope(repaintImmediately: selectTab);
		if (registerBlock && Screen.Host == null && Screen.Block != null)
			RegisterScreenTypes(Screen.Block);
		var ScreenType = Screen.GetType();
		var activationDefinition = _engine.Definitions.FirstOrDefault(item => item.ScreenType == ScreenType && item.ActivationMode.HasValue);
		if (Screen.Host == null && activationDefinition != null)
			Screen.ConfigureActivationMode(activationDefinition.ActivationMode.Value);
		var definition = requestedDefinition ?? Screen.Block?.ScreenDefinitions.FirstOrDefault(item => item.ScreenType == ScreenType);
		if (Screen.Host == null && definition != null)
			Screen.ScreenKind = definition.ScreenKind;
		Tools.UI.ValidateScreenPolicy(Screen.ActivationMode, Screen.ScreenKind);
		_engine.ValidateActivation(ScreenType, Screen.ActivationMode);
		if (undocked && (ScreenMode != ScreenMode.MultiView || Screen.ScreenKind != ScreenKind.Normal || Screen.ActivationMode == ScreenActivationMode.PermanentSingleton))
			return false;
		if (Screen.ScreenKind == ScreenKind.Empty && OpenScreens.Length > 0)
			return false;
		Guard.Argument(!Tools.UI.IsSingleton(Screen.ActivationMode)
			|| !Screens.Any(Existing => Existing.GetType() == ScreenType && !ReferenceEquals(Existing, Screen)),
			nameof(Screen), "A single-instance screen of this type already exists; use ActivateScreen to select it");
		if (TryGetBinding(Screen, out var Binding) && Binding.Window != null) {
			if (Binding.Window.WindowState == FormWindowState.Minimized)
				Binding.Window.WindowState = FormWindowState.Normal;
			Binding.Window.Activate();
			return true;
		}
		if (undocked) {
			if (Binding != null && IsPresented(Screen, Binding))
				return UndockScreen(Screen);
			Binding ??= RetainScreen(Screen, requestedDefinition);
			Binding.Window = CreateScreenForm(Screen);
			Binding.Window.Disposed += ScreenFormDisposed;
			_engine.SetOpen(Screen, true);
			if (FindForm() is { } owner)
				Binding.Window.Show(owner);
			else
				Binding.Window.Show();
			Screen.NotifyShow();
			return true;
		}
		if (ReferenceEquals(Screen, ActiveScreen)) {
			_engine.SelectScreen(Screen);
			return true;
		}
		if (checkHide && !CanHide(ActiveScreen))
			return false;

		var Previous = ActiveScreen;
		ChangeActiveScreen(null);
		if (Previous != null && (ScreenMode == ScreenMode.SingleView || Previous.ScreenKind == ScreenKind.Empty)) {
			RemovePresentation(Previous, GetBinding(Previous));
			if (Previous.ActivationMode == ScreenActivationMode.MultiInstance)
				DestroyScreen(Previous);
		}
		if (Binding == null)
			Binding = RetainScreen(Screen, requestedDefinition);
		if (!IsPresented(Screen, Binding))
			AddPresentation(Screen, Binding);
		if (selectTab && Binding.Tab != null)
			_tabs.SelectedTab = Binding.Tab;
		ChangeActiveScreen(Screen);
		Screen.NotifyShow();
		return true;
	}

	public override bool CloseScreen(WinFormsApplicationScreen Screen) => CloseScreens(new[] { Screen });

	private async void CloseAllRequested() {
		try {
			await CloseAllScreensAsync();
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	private async void CloseOthersRequested(WinFormsApplicationScreen screen) {
		try {
			await CloseAllOtherScreensAsync(screen);
		} catch (Exception error) {
			await ExceptionDialog.ShowAsync(this, error);
		}
	}

	public override bool CloseScreens(IEnumerable<WinFormsApplicationScreen> Screens) {
		Guard.ArgumentNotNull(Screens, nameof(Screens));
		var Closing = Screens.Distinct<WinFormsApplicationScreen>(ReferenceEqualityComparer.Instance).ToArray();
		if (Closing.Any(screen => screen?.ActivationMode == ScreenActivationMode.PermanentSingleton) || !CanCloseScreens(Closing))
			return false;
		CloseScreensCore(Closing);
		return true;
	}

	private void CloseScreensCore(WinFormsApplicationScreen[] Closing, bool selectRemaining = true) {
		using var Update = EnterUpdateScope();
		if (ActiveScreen != null && Closing.Contains(ActiveScreen, ReferenceEqualityComparer.Instance))
			ChangeActiveScreen(null);
		foreach (var Screen in Closing)
			DestroyScreen(Screen);
		if (selectRemaining)
			SelectRemainingScreen();
	}

	public override bool CanCloseScreens(IEnumerable<WinFormsApplicationScreen> Screens) {
		Guard.ArgumentNotNull(Screens, nameof(Screens));
		var Closing = Screens.Distinct<WinFormsApplicationScreen>(ReferenceEqualityComparer.Instance).ToArray();
		foreach (var Screen in Closing) {
			Guard.ArgumentNotNull(Screen, nameof(Screens));
			Guard.Argument(ContainsScreen(Screen), nameof(Screens), "Screen does not belong to this host");
		}
		// Validate the entire operation before removing any screen.
		return Closing.All(CanHide);
	}

	public override bool UndockScreen(WinFormsApplicationScreen Screen) {
		Guard.ArgumentNotNull(Screen, nameof(Screen));
		Guard.Argument(ContainsScreen(Screen), nameof(Screen), "Screen does not belong to this host");
		if (ScreenMode != ScreenMode.MultiView || !IsPresented(Screen, GetBinding(Screen)) || Screen.ScreenKind == ScreenKind.Empty || Screen.ActivationMode == ScreenActivationMode.PermanentSingleton)
			return false;
		if (IsScreenUndocked(Screen))
			return true;
		if (!CanHide(Screen))
			return false;
		using var Update = EnterUpdateScope();
		if (ReferenceEquals(ActiveScreen, Screen))
			ChangeActiveScreen(null);
		var Binding = GetBinding(Screen);
		RemovePresentation(Screen, Binding);
		Binding.Window = CreateScreenForm(Screen);
		Binding.Window.Disposed += ScreenFormDisposed;
		_engine.SetOpen(Screen, true);
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
		Guard.Argument(ContainsScreen(Screen), nameof(Screen), "Screen does not belong to this host");
		var Binding = GetBinding(Screen);
		if (Binding.Window == null)
			return ShowScreen(Screen);
		if (!CanHide(Screen) || !CanHide(ActiveScreen))
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

	public override bool IsScreenUndocked(WinFormsApplicationScreen Screen) => TryGetBinding(Screen, out var Binding) && Binding.Window != null;

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
		using var update = EnterUpdateScope();
		ClearDockPreview();
		if (!DockScreen(Screen))
			return false;
		_tabs.MoveTab(GetBinding(Screen).Tab!, Math.Min(Index, _tabs.TabCount - 1));
		return true;
	}

	public override bool TrySetScreenMode(ScreenMode Mode) {
		Guard.Argument(Mode == ScreenMode.SingleView || Mode == ScreenMode.MultiView, nameof(Mode), "Unknown screen mode");
		if (Mode == ScreenMode)
			return true;
		ClearDockPreview();
		var others = OpenScreens.Where(screen => !ReferenceEquals(screen, ActiveScreen)).ToArray();
		if (Mode == ScreenMode.SingleView && !CanCloseScreens(others))
			return false;
		using var update = EnterUpdateScope();
		if (Mode == ScreenMode.SingleView) {
			foreach (var screen in others) {
				if (screen.ActivationMode == ScreenActivationMode.PermanentSingleton)
					RemovePresentation(screen, GetBinding(screen));
				else
					DestroyScreen(screen);
			}
		}
		var active = ActiveScreen;
		if (active != null)
			RemovePresentation(active, GetBinding(active));
		_engine.SetMode(Mode);
		if (Mode == ScreenMode.MultiView) {
			foreach (var screen in Screens.Where(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton && !ReferenceEquals(screen, active)))
				AddPresentation(screen, GetBinding(screen));
		}
		if (active != null) {
			AddPresentation(active, GetBinding(active));
			if (GetBinding(active).Tab != null)
				_tabs.SelectedTab = GetBinding(active).Tab;
		}
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

	protected override void Dispose(bool disposing) {
		if (!disposing || _disposing) {
			base.Dispose(disposing);
			return;
		}
		var errors = new List<Exception>();
		_disposing = true;
		Tools.Exceptions.ExecuteCapturingException(ClearDockPreview, errors);
		IDisposable update = null;
		Tools.Exceptions.ExecuteCapturingException(() => update = EnterUpdateScope(), errors);
		using (Tools.Scope.ExecuteOnDispose(() => {
			if (update != null)
				Tools.Exceptions.ExecuteCapturingException(update.Dispose, errors);
		})) {
			Tools.Exceptions.ExecuteCapturingException(() => ChangeActiveScreen(null), errors);
			foreach (var screen in Screens)
				Tools.Exceptions.ExecuteCapturingException(() => DestroyScreen(screen), errors);
			Tools.Exceptions.ExecuteCapturingException(_engine.Dispose, errors);
		}
		Tools.Exceptions.ExecuteCapturingException(() => base.Dispose(disposing), errors);
		Tools.Exceptions.ExecuteCapturingException(_tabImages.Dispose, errors);
		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException(errors);
	}

	private IDisposable EnterUpdateScope(bool repaintImmediately = true) => new ScreenTransitionScope(this, repaintImmediately);

	private static bool CanHide(WinFormsApplicationScreen? Screen) {
		var Cancel = false;
		Screen?.NotifyHideScreen(ref Cancel);
		return !Cancel;
	}

	private void ChangeActiveScreen(WinFormsApplicationScreen? Screen) {
		if (ReferenceEquals(Screen, ActiveScreen))
			return;
		if (!_updating || !_activeSelectionChanged)
			OnActiveScreenChanging(ActiveScreen);
		_activeSelectionChanged = _updating;
		_engine.SelectScreen(Screen);
		if (!_updating) {
			UpdatePresentationVisibility();
			OnActiveScreenChanged(Screen);
		}
	}

	private bool ContainsScreen(WinFormsApplicationScreen screen) => screen != null && _screens.ContainsKey(screen);

	private bool TryGetBinding(WinFormsApplicationScreen screen, out ScreenBinding binding) {
		binding = null;
		return screen != null && _screens.TryGetValue(screen, out binding);
	}

	private ScreenBinding GetBinding(WinFormsApplicationScreen screen) {
		Guard.Argument(TryGetBinding(screen, out var binding), nameof(screen), "Screen does not belong to this host.");
		return binding;
	}

	private bool IsPresented(WinFormsApplicationScreen screen, ScreenBinding binding) => binding.Tab != null || binding.Window != null || ReferenceEquals(screen.Parent, _singleView);

	private ScreenBinding RetainScreen(WinFormsApplicationScreen screen, ApplicationScreenDefinition requestedDefinition = null) {
		var definition = requestedDefinition ?? screen.Block?.ScreenDefinitions.FirstOrDefault(item => item.ScreenType == screen.GetType());
		var binding = new ScreenBinding { Definition = definition, ImageKey = Guid.NewGuid().ToString("N") };
		var image = screen.ScreenImage
			?? definition?.Activators.OfType<IWinFormsApplicationMenuItem>().Select(item => item.Image16x16).FirstOrDefault(candidate => candidate != null)
			?? screen.Block?.ScreenDefinitions.Where(candidate => candidate.ScreenType == screen.GetType()).SelectMany(candidate => candidate.Activators)
				.OfType<IWinFormsApplicationMenuItem>().Select(item => item.Image16x16).FirstOrDefault(candidate => candidate != null);
		if (image != null)
			_tabImages.Images.Add(binding.ImageKey, image);
		_engine.Retain(screen, definition);
		_screens.Add(screen, binding);
		if (screen.ActivationMode == ScreenActivationMode.PermanentSingleton)
			_engine.SetOpen(screen, true);
		screen.TextChanged += ScreenTextChanged;
		screen.ScreenDestroyed += ScreenDestroyed;
		_engine.NotifyScreenChanged(screen);
		return binding;
	}

	private void UpdatePresentationVisibility() {
		var tabless = ScreenMode == ScreenMode.SingleView || ActiveScreen?.ScreenKind == ScreenKind.Empty;
		_tabs.Visible = !tabless;
		_singleView.Visible = tabless;
		if (tabless)
			_singleView.BringToFront();
		else
			_tabs.BringToFront();
	}

	private void AddPresentation(WinFormsApplicationScreen Screen, ScreenBinding Binding) {
		Screen.Dock = DockStyle.Fill;
		if (ScreenMode == ScreenMode.MultiView && Screen.ScreenKind == ScreenKind.Normal) {
			Binding.Tab = new TabPage(Screen.Title) { Tag = Screen, Padding = Padding.Empty,
				ImageKey = _tabImages.Images.ContainsKey(Binding.ImageKey) ? Binding.ImageKey : string.Empty };
			Binding.Tab.Controls.Add(Screen);
			_tabs.TabPages.Add(Binding.Tab);
		} else {
			_singleView.Controls.Add(Screen);
		}
		_engine.SetOpen(Screen, true);
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
		if (Screen.ActivationMode != ScreenActivationMode.PermanentSingleton)
			_engine.SetOpen(Screen, false);
	}

	private void DestroyScreen(WinFormsApplicationScreen Screen) {
		var Binding = GetBinding(Screen);
		Screen.TextChanged -= ScreenTextChanged;
		Screen.ScreenDestroyed -= ScreenDestroyed;
		RemovePresentation(Screen, Binding);
		_screens.Remove(Screen);
		_tabImages.Images.RemoveByKey(Binding.ImageKey);
		using var remove = Tools.Scope.ExecuteOnDispose(() => _engine.Remove(Screen));
		Screen.Dispose();
	}

	private void SelectRemainingScreen() {
		if (ActiveScreen != null || _disposing)
			return;
		// During a batched close the native selection can still name a removed page.
		var selectedIndex = _tabs.SelectedIndex;
		var next = selectedIndex >= 0 && selectedIndex < _tabs.TabPages.Count
			? _tabs.TabPages[selectedIndex].Tag as WinFormsApplicationScreen : null;
		next ??= Screens.FirstOrDefault(screen => IsPresented(screen, GetBinding(screen)) && GetBinding(screen).Window == null && screen.ScreenKind == ScreenKind.Normal);
		next ??= Screens.FirstOrDefault(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton);
		if (next != null) {
			ShowScreen(next);
			return;
		}
		if (OpenScreens.Length > 0)
			return;
		var empty = Tools.UI.GetEmptyScreen(_engine.RegistrationBlocks);
		if (empty != null)
			ActivateScreenCore(_engine.GetDefinitionOwner(empty), empty.ScreenType, empty.Title, requestedDefinition: empty, registerBlock: false);
	}

	private void TabSelecting(object? Sender, TabControlCancelEventArgs Args) {
		if (!_updating && !_tabs.Reordering && Args.TabPage?.Tag is WinFormsApplicationScreen Screen)
			// The native control is already selecting this page. Complete the host transition without selecting it again or painting before native input returns.
			Args.Cancel = !ShowScreenCore(Screen, true, selectTab: false);
	}

	private void SynchronizeTabOrder() {
		using var update = EnterUpdateScope();
		var ordered = _tabs.TabPages.Cast<TabPage>().Select(page => (WinFormsApplicationScreen)page.Tag).ToArray();
		for (var index = 0; index < ordered.Length; index++)
			_engine.Move(ordered[index], index);
	}

	private void ScreenTextChanged(object? Sender, EventArgs Args) {
		if (Sender is not WinFormsApplicationScreen screen || !TryGetBinding(screen, out var binding))
			return;
		if (binding.Tab != null)
			binding.Tab.Text = screen.Title;
		_engine.NotifyScreenChanged(screen);
	}

	private void ScreenDestroyed(object? Sender, EventArgs Args) {
		if (_disposing || Sender is not WinFormsApplicationScreen Screen || !ContainsScreen(Screen))
			return;
		using var Update = EnterUpdateScope();
		if (ReferenceEquals(ActiveScreen, Screen))
			ChangeActiveScreen(null);
		Screen.TextChanged -= ScreenTextChanged;
		Screen.ScreenDestroyed -= ScreenDestroyed;
		var binding = GetBinding(Screen);
		RemovePresentation(Screen, binding);
		_screens.Remove(Screen);
		_engine.Remove(Screen);
		SelectRemainingScreen();
	}

	private void ScreenFormDisposed(object? Sender, EventArgs Args) {
		if (_disposing || Sender is not WinFormsApplicationScreenForm Window || !TryGetBinding(Window.Screen, out var Binding) || !ReferenceEquals(Binding.Window, Window))
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
		public ApplicationScreenDefinition Definition { get; init; }
		public string ImageKey { get; init; }
		public TabPage? Tab { get; set; }
		public WinFormsApplicationScreenForm? Window { get; set; }
	}

	private sealed class ScreenTransitionScope : ScopeBase {
		private readonly WinFormsApplicationScreenHost _owner;
		private readonly IDisposable _rendering;
		private readonly IDisposable _notifications;

		public ScreenTransitionScope(WinFormsApplicationScreenHost owner, bool repaintImmediately) {
			_owner = owner;
			if (_owner._updateDepth++ > 0)
				return;
			_owner._updating = true;
			_owner._activeSelectionChanged = false;
			var entered = false;
			using var rollback = Tools.Scope.ExecuteOnDispose(() => {
				if (entered)
					return;
				Tools.Exceptions.ExecuteIgnoringException(() => _notifications?.Dispose());
				Tools.Exceptions.ExecuteIgnoringException(() => _rendering?.Dispose());
				_owner._updateDepth--;
				_owner._updating = false;
				_owner._activeSelectionChanged = false;
			});
			if (!_owner._disposing)
				_rendering = ((Control)_owner.FindForm() ?? _owner).EnterRenderingScope(repaintImmediately);
			_notifications = _owner._engine.DeferChanges();
			entered = true;
		}

		protected override void OnScopeEnd() {
			if (--_owner._updateDepth > 0)
				return;
			using var rendering = _rendering;
			using var notifications = _notifications;
			using var state = Tools.Scope.ExecuteOnDispose(() => {
				_owner._updating = false;
				_owner._activeSelectionChanged = false;
			});
			if (!_owner._disposing)
				_owner.UpdatePresentationVisibility();
			_owner._updating = false;
			if (_owner._activeSelectionChanged && !_owner._disposing)
				_owner.OnActiveScreenChanged(_owner.ActiveScreen);
		}

		protected override ValueTask OnScopeEndAsync() {
			OnScopeEnd();
			return ValueTask.CompletedTask;
		}
	}

	/// <summary>Tracks actual native controls in the common host while synchronous presentation remains on the UI thread.</summary>
	private sealed class NativeScreenHostEngine : ApplicationScreenHost<IWinFormsApplicationBlock, WinFormsApplicationScreen> {
		private readonly WinFormsApplicationScreenHost _owner;

		public NativeScreenHostEngine(WinFormsApplicationScreenHost owner)
			: base(new NativeServices(owner)) {
			_owner = owner;
			SetTrackedScreenMode(ScreenMode.SingleView);
		}

		protected override IApplicationScreenHost ScreenOwner => _owner;

		public ApplicationScreenDefinition GetActivationDefinition(Type screenType) => GetScreenActivationDefinition(screenType);

		public void ValidateActivation(Type screenType, ScreenActivationMode activationMode) => ValidateScreenActivation(screenType, activationMode);

		public ApplicationScreenDefinition[] Definitions => GetDefinitions();

		public IWinFormsApplicationBlock[] RegistrationBlocks => TrackedBlocks;

		public ApplicationScreenDefinition GetBlockDefault(IWinFormsApplicationBlock block) => GetDefaultScreen(block);

		public IWinFormsApplicationBlock GetDefinitionOwner(ApplicationScreenDefinition definition) => GetDefinitionBlock(definition);

		public void InitializeState() => MarkInitialized();

		public IDisposable DeferChanges() => DeferNotifications();

		public void RegisterDefinitions(ApplicationScreenDefinition[] definitions) => RegisterTrackedDefinitions(definitions);

		public void RegisterBlock(IWinFormsApplicationBlock block) {
			if (RegistrationBlocks.Contains(block, ReferenceEqualityComparer.Instance))
				return;
			AddAvailableBlock(block);
			RegisterTrackedBlock(block);
			OnChanged();
		}

		public void UnregisterBlock(IWinFormsApplicationBlock block) {
			UnregisterTrackedBlock(block);
			OnChanged();
		}

		public void ReorderBlocks(IEnumerable<IWinFormsApplicationBlock> blocks) => ReorderTrackedBlocks(blocks);

		public void SelectBlock(IWinFormsApplicationBlock block) => SetTrackedBlockSelection(block);

		public void Retain(WinFormsApplicationScreen screen, ApplicationScreenDefinition definition) {
			TrackScreen(screen, screen.Block, definition, screen.ActivationMode, screen.ScreenKind);
		}

		public void Remove(WinFormsApplicationScreen screen) {
			RemoveTrackedScreen(screen);
			OnChanged();
		}

		public void SelectScreen(WinFormsApplicationScreen screen) {
			var changed = !ReferenceEquals(ActiveScreen, screen);
			SetTrackedSelection(screen);
			if (changed)
				OnChanged();
		}

		public override IWinFormsApplicationBlock GetScreenBlock(WinFormsApplicationScreen screen) =>
			base.GetScreenBlock(screen) ?? (_owner.ContainsScreen(screen) ? screen.Block : null);

		public void SetOpen(WinFormsApplicationScreen screen, bool isOpen) {
			SetTrackedOpen(screen, isOpen);
			OnChanged();
		}

		public void SetMode(ScreenMode mode) {
			SetTrackedScreenMode(mode);
			OnChanged();
		}

		public void Move(WinFormsApplicationScreen screen, int index) {
			if (Array.FindIndex(OpenScreens, candidate => ReferenceEquals(candidate, screen)) == index)
				return;
			MoveTrackedScreen(screen, index);
			OnChanged();
		}

		public override Task InitializeAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			if (IsInitialized)
				return Task.CompletedTask;
			_owner.InitializeScreens(RegistrationBlocks);
			return Task.CompletedTask;
		}

		public override Task SelectBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			_owner.SelectBlock(block);
			return Task.CompletedTask;
		}

		public override async Task<WinFormsApplicationScreen> ActivateBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			EnsureRegistered(block);
			var definition = GetDefaultScreen(block);
			if (definition != null)
				return await OpenScreenAsync(block, definition, cancellationToken);
			if (await ClearSelectionAsync(cancellationToken))
				SetTrackedBlockSelection(block);
			return null;
		}

		public override Task<WinFormsApplicationScreen> ActivateScreenAsync(IWinFormsApplicationBlock block, Type screenType, string title = null,
			CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			EnsureRegistered(block);
			var definition = block.ScreenDefinitions.FirstOrDefault(item => item.ScreenType == screenType);
			Guard.Argument(definition != null, nameof(screenType), "The screen type is not registered in this block.");
			RegisterDefinitions(GetDefinitions());
			return Task.FromResult(_owner.ActivateScreenCore(block, definition.ScreenType, title ?? definition.Title, definition, false));
		}

		public override Task<WinFormsApplicationScreen> ActivateScreenAsync(IWinFormsApplicationBlock block, IScreenMenuItem item, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			EnsureRegistered(block);
			Guard.ArgumentNotNull(item, nameof(item));
			Guard.Argument(block.ScreenDefinitions.Contains(item.ScreenDefinition, ReferenceEqualityComparer.Instance), nameof(item), "The menu item does not refer to a registered screen.");
			return OpenScreenAsync(block, item.ScreenDefinition, cancellationToken);
		}

		public override Task<WinFormsApplicationScreen> OpenScreenAsync(IWinFormsApplicationBlock block, ApplicationScreenDefinition definition, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			Guard.ArgumentNotNull(definition, nameof(definition));
			EnsureRegistered(block);
			Guard.Argument(block.ScreenDefinitions.Contains(definition, ReferenceEqualityComparer.Instance), nameof(definition), "The screen definition is not owned by this block.");
			Tools.UI.ValidateScreenType(definition.ScreenType, typeof(WinFormsApplicationScreen));
			RegisterDefinitions(GetDefinitions());
			return Task.FromResult(_owner.ActivateScreenCore(block, definition.ScreenType, definition.Title, definition, false));
		}

		public override Task RegisterBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			Guard.ArgumentNotNull(block, nameof(block));
			if (_owner.FindForm() is BlockMainForm form) {
				if (!form.IsBlockRegistered(block))
					form.RegisterBlock(block);
			} else {
				if (IsInitialized)
					_owner.InitializeScreens(RegistrationBlocks.Append(block));
				else
					_owner.RegisterScreenTypes(block);
			}
			return Task.CompletedTask;
		}

		public override Task<bool> UnregisterBlockAsync(IWinFormsApplicationBlock block, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			EnsureRegistered(block);
			if (_owner.FindForm() is BlockMainForm form) {
				form.UnregisterBlock(block);
				return Task.FromResult(!form.IsBlockRegistered(block));
			}
			return Task.FromResult(_owner.UnregisterScreenTypes(block));
		}

		public override Task<bool> ClearSelectionAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			if (!CanHide(_owner.ActiveScreen))
				return Task.FromResult(false);
			using var update = _owner.EnterUpdateScope();
			var previous = _owner.ActiveScreen;
			_owner.ChangeActiveScreen(null);
			if (previous != null && (_owner.ScreenMode == ScreenMode.SingleView || previous.ScreenKind == ScreenKind.Empty)) {
				_owner.RemovePresentation(previous, _owner.GetBinding(previous));
				if (previous.ActivationMode == ScreenActivationMode.MultiInstance)
					_owner.DestroyScreen(previous);
			}
			if (OpenScreens.Length == 0)
				_owner.SelectRemainingScreen();
			return Task.FromResult(true);
		}

		public override Task<bool> TrySetScreenModeAsync(ScreenMode mode, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			return Task.FromResult(_owner.TrySetScreenMode(mode));
		}

		public override Task MoveScreenAsync(WinFormsApplicationScreen screen, int index, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			Guard.Argument(OpenScreens.Contains(screen, ReferenceEqualityComparer.Instance), nameof(screen), "The screen has no open tab.");
			Guard.ArgumentInRange(index, 0, OpenScreens.Length - 1, nameof(index));
			using var update = _owner.EnterUpdateScope();
			var binding = _owner.GetBinding(screen);
			if (binding.Tab != null) {
				var targetTab = OpenScreens.Take(index).Count(candidate => _owner.GetBinding(candidate).Tab != null);
				_owner._tabs.MoveTab(binding.Tab, Math.Min(targetTab, _owner._tabs.TabCount - 1));
			}
			Move(screen, index);
			return Task.CompletedTask;
		}

		public override Task<bool> ShowScreenAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			// The native owner validates both new supplied instances and retained screens before presentation.
			return Task.FromResult(_owner.ShowScreen(screen));
		}

		public override Task<bool> ShowScreenUndockedAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			return Task.FromResult(_owner.ShowScreenUndocked(screen));
		}

		public override Task<bool> CloseScreenAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			return Task.FromResult(_owner.CloseScreen(screen));
		}

		public override Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			return Task.FromResult(ActiveScreen == null || _owner.CloseScreen(ActiveScreen));
		}

		public override Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			return ActiveScreen == null ? Task.FromResult(true) : CanCloseScreenAsync(ActiveScreen, cancellationToken);
		}

		public override Task<bool> CloseScreensAsync(IEnumerable<WinFormsApplicationScreen> screens, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			return Task.FromResult(_owner.CloseScreens(screens));
		}

		public override Task<bool> CanCloseScreenAsync(WinFormsApplicationScreen screen, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			Guard.Argument(_owner.ContainsScreen(screen), nameof(screen), "The screen does not belong to this host.");
			return Task.FromResult(screen.ActivationMode != ScreenActivationMode.PermanentSingleton && _owner.CanCloseScreens(new[] { screen }));
		}

		public override Task<bool> CanNavigateAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			EnsureUsable();
			return Task.FromResult(_owner.CanCloseScreens(_owner.Screens));
		}

		protected override void OnScreenRemoved(WinFormsApplicationScreen screen) {
			// Presentation removal and control disposal belong to the owning native host.
		}

		private void EnsureRegistered(IWinFormsApplicationBlock block) {
			Guard.ArgumentNotNull(block, nameof(block));
			Guard.Argument(RegistrationBlocks.Contains(block, ReferenceEqualityComparer.Instance), nameof(block), "The block is not registered.");
		}
	}

	private sealed class NativeServices : IServiceProvider {
		private readonly WinFormsApplicationScreenHost _owner;

		public NativeServices(WinFormsApplicationScreenHost owner) => _owner = owner;

		public object GetService(Type serviceType) => serviceType == typeof(IApplicationScreenHost) || serviceType == typeof(IWinFormsApplicationScreenHost)
			? _owner : serviceType == typeof(IUserInterfaceServices) ? _owner.FindForm() as IUserInterfaceServices : null;
	}
}
