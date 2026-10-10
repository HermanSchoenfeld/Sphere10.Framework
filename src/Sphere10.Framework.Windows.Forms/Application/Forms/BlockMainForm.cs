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
using Sphere10.Framework.Application.UI;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

// TODO: Add icons
// TODO: Add menus
// TODO: Add block stuff to menus

public partial class BlockMainForm : MainForm, IBlockManager {
	/// <summary>Signals block registration or active application selection changes on the UI thread.</summary>
	public event EventHandlerEx ApplicationChanged;

	private IWinFormsApplicationMenu[] _applicationMenus = Array.Empty<IWinFormsApplicationMenu>();
	private IWinFormsApplicationMenuItem[] _applicationToolBarItems = Array.Empty<IWinFormsApplicationMenuItem>();
	private readonly List<ToolStripMenuItem> _applicationMenuHeaders = new();
	private readonly List<ToolStripItem> _applicationMenuItems = new();
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

		// initialize local members
		BlockBindings = new Dictionary<IWinFormsApplicationBlock, TaskPane>(ReferenceEqualityComparer.Instance);
		MenuBindings = new Dictionary<IWinFormsApplicationMenu, Expando>(ReferenceEqualityComparer.Instance);
		MenuItemBindings = new Dictionary<Control, IWinFormsApplicationMenuItem>();
		ToolStripBindings = new Dictionary<ToolStripItem, IWinFormsApplicationMenuItem>();
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

