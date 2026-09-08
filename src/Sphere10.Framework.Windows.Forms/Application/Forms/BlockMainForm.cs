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
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

// TODO: Add icons
// TODO: Add menus
// TODO: Add plugin stuff to menus

public partial class BlockMainForm : MainForm, IBlockManager {
	private readonly SidebarToggleButton _navigationPaneToggleButton;
	private readonly (Color BackColor, Color ForeColor) _defaultDockPreviewColors;
	private TaskPane? _navigationThemePane;
	private bool _navigationPaneCollapsed;
	private double _navigationPaneWidth;
	private double _navigationPaneMinimumContentWidth;
	private int _maximumNavigationPaneWidth = 480;
	private int _navigationPaneDpi = 96;
	private bool _updatingNavigationPaneWidth;

	#region Form activation/destruction

	public BlockMainForm() {
		InitializeComponent();

		// initialize local members
		PluginBindings = new Dictionary<IApplicationBlock, TaskPane>();
		MenuBindings = new Dictionary<IMenu, Expando>();
		MenuItemBindings = new Dictionary<Control, IMenuItem>();
		ToolStripBindings = new Dictionary<ToolStripItem, IMenuItem>();
		Plugins = new List<IApplicationBlock>();
		ActivePlugin = null;
		_navigationPaneWidth = _splitContainer.SplitterDistance;
		_navigationPaneDpi = DeviceDpi;
		_navigationPaneMinimumContentWidth = _splitContainer.Panel2MinSize;
		_splitContainer.SplitterMoved += NavigationPaneSplitterMoved;
		_splitContainer.SizeChanged += NavigationPaneSizeChanged;
		_defaultDockPreviewColors = (ScreenHost.TabControl.DockPreviewBackColor, ScreenHost.TabControl.DockPreviewForeColor);
		_applicationBar.ButtonPressed += NavigationSelectionChanged;
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

	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode) {
			RebuildToolBar();
		}
	}

	#endregion

	#region Properties

	[DefaultValue(false), Category("Appearance"), Description("Collapse the navigation menu while preserving its width and current selection")]
	public bool NavigationPaneCollapsed {
		get => _navigationPaneCollapsed;
		set {
			if (_navigationPaneCollapsed == value)
				return;
			_navigationPaneCollapsed = value;
			UpdateNavigationPane(ActiveScreen);
		}
	}

	/// <summary>The navigation width in device pixels, including its remembered width when temporarily hidden.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int NavigationPaneWidth {
		get => (int)Math.Round(_navigationPaneWidth);
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			ApplyNavigationPaneWidth(value);
		}
	}

	/// <summary>The maximum navigation width in logical pixels at 96 DPI. Available content space can impose a smaller limit.</summary>
	[DefaultValue(480), Category("Layout"), Description("Maximum navigation width in logical pixels at 96 DPI")]
	public int MaximumNavigationPaneWidth {
		get => _maximumNavigationPaneWidth;
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			_maximumNavigationPaneWidth = value;
			ApplyNavigationPaneWidth(_navigationPaneWidth);
		}
	}

	public IDictionary<IApplicationBlock, TaskPane> PluginBindings { get; set; }

	public IApplicationBlock ActiveBlock { get; set; }

	public List<IApplicationBlock> Blocks { get; set; }

	private IDictionary<IMenu, Expando> MenuBindings { get; set; }

	private IDictionary<Control, IMenuItem> MenuItemBindings { get; set; }

	private IDictionary<ToolStripItem, IMenuItem> ToolStripBindings { get; set; }

	private IList<IApplicationBlock> Plugins { get; set; }

	private IApplicationBlock ActivePlugin { get; set; }

	private bool HasUsableNavigationPaneBounds => WindowState != FormWindowState.Minimized &&
		Math.Min(ClientSize.Width, _splitContainer.Width) - _splitContainer.SplitterWidth - Math.Round(_navigationPaneMinimumContentWidth) >= _splitContainer.Panel1MinSize;

	#endregion

	#region Block management

	public virtual void RegisterBlock(IApplicationBlock plugin) {

		#region Pre-conditions

		Debug.Assert(plugin != null);
		Debug.Assert(!PluginBindings.ContainsKey(plugin));

		#endregion

		ScreenHost.RegisterScreenTypes(plugin);
		this.Text = plugin.Name;

		TaskPane taskPane = CreateApplicationBlockPane(plugin);
		taskPane.AutoScroll = true;
		taskPane.Dock = DockStyle.Fill;
		taskPane.Size = new Size(
			_applicationBar.Width,
			_applicationBar.Height
		);
		_applicationBar.AddItem(
			new ApplicationBar.Item(
				taskPane,
				plugin.Image32x32,
				plugin.Name
			)
		);

		Plugins.Add(plugin);
		PluginBindings.Add(plugin, taskPane);
		UpdateNavigationTheme();

		if (ActiveBlock == null) {
			ActiveBlock = plugin;
		}
		if (plugin.ShowInMenuStrip) {
			RegisterBlockInMenu(plugin);
		}
		RebuildToolBar();

// TODO: Execute these on form load rather than now?
		foreach (IMenu menu in plugin.Menus) {
			foreach (IMenuItem menuItem in menu.Items) {
				if (menuItem.ExecuteOnLoad) {
					ExecuteMenuItem(menuItem);
				}
			}
		}

		if (ActiveScreen == null && plugin.DefaultScreen != null)
			ScreenHost.ActivateScreen(plugin, plugin.DefaultScreen, plugin.DefaultScreenTitle);
	}

	public virtual void UnregisterBlock(IApplicationBlock Block) {
		Guard.ArgumentNotNull(Block, nameof(Block));
		Guard.Argument(PluginBindings.ContainsKey(Block), nameof(Block), "Block is not registered");
		if (!ScreenHost.CloseScreens(ScreenHost.Screens.Where(Screen => ReferenceEquals(Screen.ApplicationBlock, Block))))
			return;
		foreach (var Binding in MenuItemBindings.Where(Pair => ReferenceEquals(Pair.Value.Parent.Parent, Block)).ToArray()) {
			MenuItemBindings.Remove(Binding.Key);
			Binding.Key.Dispose();
		}
		foreach (var Binding in ToolStripBindings.Where(Pair => ReferenceEquals(Pair.Value.Parent.Parent, Block)).ToArray()) {
			ToolStripBindings.Remove(Binding.Key);
			Binding.Key.Dispose();
		}
		foreach (var Item in MenuStrip.Items.Cast<ToolStripItem>().Where(Item => ReferenceEquals(Item.Tag, Block)).ToArray())
			Item.Dispose();
		foreach (var Item in _applicationBar.Items.Where(Item => ReferenceEquals(Item.MenuControl, PluginBindings[Block])))
			_applicationBar.RemoveItem(Item);
		foreach (var Menu in Block.Menus)
			MenuBindings.Remove(Menu);
		PluginBindings[Block].Dispose();
		PluginBindings.Remove(Block);
		Plugins.Remove(Block);
		UpdateNavigationTheme();
		if (ReferenceEquals(ActiveBlock, Block))
			ActiveBlock = ActiveScreen?.ApplicationBlock ?? Plugins.FirstOrDefault();
		Block.Dispose();
		RebuildToolBar();
	}
	public virtual bool IsBlockRegistered(IApplicationBlock plugin) {

		#region Pre-conditions

		Debug.Assert(plugin != null);

		#endregion

		return PluginBindings.ContainsKey(plugin);
	}

	public virtual IEnumerable<IApplicationBlock> RegisteredBlocks {
		get { return Plugins; }
	}

	public virtual void ExecuteMenuItem(IMenuItem menuItem) {
		try {
			if (menuItem is IControlMenuItem) {
				ExecuteControlMenuItem(menuItem as IControlMenuItem);
			} else if (menuItem is IScreenMenuItem) {
				ExecuteViewMenuItem(menuItem as IScreenMenuItem);
			} else if (menuItem is ILinkMenuItem) {
				ExecuteLinkMenuItem(menuItem as ILinkMenuItem);
			}
		} catch (Exception e) {
			_ = ExceptionDialog.ShowAsync(e);
		}
	}

	private void ExecuteViewMenuItem(IScreenMenuItem ViewItem) {
		if (ScreenHost.ActivateScreen(ViewItem.Parent.Parent, ViewItem.Screen, ViewItem.ScreenTitle ?? ViewItem.Text) != null)
			ExecuteLinkMenuItem(ViewItem);
	}

	private void ExecuteLinkMenuItem(ILinkMenuItem linkItem) {
		linkItem.OnSelect();
	}

	private void ExecuteControlMenuItem(IControlMenuItem controlItem) {
		throw new NotImplementedException();
	}

	private Control CreateControlMenuItem(IControlMenuItem item) {
		return item.ControlToShow;
	}

	private Control CreateViewMenuItem(IScreenMenuItem viewItem) {
		TaskItem taskItem = new TaskItem();
		taskItem.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		taskItem.BackColor = System.Drawing.Color.Transparent;
		taskItem.Image = viewItem.Image16x16;
		taskItem.Name = "N/A";
		taskItem.Text = viewItem.Text;
		taskItem.TextAlign = System.Drawing.ContentAlignment.TopLeft;
		taskItem.UseVisualStyleBackColor = false;
		taskItem.Click += new EventHandler(TaskItem_Clicked);
		return taskItem;
	}

	private Control CreateLinkMenuItem(ILinkMenuItem item) {
		TaskItem taskItem = new TaskItem();
		taskItem.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		taskItem.BackColor = System.Drawing.Color.Transparent;
		taskItem.Image = item.Image16x16;
		taskItem.Name = "N/A";
		taskItem.Text = item.Text;
		taskItem.TextAlign = System.Drawing.ContentAlignment.TopLeft;
		taskItem.Font = new Font(taskItem.Font, FontStyle.Underline);
		taskItem.UseVisualStyleBackColor = false;
		taskItem.Click += new EventHandler(TaskItem_Clicked);
		return taskItem;
	}

	private Control CreateMenuItem(IMenuItem item) {

		#region Pre-conditions

		Debug.Assert(item != null);

		#endregion

		Control menuItem = null;
		if (item is IControlMenuItem) {
			menuItem = CreateControlMenuItem(item as IControlMenuItem);
		} else if (item is IScreenMenuItem) {
			menuItem = CreateViewMenuItem(item as IScreenMenuItem);
		} else if (item is ILinkMenuItem) {
			menuItem = CreateLinkMenuItem(item as ILinkMenuItem);
		}
		MenuItemBindings.Add(
			menuItem,
			item
		);

		#region Post-conditions

		Debug.Assert(menuItem != null);

		#endregion

		return menuItem;
	}

	private Expando CreateMenu(IMenu menu) {
		Expando expando = new Expando();
		expando.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		expando.Animate = true;
		expando.AutoLayout = true;
		expando.Font = new System.Drawing.Font("Tahoma", 8.25F);
		expando.Location = new System.Drawing.Point(12, 12);
		expando.Name = "N/A";
		expando.Size = new System.Drawing.Size(179, 60);
		expando.TabIndex = 0;
		expando.Text = menu.Text;
		expando.TitleImage = menu.Image32x32;
		expando.SizeChanged += expando_SizeChanged;
		foreach (IMenuItem item in menu.Items) {
			if (item.ShowOnExplorerBar) {
				expando.Items.Add(
					CreateMenuItem(item)
				);
			}
		}

		MenuBindings.Add(menu, expando);
		return expando;
	}

	void expando_SizeChanged(object sender, EventArgs e) {
	}

	private TaskPane CreateApplicationBlockPane(IApplicationBlock plugin) {
		TaskPane taskPane = new TaskPane();
		taskPane.AutoScroll = true;
		taskPane.AutoScrollMargin = new System.Drawing.Size(12, 12);
		taskPane.Dock = System.Windows.Forms.DockStyle.Fill;
		taskPane.Location = new System.Drawing.Point(0, 0);
		taskPane.Name = plugin.Name + " TaskPane";
		taskPane.TabIndex = 0;
		taskPane.Text = "N/A";
		foreach (IMenu menu in plugin.Menus) {
			taskPane.Expandos.Add(
				CreateMenu(menu)
			);
		}
		return taskPane;
	}

	private void TaskItem_Clicked(object sender, EventArgs e) {
		if (sender is TaskItem) {
			TaskItem taskItem = sender as TaskItem;
			if (MenuItemBindings.ContainsKey(taskItem)) {
				IMenuItem menuItem = MenuItemBindings[taskItem];
				ExecuteMenuItem(menuItem);
			} else {
// TODO: TaskItem did not bind to a IMenuItem
			}
		}
	}

	private void LinkItem_Clicked(object sender, EventArgs e) {
		if (sender is TaskItem) {
			TaskItem taskItem = sender as TaskItem;
			if (MenuItemBindings.ContainsKey(taskItem)) {
				ILinkMenuItem menuItem = MenuItemBindings[taskItem] as ILinkMenuItem;
				ExecuteMenuItem(menuItem);
			} else {
// TODO: TaskItem did not bind to a IMenuItem
			}
		}
	}

	#endregion

	#region Menu & Toolbar management

	private void RegisterBlockInMenu(IApplicationBlock block) {
		ToolStripMenuItem blockHeader = new ToolStripMenuItem(
			block.Name
		);

		blockHeader.Tag = block;

		// register each menu in block
		foreach (IMenu menu in block.Menus) {

			if (menu.ShowInMenuStrip) {

				ToolStripMenuItem menuHeader = new ToolStripMenuItem(menu.Text);
				menuHeader.Tag = menu;
				foreach (IMenuItem item in menu.Items) {
					ILinkMenuItem linkItem = item as ILinkMenuItem;
					if (linkItem.ShowOnExplorerBar) {
						ToolStripMenuItem newItem = new ToolStripMenuItem(
							linkItem.Text,
							linkItem.Image16x16,
							ToolStripItemActivate
						);
						int index = menuHeader.DropDownItems.Add(newItem);
						menuHeader.DropDownItems[index].Tag = item;
						ToolStripBindings.Add(
							newItem,
							item
						);
					}
				}
				blockHeader.DropDownItems.Add(menuHeader);
			}
		}

		// insert the block menu before the help menu
		InsertMenuItemBeforeHelpMenu(blockHeader);
	}

	private void ToolStripItemActivate(object sender, EventArgs e) {
		if (sender is ToolStripItem) {
			ToolStripItem stripItem = sender as ToolStripItem;
			if (ToolStripBindings.ContainsKey(stripItem)) {
				IMenuItem menuItem = ToolStripBindings[stripItem];
				ExecuteMenuItem(menuItem);
			} else {
// TODO: TaskItem did not bind to a IMenuItem
			}
		}
	}

	private void RebuildToolBar() {
		RestoreScreenToolBar();
		ToolStrip.SuspendLayout();
		using var Layout = Tools.Scope.ExecuteOnDispose(() => ToolStrip.ResumeLayout());
		ToolStrip.Items.Remove(_navigationPaneToggleButton);
		foreach (var Item in ToolStripBindings.Keys.Where(Item => Item.Owner == ToolStrip).ToArray()) {
			ToolStripBindings.Remove(Item);
			Item.Dispose();
		}
		foreach (ToolStripItem Item in ToolStrip.Items.Cast<ToolStripItem>().ToArray())
			Item.Dispose();
		ToolStrip.Items.Clear();

		#region Add standard buttons

		ToolStrip.Items.Add(_navigationPaneToggleButton);

		#endregion

		#region Add screen list buttons

		foreach (IApplicationBlock block in Plugins) {
			if (block.ShowInToolStrip) {
				if (ToolStrip.Items.Count > 0) {
					ToolStrip.Items.Add(new ToolStripSeparator());
				}
				foreach (IMenu menu in block.Menus) {
					foreach (IMenuItem item in menu.Items) {
						if (item is ILinkMenuItem) {
							ILinkMenuItem linkItem = item as ILinkMenuItem;
							if (linkItem.ShowOnToolStrip) {
								ToolStripButton button = new ToolStripButton(
									string.Empty,
									item.Image16x16 != null ? item.Image16x16 : Sphere10.Framework.Windows.Forms.Resources.DefaultToolStripImage,
									ToolStripItemActivate
								);
								button.ToolTipText = linkItem.Text;
								ToolStrip.Items.Add(button);
								ToolStripBindings.Add(
									button,
									item
								);
							}
						}
					}
				}
			}
		}

		#endregion

		MergeScreenToolBar();

		#region Add help buttons

		ToolStrip.Items.Add(new ToolStripSeparator());

		ToolStripItem contextHelpButton = ToolStrip.Items.Add(
			string.Empty,
			Sphere10.Framework.Windows.Forms.Resources.Help_16x16x32,
			ContextHelp_Click
		);
		contextHelpButton.ToolTipText = "Get help for currently opened screen";

		#endregion

	}

	private void InsertMenuItemBeforeHelpMenu(ToolStripMenuItem menuItem) {
		bool foundLocation = false;
		int location = 0;
		for (int i = 0; i < MenuStrip.Items.Count; i++) {
			if (MenuStrip.Items[i] == HelpToolStripMenuItem) {
				location = i;
				foundLocation = true;
				break;
			}
		}
		if (!foundLocation) {
			location = 0;
		}
		MenuStrip.Items.Insert(location, menuItem);
	}

	#endregion

	#region Screen management

	protected override void OnActiveScreenChanged(ApplicationScreen? Screen) {
		if (Screen != null) {
			ActiveBlock = Screen.ApplicationBlock;
			if (Screen.DisplayMode == ScreenDisplayMode.Maximized || Screen.DisplayMode == ScreenDisplayMode.FilledAndMaximized)
				WindowState = FormWindowState.Maximized;
		}
		UpdateNavigationPane(Screen);
		RebuildToolBar();
		base.OnActiveScreenChanged(Screen);
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
			if (Collapsed && HasUsableNavigationPaneBounds)
				_navigationPaneWidth = _splitContainer.SplitterDistance;
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
			// Keep the screen usable when the window is narrower than the configured sidebar limit plus its content.
			var ContentWidth = Math.Max(_navigationPaneMinimumContentWidth, Math.Round(320 * (_navigationPaneDpi / 96.0)));
			MaximumWidth = Math.Min(MaximumWidth, _splitContainer.Width - _splitContainer.SplitterWidth - ContentWidth);
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
		_navigationPaneWidth = _splitContainer.SplitterDistance;
	}

	private void NavigationPaneSplitterMoved(object? Sender, SplitterEventArgs Args) {
		if (HasUsableNavigationPaneBounds)
			ApplyNavigationPaneWidth(_splitContainer.Panel1Collapsed ? _navigationPaneWidth : _splitContainer.SplitterDistance);
	}

	private void NavigationPaneSizeChanged(object? Sender, EventArgs Args) => ApplyNavigationPaneWidth(_navigationPaneWidth);

	private void NavigationPaneToggle_Click(object? Sender, EventArgs Args) => NavigationPaneCollapsed = !NavigationPaneCollapsed;

	#endregion

	#region Misc

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

	#region Toolbar Handlers

	#endregion

	#region ApplicationBar Handlers

	private void NavigationSelectionChanged(ApplicationBar Source, ApplicationBar.Item Button) => UpdateNavigationTheme();

	private void NavigationThemeChanged(object? Sender, EventArgs Args) => UpdateNavigationTheme();

	private void UpdateNavigationTheme() {
		var Pane = !IsDisposed && !Disposing ? _applicationBar.ApplicationBarControl as TaskPane : null;
		if (Pane?.IsDisposed == true)
			Pane = null;
		if (!ReferenceEquals(_navigationThemePane, Pane)) {
			if (_navigationThemePane != null) {
				_navigationThemePane.CustomSettingsChanged -= NavigationThemeChanged;
				_navigationThemePane.BackColorChanged -= NavigationThemeChanged;
			}
			_navigationThemePane = Pane;
			if (Pane != null) {
				Pane.CustomSettingsChanged += NavigationThemeChanged;
				Pane.BackColorChanged += NavigationThemeChanged;
			}
		}
		if (IsDisposed || Disposing || ScreenHost.IsDisposed)
			return;
		var BackColor = Pane?.GradientStartColor ?? _defaultDockPreviewColors.BackColor;
		ScreenHost.TabControl.DockPreviewBackColor = BackColor;
		ScreenHost.TabControl.DockPreviewForeColor = Pane == null ? _defaultDockPreviewColors.ForeColor : GetNavigationThemeForeColor(BackColor);
	}

	private static Color GetNavigationThemeForeColor(Color BackColor) {
		// Choose whichever text color has greater contrast against the pane's actual background.
		static double Linearize(byte Channel) {
			var Value = Channel / 255.0;
			return Value <= 0.04045 ? Value / 12.92 : Math.Pow((Value + 0.055) / 1.055, 2.4);
		}
		var Luminance = 0.2126 * Linearize(BackColor.R) + 0.7152 * Linearize(BackColor.G) + 0.0722 * Linearize(BackColor.B);
		return (Luminance + 0.05) / 0.05 >= 1.05 / (Luminance + 0.05) ? Color.Black : Color.White;
	}

	private void _applicationBar_ButtonPressed(ApplicationBar source, ApplicationBar.Item button) {
		// purpose here is to set ActivePlugin to current Plugin visible in application bar
		// current taskpane on application bar is current active view
		if (_applicationBar.ApplicationBarControl != null) {
			TaskPane pane = (TaskPane)_applicationBar.ApplicationBarControl;
			foreach (Control control in PluginBindings.Values) {
				if (pane == control) {
					foreach (IApplicationBlock block in PluginBindings.Keys) {
						if (PluginBindings[block] == control) {
							ActiveBlock = block;
							break;
						}
					}
					break;
				}
			}
		}
	}

	#endregion

}
