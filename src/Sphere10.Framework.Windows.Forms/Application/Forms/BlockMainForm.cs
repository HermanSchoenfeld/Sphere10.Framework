// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public partial class BlockMainForm : MainForm, IBlockManager {
	/// <summary>Signals block registration or active application selection changes on the UI thread.</summary>
	public event EventHandlerEx ApplicationChanged;

	private readonly SidebarToggleButton _navigationPaneToggleButton;
	private readonly (Color BackColor, Color ForeColor) _defaultDockPreviewColors;
	private TaskPane? _navigationThemePane;
	private bool _navigationPaneCollapsed;
	private double _navigationPaneWidth;
	private double _navigationPaneMinimumContentWidth;
	private int _maximumNavigationPaneWidth = 480;
	private int _navigationPaneDpi = 96;
	private bool _updatingNavigationPaneWidth;
	private bool _registeringBlocks;

	#region Form activation/destruction

	public BlockMainForm() {
		InitializeComponent();

		// Initialize the current navigation layout; registered blocks do not allocate panes until selected.
		_navigationPaneWidth = _splitContainer.SplitterDistance;
		_navigationPaneDpi = DeviceDpi;
		_navigationPaneMinimumContentWidth = _splitContainer.Panel2MinSize;
		_splitContainer.SplitterMoved += NavigationPaneSplitterMoved;
		_splitContainer.SizeChanged += NavigationPaneSizeChanged;
		_defaultDockPreviewColors = (ScreenHost.TabControl.DockPreviewBackColor, ScreenHost.TabControl.DockPreviewForeColor);
		_applicationBar.ButtonPressed += NavigationSelectionChanged;
		ScreenHost.Changed += ScreenHostChanged;
		Disposed += NavigationThemeChanged;

		_navigationPaneToggleButton = new SidebarToggleButton {
			Name = "_navigationPaneToggleButton",
			CheckOnClick = false,
			Overflow = ToolStripItemOverflow.Never,
			ToolTipText = "Hide sidebar (Ctrl+Alt+M)"
		};
		_navigationPaneToggleButton.Click += NavigationPaneToggle_Click;
		ToolStrip.Items.Insert(0, _navigationPaneToggleButton);

		// The SplitContainer owns the sidebar divider; no additional gutter is needed beside the content.
		_splitter.Dispose();
		_splitContainer.Panel2.Controls.Add(ScreenHost);
		ScreenHost.BringToFront();
		ApplyNavigationPaneWidth(_navigationPaneWidth);

	}

	#endregion

	#region Properties

	[DefaultValue(false), Category("Appearance"), Description("Collapse the navigation menu while preserving its width and current selection")]
	public bool NavigationPaneCollapsed {
		get {
			return _navigationPaneCollapsed;
		}
		set {
			if (_navigationPaneCollapsed == value)
				return;
			_navigationPaneCollapsed = value;
			UpdateNavigationPane(ActiveScreen);
		}
	}

	/// <summary>The preferred navigation width in device pixels, retained while hidden or temporarily constrained by a narrow window.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int NavigationPaneWidth {
		get {
			return (int)Math.Round(_navigationPaneWidth);
		}
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			ApplyNavigationPaneWidth(value);
		}
	}

	/// <summary>The maximum navigation width in logical pixels at 96 DPI. Available content space can impose a smaller limit.</summary>
	[DefaultValue(480), Category("Layout"), Description("Maximum navigation width in logical pixels at 96 DPI")]
	public int MaximumNavigationPaneWidth {
		get {
			return _maximumNavigationPaneWidth;
		}
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			_maximumNavigationPaneWidth = value;
			ApplyNavigationPaneWidth(_navigationPaneWidth);
		}
	}

	/// <summary>The block owning this window's visible active screen.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IApplicationBlock ActiveBlock {
		get {
			return ScreenHost.ActiveBlock;
		}
	}

	/// <summary>The block being browsed in the sidebar, independently of the visible active screen.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IApplicationBlock SelectedBlock {
		get {
			return ScreenHost.SelectedBlock;
		}
		set {
			if (!ReferenceEquals(ScreenHost.SelectedBlock, value))
				ScreenHost.SelectBlock(value);
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IApplicationBlock[] Blocks {
		get {
			return RegisteredBlocks;
		}
	}

	/// <summary>The registered blocks owned by the screen host.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IApplicationBlock[] RegisteredBlocks {
		get {
			return ScreenHost.Blocks;
		}
	}

	private bool HasUsableNavigationPaneBounds {
		get {
			return WindowState != FormWindowState.Minimized &&
				Math.Min(ClientSize.Width, _splitContainer.Width) - _splitContainer.SplitterWidth - Math.Round(_navigationPaneMinimumContentWidth) >= _splitContainer.Panel1MinSize;
		}
	}

	#endregion

	#region Block management

	public virtual void RegisterBlock(IApplicationBlock block) {
		if (_registeringBlocks)
			RegisterBlockCore(block);
		else
			RegisterBlocks(ScreenHost.StartupBlocks.Append(block), false);
	}

	// Register only the block's navigation button; its menu is built when selected.
	private void RegisterBlockCore(IApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		ScreenHost.RegisterScreenTypes(block);
		if (SelectedBlock == null || ReferenceEquals(SelectedBlock, block))
			Text = block.Name;

		var button = new SquareButton { Tag = block };
		_applicationBar.AddItem(new ApplicationBar.Item(button, null, block.Image32x32, block.Name));
		if (SelectedBlock == null)
			SelectedBlock = block;
		SynchronizeNavigationSelection();
	}

	/// <summary>Preserves startup declaration order independently of the navigation's Position ordering.</summary>
	internal bool RegisterBlocks(IEnumerable<IApplicationBlock> blocks, bool invokeOverrides = true) {
		var ordered = blocks.Distinct<IApplicationBlock>(ReferenceEqualityComparer.Instance).ToArray();
		var definitions = ordered.Where(block => !IsBlockRegistered(block)).ToArray();
		var accepted = ScreenHost.InitializeScreens(ordered, () => {
			using (Tools.Scope.ExecuteOnDispose(() => _registeringBlocks = false)) {
				_registeringBlocks = true;
				foreach (var block in definitions.OrderBy(block => block.Position)) {
					if (invokeOverrides)
						RegisterBlock(block);
					else
						RegisterBlockCore(block);
				}
			}
			var hasDefault = Tools.UI.GetDefaultScreen(ordered) != null;
			foreach (var item in definitions.SelectMany(block => block.Menus).SelectMany(menu => menu.Items)) {
				if (item.ExecuteOnLoad && (item is not IWinFormsScreenMenuItem || !hasDefault))
					ExecuteMenuItem(item);
			}
		});
		return accepted;
	}

	/// <summary>Removes an accepted block and its navigation view after the host's existing screen guards succeed.</summary>
	public virtual void UnregisterBlock(IApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.Argument(IsBlockRegistered(block), nameof(block), "Block is not registered");
		using var update = ScreenHost.EnterPresentationScope();
		if (!ScreenHost.UnregisterScreenTypes(block))
			return;

		// Release the removed block's generated presentation before disposing its command owners.
		foreach (var item in _applicationBar.Items.Where(item => ReferenceEquals(item.Button.Tag, block))) {
			var pane = item.MenuControl;
			_applicationBar.RemoveItem(item);
			DisposeNavigationPane(pane);
		}
		SynchronizeNavigationSelection();
		RebuildApplicationCommands();
		block.Dispose();
	}

	/// <summary>Queries the host's registered block inventory.</summary>
	public virtual bool IsBlockRegistered(IApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		return ScreenHost.Blocks.Contains(block, ReferenceEqualityComparer.Instance);
	}

	public virtual void ExecuteMenuItem(IApplicationMenuItem menuItem) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			if (menuItem is IWinFormsControlMenuItem) {
				ExecuteControlMenuItem(menuItem as IWinFormsControlMenuItem);
			} else if (menuItem is IWinFormsScreenMenuItem) {
				ExecuteViewMenuItem(menuItem as IWinFormsScreenMenuItem);
			} else if (menuItem is IWinFormsLinkMenuItem) {
				ExecuteLinkMenuItem(menuItem as IWinFormsLinkMenuItem);
			}
		}, out var e);
		if (e != null) {
			_ = ExceptionDialog.ShowAsync(e);
		}
	}

	private void ExecuteViewMenuItem(IWinFormsScreenMenuItem ViewItem) {
		if (ScreenHost.ActivateScreen(ViewItem.Parent.Parent, ViewItem.ScreenDefinition) != null)
			ExecuteLinkMenuItem(ViewItem);
	}

	private void ExecuteLinkMenuItem(IWinFormsLinkMenuItem linkItem) {
		linkItem.OnSelect(this);
	}

	private void ExecuteControlMenuItem(IWinFormsControlMenuItem controlItem) {
		throw new NotImplementedException();
	}

	/// <summary>Creates the selected block's current sidebar from its owner-supplied menus.</summary>
	protected virtual TaskPane CreateApplicationBlockPane(IApplicationBlock block) {
		var taskPane = new TaskPane {
			AutoScroll = true,
			AutoScrollMargin = new Size(12, 12),
			Dock = DockStyle.Fill,
			Name = block.Name + " TaskPane",
			Size = _applicationBar.Size,
			Tag = block
		};
		foreach (var menu in block.Menus)
			taskPane.Expandos.Add(CreateMenu(menu));
		return taskPane;
	}

	// Each generated menu group owns its font and child controls; command images remain borrowed.
	private Expando CreateMenu(IApplicationMenu menu) {
		var font = new Font("Tahoma", 8.25F);
		var expando = new Expando {
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
			Animate = true,
			AutoLayout = true,
			Font = font,
			Location = new Point(12, 12),
			Size = new Size(179, 60),
			Text = menu.Text,
			TitleImage = menu.Image32x32
		};
		expando.Disposed += (_, _) => font.Dispose();
		foreach (var item in menu.Items.Where(item => item.ShowOnExplorerBar))
			expando.Items.Add(CreateMenuItem(item));
		return expando;
	}

	// Generated views dispatch directly to their owner command; supplied controls retain their owner's lifetime.
	private Control CreateMenuItem(IApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		if (item is IWinFormsControlMenuItem control)
			return control.ControlToShow;

		var link = Guard.ArgumentCast<IWinFormsLinkMenuItem>(item, nameof(item));
		var taskItem = new TaskItem {
			Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
			BackColor = Color.Transparent,
			Image = link.Image16x16,
			Text = link.Text,
			TextAlign = ContentAlignment.TopLeft,
			UseVisualStyleBackColor = false
		};
		if (item is not IWinFormsScreenMenuItem) {
			// The generated link owns only its underline font; the command retains its image.
			var font = new Font(taskItem.Font, FontStyle.Underline);
			taskItem.Font = font;
			taskItem.Disposed += (_, _) => font.Dispose();
		}
		taskItem.Click += (_, _) => ExecuteMenuItem(item);
		return taskItem;
	}

	#endregion

	#region Screen management

	// Screen activation synchronizes the browsed block while preserving the screen's native display policy.
	protected override void OnActiveScreenChanged(ApplicationScreen screen) {
		if (screen != null) {
			if (screen.Block != null)
				SelectedBlock = screen.Block;
			if (screen.DisplayMode == ScreenDisplayMode.Maximized || screen.DisplayMode == ScreenDisplayMode.FilledAndMaximized)
				WindowState = FormWindowState.Maximized;
		}
		base.OnActiveScreenChanged(screen);
	}

	protected override bool ProcessCmdKey(ref Message Message, Keys KeyData) {
		if (KeyData == (Keys.Control | Keys.Alt | Keys.M) && ActiveScreen?.DisplayMode is not (ScreenDisplayMode.Filled or ScreenDisplayMode.FilledAndMaximized)) {
			NavigationPaneCollapsed = !NavigationPaneCollapsed;
			return true;
		}
		return base.ProcessCmdKey(ref Message, KeyData);
	}

	protected override void RescaleConstantsForDpi(int DeviceDpiOld, int DeviceDpiNew) {
		base.RescaleConstantsForDpi(DeviceDpiOld, DeviceDpiNew);
		// WinForms scales the splitter itself; the remembered width also needs to follow monitor changes while hidden.
		_navigationPaneWidth *= (double)DeviceDpiNew / DeviceDpiOld;
		_navigationPaneMinimumContentWidth *= (double)DeviceDpiNew / DeviceDpiOld;
		_navigationPaneDpi = DeviceDpiNew;
	}

	protected override void OnLayout(LayoutEventArgs Args) {
		if (_splitContainer == null || _navigationPaneWidth <= 0 || _updatingNavigationPaneWidth) {
			base.OnLayout(Args);
			return;
		}
		var RememberedWidth = _navigationPaneWidth;
		_updatingNavigationPaneWidth = true;
		using (Tools.Scope.ExecuteOnDispose(() => _updatingNavigationPaneWidth = false)) {
			// Release the previous drag range before docking. Even the normal minimum can move the splitter when a minimized panel is zero-sized.
			_splitContainer.Panel2MinSize = 0;
			base.OnLayout(Args);
		}
		ApplyNavigationPaneWidth(RememberedWidth);
	}

	protected override void SetClientSizeCore(int Width, int Height) {
		if (_splitContainer == null || _navigationPaneWidth <= 0 || _updatingNavigationPaneWidth) {
			base.SetClientSizeCore(Width, Height);
			return;
		}
		var RememberedWidth = _navigationPaneWidth;
		_updatingNavigationPaneWidth = true;
		using (Tools.Scope.ExecuteOnDispose(() => _updatingNavigationPaneWidth = false)) {
			// Form applies intermediate native bounds before storing the requested client size; only use the completed geometry.
			_splitContainer.Panel2MinSize = 0;
			base.SetClientSizeCore(Width, Height);
		}
		ApplyNavigationPaneWidth(RememberedWidth);
	}

	protected override void OnDpiChanged(DpiChangedEventArgs Args) {
		// Wait for WinForms to scale the entire control tree before applying limits against the new window size.
		var WasUpdating = _updatingNavigationPaneWidth;
		_updatingNavigationPaneWidth = true;
		using (Tools.Scope.ExecuteOnDispose(() => _updatingNavigationPaneWidth = WasUpdating)) {
			_splitContainer.Panel2MinSize = 0;
			base.OnDpiChanged(Args);
		}
		ApplyNavigationPaneWidth(_navigationPaneWidth);
	}

	private void UpdateNavigationPane(ApplicationScreen? Screen) {
		var ScreenFillsWindow = Screen?.DisplayMode is ScreenDisplayMode.Filled or ScreenDisplayMode.FilledAndMaximized;
		var Collapsed = NavigationPaneCollapsed || ScreenFillsWindow;
		_splitContainer.SuspendLayout();
		using var LayoutScope = Tools.Scope.ExecuteOnDispose(() => _splitContainer.ResumeLayout(true));
		if (_splitContainer.Panel1Collapsed != Collapsed) {
			var RememberedWidth = _navigationPaneWidth;
			var WasUpdating = _updatingNavigationPaneWidth;
			_updatingNavigationPaneWidth = true;
			using (Tools.Scope.ExecuteOnDispose(() => _updatingNavigationPaneWidth = WasUpdating)) {
				_splitContainer.Panel2MinSize = 0;
				_splitContainer.Panel1Collapsed = Collapsed;
			}
			ApplyNavigationPaneWidth(RememberedWidth);
		}
		_navigationPaneToggleButton.Checked = !Collapsed;
		_navigationPaneToggleButton.Enabled = !ScreenFillsWindow;
		_navigationPaneToggleButton.ToolTipText = $"{_navigationPaneToggleButton.Text} (Ctrl+Alt+M)";
	}

	private int GetMaximumNavigationPaneWidth(bool FitClientArea) {
		var MaximumWidth = Math.Min(int.MaxValue, Math.Round(MaximumNavigationPaneWidth * (_navigationPaneDpi / 96.0)));
		if (FitClientArea) {
			// Frame resizing gives space to the content pane; only physically unavoidable native panel limits constrain navigation.
			MaximumWidth = Math.Min(MaximumWidth, _splitContainer.Width - _splitContainer.SplitterWidth - Math.Round(_navigationPaneMinimumContentWidth));
		}
		return (int)Math.Max(_splitContainer.Panel1MinSize, MaximumWidth);
	}

	private void ApplyNavigationPaneWidth(double RequestedWidth) {
		if (_updatingNavigationPaneWidth || IsDisposed || Disposing)
			return;
		_updatingNavigationPaneWidth = true;
		using var UpdateScope = Tools.Scope.ExecuteOnDispose(() => _updatingNavigationPaneWidth = false);
		_navigationPaneWidth = Tools.Values.ClipValue(RequestedWidth, _splitContainer.Panel1MinSize, GetMaximumNavigationPaneWidth(false));
		// Minimize and intermediate layouts can have no legal divider position. Retain the preference until usable bounds return.
		if (!HasUsableNavigationPaneBounds)
			return;
		_splitContainer.Panel2MinSize = 0;
		if (_splitContainer.Panel1Collapsed) {
			_splitContainer.Panel2MinSize = (int)Math.Round(_navigationPaneMinimumContentWidth);
			return;
		}
		var MaximumWidth = GetMaximumNavigationPaneWidth(true);
		var Width = Tools.Values.ClipValue((int)Math.Round(_navigationPaneWidth), _splitContainer.Panel1MinSize, MaximumWidth);
		// Revealing an oversized hidden pane can leave stale negative content bounds until the divider is moved inside the window.
		if (_splitContainer.SplitterDistance > Width)
			_splitContainer.SplitterDistance = Width;
		// Use the native drag range so reaching the limit does not cancel the user's mouse or keyboard gesture.
		_splitContainer.Panel2MinSize = Math.Max((int)Math.Round(_navigationPaneMinimumContentWidth), _splitContainer.Width - _splitContainer.SplitterWidth - MaximumWidth);
		if (_splitContainer.SplitterDistance != Width)
			_splitContainer.SplitterDistance = Width;
		// The visible divider can be clamped by the frame width; only an explicit divider move replaces the preference.
	}

	private void NavigationPaneSplitterMoved(object? Sender, SplitterEventArgs Args) {
		if (HasUsableNavigationPaneBounds)
			ApplyNavigationPaneWidth(_splitContainer.Panel1Collapsed ? _navigationPaneWidth : _splitContainer.SplitterDistance);
	}

	private void NavigationPaneSizeChanged(object sender, EventArgs args) {
		ApplyNavigationPaneWidth(_navigationPaneWidth);
	}

	private void NavigationPaneToggle_Click(object sender, EventArgs args) {
		NavigationPaneCollapsed = !NavigationPaneCollapsed;
	}

	#endregion

	#region Misc

	protected virtual void OnApplicationChanged() {
		ApplicationChanged?.Invoke();
	}

	public virtual void ShowActiveScreenContextHelp() {
		var helpServices = Sphere10Framework.Instance.ServiceProvider.GetService<IHelpServices>();
		if (helpServices == null)
			return;

		if (ActiveBlock != null && ActiveScreen != null) {
			helpServices.ShowContextHelp(ActiveScreen);
		}
	}

	#endregion

	#region Handlers

	protected override void ContextHelp_Click(object sender, EventArgs e) {
		ShowActiveScreenContextHelp();
	}

	protected override void MainForm_HelpRequested(object sender, HelpEventArgs hlpevent) {
		ShowActiveScreenContextHelp();
	}

	#endregion

	#region ApplicationBar Handlers

	private void NavigationSelectionChanged(ApplicationBar source, ApplicationBar.Item button) {
		var block = button.Button.Tag as IApplicationBlock;
		using var restoreSelection = Tools.Scope.ExecuteOnDispose(SynchronizeNavigationSelection);
		if (block != null && IsBlockRegistered(block))
			ScreenHost.SelectBlock(block);
	}

	private void ScreenHostChanged() {
		if (IsDisposed || Disposing)
			return;
		SynchronizeNavigationSelection();
		UpdateNavigationPane(ActiveScreen);
		OnApplicationChanged();
	}

	// Keep only the selected block's generated menu; unrelated host notifications reuse that current view.
	private void SynchronizeNavigationSelection() {
		if (IsDisposed || Disposing)
			return;
		var selected = _applicationBar.Items.FirstOrDefault(item => ReferenceEquals(item.Button.Tag, SelectedBlock));
		foreach (var item in _applicationBar.Items.Where(item => item.Button.Tag is IApplicationBlock && !ReferenceEquals(item, selected))) {
			var pane = item.MenuControl;
			item.MenuControl = null;
			DisposeNavigationPane(pane);
		}
		if (selected != null && (selected.MenuControl == null || selected.MenuControl.IsDisposed)) {
			selected.MenuControl = CreateApplicationBlockPane(SelectedBlock);
			selected.MenuControl.Tag = SelectedBlock;
		}
		_applicationBar.SelectedItem = selected;
		UpdateNavigationTheme();
	}

	// Release generated views while retaining controls supplied by the block's command owners.
	private static void DisposeNavigationPane(Control pane) {
		if (pane == null)
			return;
		if (pane.Tag is IApplicationBlock block) {
			foreach (var item in block.Menus.SelectMany(menu => menu.Items).OfType<IWinFormsControlMenuItem>()) {
				var control = item.ControlToShow;
				if (pane.Contains(control))
					control.Parent.Controls.Remove(control);
			}
		}
		pane.Dispose();
	}

	// Forward live pane color/theme changes to the docking preview.
	private void NavigationThemeChanged(object sender, EventArgs args) {
		UpdateNavigationTheme();
	}

	// Observe only the currently displayed pane and use drawing utilities for its contrast color.
	private void UpdateNavigationTheme() {
		var pane = !IsDisposed && !Disposing ? _applicationBar.ApplicationBarControl as TaskPane : null;
		if (pane?.IsDisposed == true)
			pane = null;
		if (!ReferenceEquals(_navigationThemePane, pane)) {
			if (_navigationThemePane != null) {
				_navigationThemePane.CustomSettingsChanged -= NavigationThemeChanged;
				_navigationThemePane.BackColorChanged -= NavigationThemeChanged;
			}
			_navigationThemePane = pane;
			if (pane != null) {
				pane.CustomSettingsChanged += NavigationThemeChanged;
				pane.BackColorChanged += NavigationThemeChanged;
			}
		}
		if (IsDisposed || Disposing || ScreenHost.IsDisposed)
			return;
		var backColor = pane?.GradientStartColor ?? _defaultDockPreviewColors.BackColor;
		ScreenHost.TabControl.DockPreviewBackColor = backColor;
		ScreenHost.TabControl.DockPreviewForeColor = pane == null ? _defaultDockPreviewColors.ForeColor : Tools.Drawing.GetContrastingTextColor(backColor);
	}

	#endregion

}