	public IDictionary<IWinFormsApplicationBlock, TaskPane> BlockBindings { get; set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IWinFormsApplicationBlock ActiveBlock {
		get => ScreenHost.ActiveBlock;
		set {
			if (ReferenceEquals(ScreenHost.ActiveBlock, value))
				return;
			ScreenHost.SelectBlock(value);
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IWinFormsApplicationBlock[] Blocks => RegisteredBlocks;

	private IDictionary<IWinFormsApplicationMenu, Expando> MenuBindings { get; set; }

	private IDictionary<Control, IWinFormsApplicationMenuItem> MenuItemBindings { get; set; }

	private IDictionary<ToolStripItem, IWinFormsApplicationMenuItem> ToolStripBindings { get; set; }

	private bool HasUsableNavigationPaneBounds => WindowState != FormWindowState.Minimized &&
		Math.Min(ClientSize.Width, _splitContainer.Width) - _splitContainer.SplitterWidth - Math.Round(_navigationPaneMinimumContentWidth) >= _splitContainer.Panel1MinSize;

	#endregion

	#region Block management

	public virtual void RegisterBlock(IWinFormsApplicationBlock block) {
		if (_registeringBlocks)
			RegisterBlockCore(block);
		else
			RegisterBlocks(ScreenHost.StartupBlocks.Append(block), false);
	}

	private void RegisterBlockCore(IWinFormsApplicationBlock block) {
		#region Pre-conditions

		Debug.Assert(block != null);
		Debug.Assert(!BlockBindings.ContainsKey(block));

		#endregion

		ScreenHost.RegisterScreenTypes(block);
		this.Text = block.Name;

		TaskPane taskPane = CreateApplicationBlockPane(block);
		taskPane.AutoScroll = true;
		taskPane.Dock = DockStyle.Fill;
		taskPane.Size = new Size(
			_applicationBar.Width,
			_applicationBar.Height
		);
		_applicationBar.AddItem(
			new ApplicationBar.Item(
				taskPane,
				block.Image32x32,
				block.Name
			)
		);

		BlockBindings.Add(block, taskPane);
		UpdateNavigationTheme();

		if (ActiveBlock == null) {
			ActiveBlock = block;
		}
	}

	/// <summary>Preserves startup declaration order independently of the navigation's Position ordering.</summary>
	internal bool RegisterBlocks(IEnumerable<IWinFormsApplicationBlock> blocks, bool invokeOverrides = true) {
		var ordered = blocks.Distinct<IWinFormsApplicationBlock>(ReferenceEqualityComparer.Instance).ToArray();
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

	public virtual void UnregisterBlock(IWinFormsApplicationBlock Block) {
		Guard.ArgumentNotNull(Block, nameof(Block));
		Guard.Argument(BlockBindings.ContainsKey(Block), nameof(Block), "Block is not registered");
		using var update = ScreenHost.EnterPresentationScope();
		if (!ScreenHost.UnregisterScreenTypes(Block))
			return;
		foreach (var Binding in MenuItemBindings.Where(Pair => ReferenceEquals(Pair.Value.Parent?.Parent, Block)).ToArray()) {
			MenuItemBindings.Remove(Binding.Key);
			Binding.Key.Dispose();
		}
		foreach (var Binding in ToolStripBindings.Where(Pair => ReferenceEquals(Pair.Value.Parent?.Parent, Block)).ToArray()) {
			ToolStripBindings.Remove(Binding.Key);
			Binding.Key.Dispose();
		}
		foreach (var Item in MenuStrip.Items.Cast<ToolStripItem>().Where(Item => ReferenceEquals(Item.Tag, Block)).ToArray())
			Item.Dispose();
		foreach (var Item in _applicationBar.Items.Where(Item => ReferenceEquals(Item.MenuControl, BlockBindings[Block])))
			_applicationBar.RemoveItem(Item);
		foreach (var Menu in Block.Menus)
			MenuBindings.Remove(Menu);
		BlockBindings[Block].Dispose();
		BlockBindings.Remove(Block);
		UpdateNavigationTheme();
		if (ReferenceEquals(ActiveBlock, Block))
			ActiveBlock = ActiveScreen?.Block ?? ScreenHost.Blocks.FirstOrDefault();
		Block.Dispose();
	}
	public virtual bool IsBlockRegistered(IWinFormsApplicationBlock block) {

		#region Pre-conditions

		Debug.Assert(block != null);

		#endregion

		return BlockBindings.ContainsKey(block);
	}

	public virtual IWinFormsApplicationBlock[] RegisteredBlocks => ScreenHost.Blocks;

	public virtual void ExecuteMenuItem(IWinFormsApplicationMenuItem menuItem) {
		try {
			if (menuItem is IWinFormsControlMenuItem) {
				ExecuteControlMenuItem(menuItem as IWinFormsControlMenuItem);
			} else if (menuItem is IWinFormsScreenMenuItem) {
				ExecuteViewMenuItem(menuItem as IWinFormsScreenMenuItem);
			} else if (menuItem is IWinFormsLinkMenuItem) {
				ExecuteLinkMenuItem(menuItem as IWinFormsLinkMenuItem);
			}
		} catch (Exception e) {
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

	private Control CreateControlMenuItem(IWinFormsControlMenuItem item) {
		return item.ControlToShow;
	}

	private Control CreateViewMenuItem(IWinFormsScreenMenuItem viewItem) {
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

	private Control CreateLinkMenuItem(IWinFormsLinkMenuItem item) {
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

	private Control CreateMenuItem(IWinFormsApplicationMenuItem item) {

		#region Pre-conditions

		Debug.Assert(item != null);

		#endregion

		Control menuItem = null;
		if (item is IWinFormsControlMenuItem) {
			menuItem = CreateControlMenuItem(item as IWinFormsControlMenuItem);
		} else if (item is IWinFormsScreenMenuItem) {
			menuItem = CreateViewMenuItem(item as IWinFormsScreenMenuItem);
		} else if (item is IWinFormsLinkMenuItem) {
			menuItem = CreateLinkMenuItem(item as IWinFormsLinkMenuItem);
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

	private Expando CreateMenu(IWinFormsApplicationMenu menu) {
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
		foreach (IWinFormsApplicationMenuItem item in menu.Items) {
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

	private TaskPane CreateApplicationBlockPane(IWinFormsApplicationBlock block) {
		TaskPane taskPane = new TaskPane();
		taskPane.AutoScroll = true;
		taskPane.AutoScrollMargin = new System.Drawing.Size(12, 12);
		taskPane.Dock = System.Windows.Forms.DockStyle.Fill;
		taskPane.Location = new System.Drawing.Point(0, 0);
		taskPane.Name = block.Name + " TaskPane";
		taskPane.TabIndex = 0;
		taskPane.Text = "N/A";
		foreach (IWinFormsApplicationMenu menu in block.Menus) {
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
				IWinFormsApplicationMenuItem menuItem = MenuItemBindings[taskItem];
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
				IWinFormsLinkMenuItem menuItem = MenuItemBindings[taskItem] as IWinFormsLinkMenuItem;
				ExecuteMenuItem(menuItem);
			} else {
// TODO: TaskItem did not bind to a IMenuItem
			}
		}
	}

	#endregion

	#region Menu & Toolbar management

	internal void SetApplicationCommands(IWinFormsApplicationMenu[] menus, IWinFormsApplicationMenuItem[] toolBarItems) {
		var items = menus.SelectMany(menu => menu.Items).Concat(toolBarItems).ToArray();
		Guard.Argument(items.All(item => item is IWinFormsLinkMenuItem), nameof(menus), "Application menus and toolbars require native link, action, or screen commands.");
		Guard.Argument(items.OfType<IWinFormsScreenMenuItem>().All(item => item.Parent?.Parent != null), nameof(menus), "Application screen commands must belong to a block menu.");
		_applicationMenus = Tools.Array.Clone(menus);
		_applicationToolBarItems = Tools.Array.Clone(toolBarItems);
		using var rendering = this.EnterRenderingScope();
		UpdateApplicationCommands();
	}

	/// <summary>Rebuilds the visible application and contextual commands once after a completed host transition.</summary>
	protected virtual void UpdateApplicationCommands() {
		var layers = GetApplicationCommandLayers();
		RebuildApplicationMenus(layers);
		RebuildToolBar(layers);
	}

	private ApplicationCommandLayer[] GetApplicationCommandLayers() {
		var layers = new List<ApplicationCommandLayer> {
			new(ApplicationCommandLevel.Application, new NativeCommandProvider(_applicationMenus, _applicationToolBarItems))
		};
		var block = ActiveScreen?.Block ?? ActiveBlock;
		if (block != null) {
			var menus = block.ShowInMenuStrip ? block.Menus.Where(menu => menu.ShowInMenuStrip).ToArray() : Array.Empty<IWinFormsApplicationMenu>();
			var toolbar = block.ShowInToolStrip ? block.Menus.SelectMany(menu => menu.Items).OfType<IWinFormsLinkMenuItem>()
				.Where(item => item.ShowOnToolStrip).Cast<IWinFormsApplicationMenuItem>() : Enumerable.Empty<IWinFormsApplicationMenuItem>();
			layers.Add(new ApplicationCommandLayer(ApplicationCommandLevel.Block, new NativeCommandProvider(menus, block.ToolBarItems.Concat(toolbar).ToArray())));
		}
		if (ActiveScreen is IApplicationCommandProvider<IWinFormsApplicationMenu, IWinFormsApplicationMenuItem> screen)
			layers.Add(new ApplicationCommandLayer(ApplicationCommandLevel.Screen, screen));
		return layers.ToArray();
	}

	private void RebuildApplicationMenus(ApplicationCommandLayer[] layers) {
		foreach (var item in _applicationMenuItems) {
			ToolStripBindings.Remove(item);
			item.Dispose();
		}
		_applicationMenuItems.Clear();
		foreach (var header in _applicationMenuHeaders.Where(header => header.IsDisposed || header.DropDownItems.Count == 0).ToArray()) {
			header.Dispose();
			_applicationMenuHeaders.Remove(header);
		}
		var menus = Tools.UI.MergeApplicationMenus<IWinFormsApplicationMenu, IWinFormsApplicationMenuItem>(
			(source, items) => new NativeMenuProjection(source, items), menu => GetMenuCaptionKey(menu.Text),
			item => GetMenuCaptionKey(item.Title), layers);
		foreach (var menu in menus) {
			var header = FindApplicationMenuHeader(menu.Text);
			if (header == null) {
				header = new ToolStripMenuItem(menu.Text);
				_applicationMenuHeaders.Add(header);
				InsertMenuItemBeforeHelpMenu(header);
			}
			var hasExistingItems = header.DropDownItems.Count > 0;
			var insertionIndex = 0;
			foreach (var item in menu.Items.Cast<IWinFormsLinkMenuItem>()) {
				var button = new ToolStripMenuItem(item.Text, item.Image16x16, ToolStripItemActivate) {
					Enabled = IsApplicationCommandAvailable(item)
				};
				header.DropDownItems.Insert(insertionIndex++, button);
				_applicationMenuItems.Add(button);
				ToolStripBindings.Add(button, item);
			}
			if (hasExistingItems && insertionIndex > 0) {
				var separator = new ToolStripSeparator();
				header.DropDownItems.Insert(insertionIndex, separator);
				_applicationMenuItems.Add(separator);
			}
		}
		KeepHelpMenuLast();
	}

	private bool IsApplicationCommandAvailable(IWinFormsLinkMenuItem item) =>
		item is not IWinFormsScreenMenuItem screen || screen.Parent?.Parent != null && IsBlockRegistered(screen.Parent.Parent);

	private void AddToolBarItem(IWinFormsLinkMenuItem item, bool enabled = true) {
		var button = new ToolStripButton(string.Empty, item.Image16x16 ?? Resources.DefaultToolStripImage, ToolStripItemActivate) {
			ToolTipText = item.Text,
			Enabled = enabled
		};
		ToolStrip.Items.Add(button);
		ToolStripBindings.Add(button, item);
	}

	private void ToolStripItemActivate(object sender, EventArgs e) {
		if (sender is ToolStripItem) {
			ToolStripItem stripItem = sender as ToolStripItem;
			if (ToolStripBindings.ContainsKey(stripItem)) {
				IWinFormsApplicationMenuItem menuItem = ToolStripBindings[stripItem];
				ExecuteMenuItem(menuItem);
			} else {
// TODO: TaskItem did not bind to a IMenuItem
			}
		}
	}

	private void RebuildToolBar() => RebuildToolBar(GetApplicationCommandLayers());

	private void RebuildToolBar(ApplicationCommandLayer[] layers) {
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
		foreach (var item in Tools.UI.MergeApplicationToolBarItems<IWinFormsApplicationMenuItem>(item => GetMenuCaptionKey(item.Title), layers).Cast<IWinFormsLinkMenuItem>())
			AddToolBarItem(item, IsApplicationCommandAvailable(item));

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

	protected override void OnActiveScreenChanged(WinFormsApplicationScreen? Screen) {
		if (Screen != null) {
			ActiveBlock = Screen.Block;
			if (Screen.DisplayMode == ScreenDisplayMode.Maximized || Screen.DisplayMode == ScreenDisplayMode.FilledAndMaximized)
				WindowState = FormWindowState.Maximized;
		}
		MergeScreenMenu(Screen);
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

	private void UpdateNavigationPane(WinFormsApplicationScreen? Screen) {
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

	protected virtual void OnApplicationChanged() => ApplicationChanged?.Invoke();

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

	private void NavigationSelectionChanged(ApplicationBar source, ApplicationBar.Item button) {
		var block = BlockBindings.FirstOrDefault(binding => ReferenceEquals(binding.Value, button.MenuControl)).Key;
		if (block != null)
			ScreenHost.SelectBlock(block);
		UpdateNavigationTheme();
	}

	private void ScreenHostChanged() {
		if (IsDisposed || Disposing)
			return;
		if (ActiveBlock != null && BlockBindings.TryGetValue(ActiveBlock, out var pane)) {
			var selected = _applicationBar.Items.FirstOrDefault(item => ReferenceEquals(item.MenuControl, pane));
			if (selected != null && !ReferenceEquals(selected, _applicationBar.SelectedItem))
				_applicationBar.SelectedItem = selected;
		}
		UpdateNavigationPane(ActiveScreen);
		UpdateApplicationCommands();
		OnApplicationChanged();
	}

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

	#endregion

	private sealed class NativeCommandProvider : IApplicationCommandProvider<IWinFormsApplicationMenu, IWinFormsApplicationMenuItem> {
		public NativeCommandProvider(IWinFormsApplicationMenu[] menus, IWinFormsApplicationMenuItem[] toolBarItems) {
			Menus = menus;
			ToolBarItems = toolBarItems;
		}

		public IWinFormsApplicationMenu[] Menus { get; }
		public IWinFormsApplicationMenuItem[] ToolBarItems { get; }
	}

	private sealed class NativeMenuProjection : IWinFormsApplicationMenu {
		private readonly IWinFormsApplicationMenu _source;
		private readonly IWinFormsApplicationMenuItem[] _items;

		public NativeMenuProjection(IWinFormsApplicationMenu source, IWinFormsApplicationMenuItem[] items) {
			_source = source;
			_items = Tools.Array.Clone(items);
			Parent = source.Parent;
		}

		public string Text => _source.Text;
		public Image Image32x32 => _source.Image32x32;
		public bool ShowInMenuStrip => _source.ShowInMenuStrip;
		public IWinFormsApplicationBlock Parent { get; set; }
		public IWinFormsApplicationMenuItem[] Items => Tools.Array.Clone(_items);
		public void Dispose() { }
	}

}
