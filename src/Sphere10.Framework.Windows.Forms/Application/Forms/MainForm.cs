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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Native application frame that composes owner-supplied commands around its active screen.</summary>
public partial class MainForm : LiteMainForm, IMainForm {
	#region Fields

	private readonly ToolStripStableHeightBehavior _toolStripHeight;
	private IWinFormsApplicationMenu[] _menus;
	private IWinFormsApplicationMenuItem[] _toolBarItems;
	private WinFormsApplicationScreen _menuSelectedScreen;
	private bool _rebuildingCommands;
	private IUserInterfaceServices _commandUserInterfaceServices;

	#endregion

	#region Construction

	public MainForm() {
		InitializeComponent();
		_toolStripHeight = new ToolStripStableHeightBehavior(ToolStrip, ToolStripStableHeightBehavior.StandardLogicalHeight);
		Disposed += (_, _) => _toolStripHeight.Dispose();
		ScreenHost = new WinFormsApplicationScreenHost { Dock = DockStyle.Fill };
		Controls.Add(ScreenHost);
		ScreenHost.BringToFront();
		ScreenHost.ActiveScreenChanging += OnActiveScreenChanging;
		ScreenHost.ActiveScreenChanged += OnActiveScreenChanged;
		ScreenHost.ActiveScreenChanged += screen => _menuSelectedScreen = screen;
		ScreenHost.Changed += ScreensChanged;
	}

	#endregion

	#region Properties

	/// <summary>Activates and caches this form's own menu definitions; contextual owners retain their own commands.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IWinFormsApplicationMenu[] Menus {
		get {
			return Tools.Array.Clone(_menus ??= GetMenus());
		}
	}

	/// <summary>Activates and caches this form's own toolbar definitions without copying their native state or callbacks.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IWinFormsApplicationMenuItem[] ToolBarItems {
		get {
			return Tools.Array.Clone(_toolBarItems ??= GetToolBarItems());
		}
	}

	[DefaultValue(ScreenMode.SingleView), Category("Behavior"), Description("Display one screen or multiple screens in detachable tabs")]
	public ScreenMode ScreenMode {
		get {
			return ScreenHost.ScreenMode;
		}
		set {
			ScreenHost.ScreenMode = value;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public WinFormsApplicationScreenHost ScreenHost { get; }

	/// <summary>The UI invoking the current native command; asynchronous handlers capture it before their first await.</summary>
	/// <remarks>A detached command supplies its local screen host without changing this form's active screen.</remarks>
	[Browsable(false)]
	protected IUserInterfaceServices CommandUserInterfaceServices {
		get {
			return _commandUserInterfaceServices ?? this;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public WinFormsApplicationScreen ActiveScreen {
		get {
			return ScreenHost.ActiveScreen;
		}
		set {
			if (value != null)
				ScreenHost.ShowScreen(value);
			else if (ActiveScreen != null)
				ScreenHost.CloseScreen(ActiveScreen);
		}
	}

	[DefaultValue(ToolStripStableHeightBehavior.StandardLogicalHeight), Category("Layout"), Description("Common application toolbar row height in logical pixels; screen command changes do not resize the row")]
	public int ToolBarRowHeight {
		get {
			return _toolStripHeight.LogicalHeight;
		}
		set {
			Guard.ArgumentGT(value, 0, nameof(value));
			_toolStripHeight.LogicalHeight = value;
		}
	}

	public override string Status {
		get {
			return _statusLabel.Text;
		}
		set {
			ExecuteInUIFriendlyContext(() => _statusLabel.Text = value);
		}
	}

	[Browsable(false)]
	protected ToolStripMenuItem HelpToolStripMenuItem {
		get {
			return _helpToolStripMenuItem;
		}
	}

	/// <summary>The actual inherited Options header; derived designers append their native option controls here.</summary>
	[Browsable(false)]
	protected ToolStripMenuItem OptionsToolStripMenuItem {
		get {
			return _optionsMenuItem;
		}
	}

	[Browsable(false)]
	protected ToolStripMenuItem ScreensToolStripMenuItem {
		get {
			return _screensMenuItem;
		}
	}

	[Browsable(false)]
	protected ToolStrip ToolStrip {
		get {
			return _toolStrip;
		}
	}

	[Browsable(false)]
	protected MenuStrip MenuStrip {
		get {
			return _menuStrip;
		}
	}

	[Browsable(false)]
	protected StatusStrip StatusStrip {
		get {
			return _statusStrip;
		}
	}

	#endregion

	#region Form lifetime

	// Designer controls supply the commands; service-dependent availability is applied after runtime attachment.
	protected override void OnLoad(EventArgs e) {
		if (!Tools.Runtime.IsDesignMode) {
			_screenOptionsMenuItem.Available = Application != null || ApplicationServices != null;
			_optionsMenuItem.Available = _optionsMenuItem.DropDownItems.Cast<ToolStripItem>().Any(item => item.Available);
			RebuildApplicationCommands();
		}
		base.OnLoad(e);
	}

	// Application shutdown retains the existing root-host screen guards.
	protected override void OnApplicationExiting(CancelEventArgs args) {
		base.OnApplicationExiting(args);
		if (!args.Cancel && !ScreenExitPermissionsChecked)
			args.Cancel = !ScreenHost.CanCloseScreens(ScreenHost.Screens);
	}

	#endregion

	#region Commands

	/// <summary>Rebuilds the current window's presentation from its command owners and their fixed definitions.</summary>
	protected virtual void RebuildApplicationCommands() {
		if (_rebuildingCommands || IsDisposed || Disposing)
			return;
		_rebuildingCommands = true;
		using var rebuild = Tools.Scope.ExecuteOnDispose(() => _rebuildingCommands = false);
		using var rendering = this.EnterRenderingScope();

		// Activate the form's own definitions before contextual items enter its visible strips.
		CaptureApplicationCommands();
		UnmergeApplicationCommands();
		WinFormsCommandControls.MergeSources(this, MenuStrip, ToolStrip,
			Menus.Where(menu => menu is not NativeToolStripMenu), ToolBarItems.Where(item => item is not NativeToolStripCommand), this, this);
		if (Application is WinFormsApplication application)
			WinFormsCommandControls.MergeSources(this, MenuStrip, ToolStrip,
				application.AdditionalMenus, application.AdditionalToolBarItems, application, this);

		// Shared plugin/block definitions create window-owned views; the screen retains its actual native source controls.
		WinFormsCommandControls.MergeSources(this, MenuStrip, ToolStrip, GetContextCommandProviders(), this);
		ActiveScreen?.MergeCommandsInto(MenuStrip, ToolStrip);
		KeepHelpMenuLast();
	}

	// Return sources in reverse composition order before changing owner, docking, or disposing the window.
	protected void UnmergeApplicationCommands() {
		ScreenHost?.ActiveScreen?.UnmergeCommands();
		WinFormsCommandControls.ClearSources(this);
	}

	/// <summary>Supplies only the visible active screen's plugin and block, independently of sidebar selection.</summary>
	protected virtual IEnumerable<IApplicationCommandProvider> GetContextCommandProviders() {
		var block = ActiveScreen?.Block;
		if (block?.Source != null)
			yield return block.Source;
		if (block != null)
			yield return block;
	}

	/// <summary>Creates this form's own menu definitions after its derived designer has initialized.</summary>
	protected virtual IWinFormsApplicationMenu[] GetMenus() {
		MergeInheritedDesignerStrips();
		return MenuStrip.Items.OfType<ToolStripMenuItem>()
			.Select(item => (IWinFormsApplicationMenu)new NativeToolStripMenu(item, child => child.Tag is not WinFormsApplicationScreen)).ToArray();
	}

	/// <summary>Creates this form's own toolbar definitions after its derived designer has initialized.</summary>
	protected virtual IWinFormsApplicationMenuItem[] GetToolBarItems() {
		MergeInheritedDesignerStrips();
		return ToolStrip.Items.Cast<ToolStripItem>().Select(item => (IWinFormsApplicationMenuItem)NativeToolStripCommand.Create(item)).ToArray();
	}

	// Existing application construction activates both owner caches before any contextual merge.
	internal void CaptureApplicationCommands() {
		_ = Menus;
		_ = ToolBarItems;
	}

	// Preserve designer callbacks while supplying the invoking window to context-aware owner handlers.
	internal void ExecuteNativeCommand(Action command, IUserInterfaceServices userInterface) {
		var previous = _commandUserInterfaceServices;
		_commandUserInterfaceServices = userInterface;
		try {
			command();
		} finally {
			_commandUserInterfaceServices = previous;
		}
	}

	// Screen navigation alone rebuilds its membership on opening; other owner commands remain fixed after activation.
	internal bool IsScreenNavigationMenu(ToolStripItem item) {
		return ReferenceEquals(item, _screensMenuItem);
	}

	// Visual-inheritance sources belong to the form; hosted screens and generated contextual views have separate owners.
	private void MergeInheritedDesignerStrips() {
		foreach (var control in EnumerateApplicationControls(this)) {
			if (control is MergeableMenuStrip menu && ReferenceEquals(menu.InheritedToolStrip, MenuStrip) && menu.Items.Count > 0)
				menu.PerformLayout();
			else if (control is MergeableToolStrip toolbar && ReferenceEquals(toolbar.InheritedToolStrip, ToolStrip) && toolbar.Items.Count > 0)
				toolbar.PerformLayout();
		}
	}

	// Inspect the inherited form surface without entering independent hosted screens or generated sources.
	private IEnumerable<Control> EnumerateApplicationControls(Control parent) {
		foreach (Control child in parent.Controls) {
			if (ReferenceEquals(child, ScreenHost) || WinFormsCommandControls.IsCommandSource(child))
				continue;
			yield return child;
			foreach (var descendant in EnumerateApplicationControls(child))
				yield return descendant;
		}
	}

	// Preserve the existing application menu convention after native merge operations.
	protected virtual void KeepHelpMenuLast() {
		if (HelpToolStripMenuItem == null || HelpToolStripMenuItem.IsDisposed || !MenuStrip.Items.Contains(HelpToolStripMenuItem)
			|| MenuStrip.Items.IndexOf(HelpToolStripMenuItem) == MenuStrip.Items.Count - 1)
			return;
		MenuStrip.Items.Remove(HelpToolStripMenuItem);
		MenuStrip.Items.Add(HelpToolStripMenuItem);
	}

	#endregion

	#region Screen presentation

	/// <summary>Shows the supplied screen through this form's existing host and activation policy.</summary>
	public bool ShowScreen(WinFormsApplicationScreen screen) {
		return ScreenHost.ShowScreen(screen);
	}

	// The outgoing owner restores its source controls before the root host changes active selection.
	protected virtual void OnActiveScreenChanging(WinFormsApplicationScreen screen) {
		if (screen != null) {
			screen.NativeCommandsChanged -= ActiveScreenCommandsChanged;
			screen.ScreenDestroyed -= ActiveCommandScreenDestroyed;
			screen.UnmergeCommands();
		}
		WinFormsCommandControls.ClearSources(this);
	}

	// A window composes only its own active screen's context, independently of other detached windows.
	protected virtual void OnActiveScreenChanged(WinFormsApplicationScreen screen) {
		if (screen != null) {
			screen.NativeCommandsChanged += ActiveScreenCommandsChanged;
			screen.ScreenDestroyed += ActiveCommandScreenDestroyed;
		}
		RebuildApplicationCommands();
	}

	// Explicit pre-activation screen configuration changes reuse the same owner-driven composition path.
	private void ActiveScreenCommandsChanged() {
		RebuildApplicationCommands();
	}

	// Return the active owner's native sources before its normal screen teardown.
	private void ActiveCommandScreenDestroyed(object sender, EventArgs args) {
		if (ReferenceEquals(sender, ActiveScreen))
			OnActiveScreenChanging(ActiveScreen);
	}

	// Read presented instances from their host instead of retaining a second list of generated menu items.
	private void RebuildScreensMenu(WinFormsApplicationScreen selectedScreen = null) {
		foreach (var item in _screensMenuItem.DropDownItems.OfType<ToolStripMenuItem>().Where(item => item.Tag is WinFormsApplicationScreen).ToArray())
			item.Dispose();
		var screens = ScreenHost.OpenScreens;
		var selected = selectedScreen ?? GetScreensMenuSelection();
		var titleCounts = new Dictionary<string, int>(StringComparer.Ordinal);
		var insertionIndex = 0;
		foreach (var screen in screens) {
			var title = string.IsNullOrWhiteSpace(screen.Title) ? screen.GetType().Name : screen.Title;
			if (!string.IsNullOrWhiteSpace(screen.Block?.Name))
				title = $"{screen.Block.Name} — {title}";
			titleCounts.TryGetValue(title, out var occurrence);
			titleCounts[title] = ++occurrence;
			var caption = occurrence > 1 ? $"{title} ({occurrence})" : title;
			if (ScreenHost.IsScreenUndocked(screen))
				caption += " (Undocked)";
			var item = new ToolStripMenuItem(caption) { Tag = screen, Checked = ReferenceEquals(screen, selected), Image = screen.Block?.Image32x32 };
			item.Click += OpenScreenMenuClicked;
			_screensMenuItem.DropDownItems.Insert(insertionIndex++, item);
		}
		_noOpenScreensMenuItem.Visible = screens.Length == 0;
		_closeAllScreensMenuItem.Enabled = screens.Any(screen => screen.ActivationMode != ScreenActivationMode.PermanentSingleton);
		_closeOtherScreensMenuItem.Enabled = selected != null && screens.Any(screen => !ReferenceEquals(screen, selected)
			&& screen.ActivationMode != ScreenActivationMode.PermanentSingleton);
	}

	// An inspected detached instance remains the menu's explicit target without changing the docked selection.
	private WinFormsApplicationScreen GetScreensMenuSelection() {
		return _menuSelectedScreen != null && ScreenHost.OpenScreens.Contains(_menuSelectedScreen, ReferenceEqualityComparer.Instance)
			&& ScreenHost.IsScreenUndocked(_menuSelectedScreen) ? _menuSelectedScreen : ActiveScreen;
	}

	#endregion

	#region IUserInterfaceServices

	event EventHandlerEx IUserInterfaceServices.Changed {
		add {
			ScreenHost.Changed += value;
		}
		remove {
			ScreenHost.Changed -= value;
		}
	}

	IApplicationScreenHost IUserInterfaceServices.ScreenHost {
		get {
			return ScreenHost;
		}
	}

	public virtual Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) {
		return WinFormsCommandControls.ExecuteMenuItemAsync(this, item, cancellationToken);
	}

	#endregion

	#region Event handlers

	private void ScreensMenuOpening(object sender, EventArgs args) {
		var userInterface = CommandUserInterfaceServices;
		RebuildScreensMenu(ReferenceEquals(userInterface, this) ? null : userInterface.ScreenHost?.ActiveScreen as WinFormsApplicationScreen);
	}

	private void ScreensChanged() {
		if (!IsDisposed && !Disposing && !_screensMenuItem.DropDown.Visible)
			RebuildScreensMenu();
	}

	// Await the real modal dialog and report a failed optional load without blocking the UI thread.
	private async void ScreenOptionsClick(object sender, EventArgs e) {
		var userInterface = CommandUserInterfaceServices;
		try {
			await ShowScreenOptionsFromAsync(userInterface);
		} catch (Exception error) {
			ReportError(error);
		}
	}

	private async void OpenScreenMenuClicked(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (sender is ToolStripMenuItem { Tag: WinFormsApplicationScreen screen } && ScreenHost.OpenScreens.Contains(screen, ReferenceEqualityComparer.Instance)
				&& await ScreenHost.ShowScreenAsync(screen)) {
				_menuSelectedScreen = screen;
				RebuildScreensMenu();
			}
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void CloseAllScreensClicked(object sender, EventArgs args) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await ScreenHost.CloseAllScreensAsync();
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	private async void CloseOtherScreensClicked(object sender, EventArgs args) {
		var userInterface = CommandUserInterfaceServices;
		var screen = ReferenceEquals(userInterface, this) ? GetScreensMenuSelection()
			: userInterface.ScreenHost?.ActiveScreen as WinFormsApplicationScreen;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (screen != null)
				await ScreenHost.CloseAllOtherScreensAsync(screen);
		});
		if (error != null)
			await ExceptionDialog.ShowAsync(this, error);
	}

	protected virtual async void RequestAFeature_Click(object sender, EventArgs e) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await ShowRequestFeatureDialog();
		});
		if (error != null) {
			ReportError(error);
		}
	}

	protected virtual async void SendComment_Click(object sender, EventArgs e) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await ShowSendCommentDialog();
		});
		if (error != null) {
			ReportError(error);
		}
	}

	protected virtual async void ReportABug_Click(object sender, EventArgs e) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await ShowSubmitBugReportDialog();
		});
		if (error != null) {
			ReportError(error);
		}
	}

	protected virtual async void About_Click(object sender, EventArgs e) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			await ShowAboutBox();
		});
		if (error != null) {
			ReportError(error);
		}
	}

	protected virtual void ContextHelp_Click(object sender, EventArgs e) {
		var screen = CommandUserInterfaceServices.ScreenHost?.ActiveScreen;
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			var helpServices = Sphere10Framework.Instance.ServiceProvider.GetService<IHelpServices>();
			if (screen != null)
				helpServices.ShowContextHelp(screen);
			else
				helpServices.ShowHelp();
		}, out var error);
		if (error != null) {
			ReportError(error);
		}
	}

	protected virtual void UserGuide_Click(object sender, EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			var helpServices = Sphere10Framework.Instance.ServiceProvider.GetService<IHelpServices>();
			helpServices.ShowHelp();
		}, out var error);
		if (error != null) {
			ReportError(error);
		}
	}

	protected virtual void Exit_Click(object sender, EventArgs e) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			Exit(false);
		}, out var error);
		if (error != null) {
			ReportError(error);
		}
	}

	protected virtual void MainForm_HelpRequested(object sender, HelpEventArgs hlpevent) {
		Tools.Exceptions.ExecuteCapturingException<Exception>(() => {
			var helpServices = Sphere10Framework.Instance.ServiceProvider.GetService<IHelpServices>();
			helpServices.ShowHelp();
		}, out var error);
		if (error != null) {
			ReportError(error);
		}
	}

	#endregion
}
