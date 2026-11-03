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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.Components.BlockFramework;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Application UIs can be presented as a screen. It is a proxy ApplicationServiceProvider
/// routing to parent provider.
///
/// NOTE: The ApplicationServiceProvider property, which defines the underlying provider all
/// such  calls are routed to, is guaranteed to be set post-construction.
/// </summary>
public class ApplicationScreen : ApplicationControl, IApplicationScreen {
	public event EventHandler ScreenLoaded;

	public event EventHandler ScreenDisplayed;

	public event EventHandler ScreenDisplayedFirstTime;

	public event EventHandler<HideScreenEventArgs> ScreenHidden;

	public event EventHandler ScreenDestroyed;

	internal event EventHandlerEx NativeCommandsChanged;

	private int _showCount;

	private bool _destroyed;

	private ScreenActivationMode _activationMode = ScreenActivationMode.MultiInstance;

	private readonly List<ToolStripItem> _menuStripItems;

	private ToolStrip _screenToolBar;

	private ToolStripStableHeightBehavior _screenToolBarHeight;

	private MenuStrip _screenMenuStrip;

	private IApplicationMenu[] _menus;

	private IApplicationMenuItem[] _toolBarItems;

	private MenuStrip _registeredMenuStrip;

	private ToolStripVisualInheritanceFixer _menuMerger;

	private ToolStripVisualInheritanceFixer _toolBarMerger;

	/// <summary>Creates a native screen with an optional owning application block.</summary>
	public ApplicationScreen()
		: this(null) {
	}

	/// <summary>Creates a native screen with an optional owning application block.</summary>
	public ApplicationScreen(IApplicationBlock applicationBlock) {
		Block = applicationBlock;
		Url = FileName = null;
		Type = HelpType.None;
		_menuStripItems = new List<ToolStripItem>();
		_showCount = 0;
	}

	[Browsable(true), Category("Appearance")]
	public string ApplicationMenuStripText { get; set; }

	[Browsable(true), Category("Appearance")]
	public bool ShowInApplicationMenuStrip { get; set; }

	[Browsable(true), Category("Layout"), Description("How this screen will be displayed to the user")]
	public ScreenDisplayMode DisplayMode { get; set; }

	/// <summary>Defaults to MultiInstance unless the constructor or this registration declares another policy.</summary>
	[Browsable(true), Category("Behavior"), DefaultValue(ScreenActivationMode.MultiInstance), Description("The instance policy declared by this screen type")]
	public ScreenActivationMode ActivationMode {
		get {
			return _activationMode;
		}
		protected set {
			Tools.UI.ValidateActivationMode(value);
			Guard.Ensure(Host == null, "A screen's activation mode cannot change while it belongs to a host");
			_activationMode = value;
		}
	}

	[Browsable(true), Category("Appearance"), Description("The title displayed in the screen tab and detached window")]
	public string Title {
		get {
			return Text;
		}
		set {
			Text = value;
		}
	}

	/// <summary>A borrowed image supplied before hosting, with registered menu activator images used as the fallback.</summary>
	[Browsable(true), Category("Appearance"), DefaultValue(null), Description("The borrowed image used for this screen's tab. Set before the screen is first hosted.")]
	public Image ScreenImage { get; set; }

	/// <summary>Whether the registered screen can be reopened without session-specific launch state.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual bool CanRestoreAtStartup {
		get {
			return true;
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IApplicationScreenHost Host { get; set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IApplicationBlock Block { get; set; }

	[Browsable(false)]
	public IApplicationBlock ApplicationBlock {
		get {
			return Block;
		}
		set {
			Block = value;
		}
	}

	/// <summary>The registered presentation role. Empty screens occupy the workspace without a tab.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ScreenKind ScreenKind { get; internal set; }

	/// <summary>The actual menu items supplied by this screen.</summary>
	[Browsable(false)]
	public ToolStripItem[] MenuItems {
		get {
			return ScreenMenuStrip == null ? _menuStripItems.ToArray() : Menus.OfType<NativeToolStripMenu>().Select(menu => (ToolStripItem)menu.Item).ToArray();
		}
	}

	/// <summary>The screen-owned designer menu strip merged while this screen is active.</summary>
	/// <remarks>Assign during configuration. Its original callbacks, state and images remain on the native items.</remarks>
	[Browsable(true), Category("Behavior"), Description("The screen-owned MenuStrip whose actual items are merged while this screen is active.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public MenuStrip ScreenMenuStrip {
		get {
			return _screenMenuStrip;
		}
		set {
			if (ReferenceEquals(_screenMenuStrip, value))
				return;
			EnsureCommandsNotActivated();
			_screenMenuStrip = value;
			NativeCommandsChanged?.Invoke();
		}
	}

	/// <summary>The screen-owned designer toolbar merged while this screen is active.</summary>
	/// <remarks>Assign during configuration. The merge control restores the same native items on deactivation.</remarks>
	[Browsable(true), Category("Behavior"), Description("The screen-owned ToolStrip whose actual items are merged while this screen is active.")]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public ToolStrip ScreenToolBar {
		get {
			return _screenToolBar;
		}
		set {
			if (ReferenceEquals(_screenToolBar, value))
				return;
			EnsureCommandsNotActivated();
			_screenToolBarHeight?.Dispose();
			_screenToolBar = value;
			_screenToolBarHeight = value == null ? null : new ToolStripStableHeightBehavior(value);
			NativeCommandsChanged?.Invoke();
		}
	}

	/// <summary>Activates and caches this screen's fixed command membership.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IApplicationMenu[] Menus {
		get {
			return Tools.Array.Clone(_menus ??= GetMenus());
		}
	}

	/// <summary>Activates and caches the screen's toolbar commands; native item state remains live.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IApplicationMenuItem[] ToolBarItems {
		get {
			return Tools.Array.Clone(_toolBarItems ??= GetToolBarItems());
		}
	}

	public HelpType Type { get; }

	public string FileName { get; }

	public string Url { get; }

	public int? PageNumber { get; }

	public int? HelpTopicID { get; }

	public int? HelpTopicAlias { get; }

	/// <summary>Override with the ordinary settings type to declare this screen's preferences without constructing its control.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual SettingsObject ScreenSettings {
		get {
			return DeclaredSettings;
		}
	}

	/// <summary>The owning host, for operations on this screen instance.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected SharedUI.IApplicationScreenHost ApplicationScreenHost {
		get {
			return Host;
		}
	}

	// The legacy registration API owns its source strip just as a designer-authored screen does.
	internal MenuStrip CommandMenuStrip {
		get {
			_ = Menus;
			return ScreenMenuStrip ?? _registeredMenuStrip;
		}
	}

	/// <summary>Creates this screen's menu definitions once, after its designer configuration is complete.</summary>
	protected virtual IApplicationMenu[] GetMenus() {
		var source = ScreenMenuStrip;
		if (source == null && _menuStripItems.Count > 0 && ShowInApplicationMenuStrip) {
			_registeredMenuStrip = new MenuStrip();
			var header = new ToolStripMenuItem(ApplicationMenuStripText);
			header.DropDownItems.AddRange(_menuStripItems.ToArray());
			_registeredMenuStrip.Items.Add(header);
			source = _registeredMenuStrip;
		}
		return source == null ? Array.Empty<IApplicationMenu>() : source.Items.OfType<ToolStripMenuItem>()
			.Select(item => (IApplicationMenu)new NativeToolStripMenu(item, _ => true)).ToArray();
	}

	/// <summary>Creates toolbar definitions from this screen's original native controls.</summary>
	protected virtual IApplicationMenuItem[] GetToolBarItems() {
		return ScreenToolBar == null ? Array.Empty<IApplicationMenuItem>() : ScreenToolBar.Items.Cast<ToolStripItem>()
			.Select(item => (IApplicationMenuItem)NativeToolStripCommand.Create(item)).ToArray();
	}

	// Merge bookkeeping belongs to the reusable merge control, while this screen owns the native sources.
	internal void MergeCommandsInto(MenuStrip menuStrip, ToolStrip toolBar) {
		_ = Menus;
		_ = ToolBarItems;
		var sourceMenu = CommandMenuStrip;
		if (sourceMenu != null && menuStrip != null && !ReferenceEquals(sourceMenu, menuStrip)) {
			_menuMerger ??= new ToolStripVisualInheritanceFixer { ToolStrip = sourceMenu };
			_menuMerger.MergeInto(menuStrip, matchMenuText: true);
		}
		if (ScreenToolBar != null && toolBar != null && !ReferenceEquals(ScreenToolBar, toolBar)) {
			_toolBarMerger ??= new ToolStripVisualInheritanceFixer { ToolStrip = ScreenToolBar };
			_toolBarMerger.MergeInto(toolBar);
		}
	}

	// Return the source items before another window or the screen's disposal uses them.
	internal void UnmergeCommands() {
		_toolBarMerger?.Unmerge();
		_menuMerger?.Unmerge();
	}

	// Activate definitions before a detached window borrows originals; later provider reads cannot reparent its visible commands.
	internal bool ActivateDetachedCommands() {
		var header = _registeredMenuStrip?.Items.OfType<ToolStripMenuItem>().FirstOrDefault();
		var restoreSource = header != null && _menuStripItems.Any(item => ReferenceEquals(item.Owner, header.DropDown));
		_ = Menus;
		_ = ToolBarItems;
		return restoreSource;
	}

	// Return still-borrowed registrations to their previous grouped source, leaving standalone items unparented as before.
	internal void RestoreRegisteredMenuItems(MenuStrip detachedMenu, bool restoreSource) {
		var header = restoreSource ? _registeredMenuStrip?.Items.OfType<ToolStripMenuItem>().FirstOrDefault() : null;
		foreach (var item in _menuStripItems.Where(item => ReferenceEquals(item.Owner, detachedMenu))) {
			detachedMenu.Items.Remove(item);
			header?.DropDownItems.Add(item);
		}
	}

	/// <summary>Reads the actual settings instance owned by this screen's block.</summary>
	protected TSettings GetBlockSettings<TSettings>()
		where TSettings : SettingsObject {
		Guard.Ensure(Block?.BlockSettings != null, "Declare block settings before reading them.");
		return Guard.ArgumentCast<TSettings>(Block.BlockSettings, nameof(TSettings));
	}

	/// <summary>Reads the actual settings instance owned by this screen's contributing plugin.</summary>
	protected TSettings GetPluginSettings<TSettings>()
		where TSettings : SettingsObject {
		Guard.Ensure(Block?.Source?.PluginSettings != null, "Declare plugin settings before reading them.");
		return Guard.ArgumentCast<TSettings>(Block.Source.PluginSettings, nameof(TSettings));
	}

	/// <summary>Binds a grid to one public property on the ordinary settings object.</summary>
	protected Task BindGridPreferencesAsync(VisualGrid grid, string propertyName, CancellationToken cancellationToken = default) {
		Guard.Ensure(SettingsBinding != null, "Declare settings before binding a screen-owned grid.");
		return SettingsBinding.BindGridAsync(grid, propertyName, null, cancellationToken);
	}

	/// <summary>Binds one named grid in a settings dictionary without repeated accessor delegates.</summary>
	protected Task BindGridPreferencesAsync(VisualGrid grid, string propertyName, string role, CancellationToken cancellationToken = default) {
		Guard.Ensure(SettingsBinding != null, "Declare settings before binding a screen-owned grid.");
		return SettingsBinding.BindGridAsync(grid, propertyName, role, cancellationToken);
	}

	/// <summary>Binds remembered output to one public property on the ordinary settings object.</summary>
	protected Task BindLogOutputPreferencesAsync(LogOutputSelectorControl selector, string propertyName, CancellationToken cancellationToken = default) {
		Guard.Ensure(SettingsBinding != null, "Declare settings before binding screen-owned logging.");
		return SettingsBinding.BindLogOutputAsync(selector, propertyName, cancellationToken);
	}

	/// <summary>Binds one declared grid section and imports its legacy record once without writing during display.</summary>
	protected Task BindGridPreferencesAsync<TSettings>(VisualGrid grid, string sectionPath, Func<TSettings, VisualGridSettings> get,
		Action<TSettings, VisualGridSettings> set, CancellationToken cancellationToken = default)
		where TSettings : SettingsObject {
		Guard.Ensure(SettingsBinding != null, "Declare typed settings before binding a screen-owned grid.");
		return SettingsBinding.BindGridAsync(grid, sectionPath, get, set, cancellationToken);
	}

	/// <summary>Binds remembered logger values to the actual screen settings without starting output during restoration.</summary>
	protected Task BindLogOutputPreferencesAsync<TSettings>(LogOutputSelectorControl selector, string sectionPath,
		Func<TSettings, LogOutputSelectorPreferences> get, Action<TSettings, LogOutputSelectorPreferences> set, CancellationToken cancellationToken = default)
		where TSettings : SettingsObject {
		Guard.Ensure(SettingsBinding != null, "Declare typed settings before binding screen-owned logging preferences.");
		return SettingsBinding.BindLogOutputAsync(selector, sectionPath, get, set, cancellationToken);
	}

	/// <summary>Marks deliberate edits to the actual typed settings for accepted close or an explicit awaited save.</summary>
	protected void MarkSettingsChanged() {
		Guard.Ensure(SettingsBinding != null, "Declare typed settings before changing preferences.");
		SettingsBinding.MarkChanged();
	}

	/// <summary>Captures accepted native preferences before close or application shutdown; disposal never saves.</summary>
	public virtual async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		CopyUIToModel();
		if (SettingsBinding != null)
			await SettingsBinding.SaveAsync(cancellationToken);
	}

	// The reusable header menu resolves its screen and existing grid binding on demand.
	internal bool CanConfigureGrid(VisualGrid grid) {
		return SettingsBinding?.CanConfigureGrid(grid) == true;
	}

	// Explicit grid configuration saves use the screen's existing settings provider and notification path.
	internal Task SaveGridConfigurationAsync(VisualGrid grid, CancellationToken cancellationToken = default) {
		Guard.Ensure(SettingsBinding != null, "Declare settings before saving a grid configuration.");
		return SettingsBinding.SaveGridConfigurationAsync(grid, cancellationToken);
	}

	// Reset only this grid's live presentation; saving the reset is a separate user action.
	internal Task RestoreGridDefaultsAsync(VisualGrid grid, CancellationToken cancellationToken = default) {
		Guard.Ensure(SettingsBinding != null, "Declare settings before restoring a grid configuration.");
		return SettingsBinding.RestoreGridDefaultsAsync(grid, cancellationToken);
	}

	/// <summary>Applies the selected culture to this screen and its child application controls.</summary>
	public override void SetLocalizedText(CultureInfo culture = null) {
		base.SetLocalizedText(culture);
		SetLocalizedTextInApplicationControls(this.Controls);
	}

	/// <summary>Allows a screen to initialize presentation when first displayed.</summary>
	protected virtual void OnShowFirstTime() {
	}

	/// <summary>Runs the existing control initialization before notifying screen listeners.</summary>
	protected override void OnLoad(EventArgs Args) {
		base.OnLoad(Args);
		ScreenLoaded?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>Releases settings subscriptions and screen-owned native command resources.</summary>
	protected override void Dispose(bool Disposing) {
		if (Disposing && !_destroyed) {
			ReleaseSettings();
			UnmergeCommands();
			NotifyScreenDestroyed();
			_menuMerger?.Dispose();
			_toolBarMerger?.Dispose();

			// Cached descriptor factories own their definitions; native projections retain their no-op disposal contract.
			var menus = (_menus ?? Array.Empty<IApplicationMenu>())
				.Distinct<IApplicationMenu>(ReferenceEqualityComparer.Instance).ToArray();
			var menuItems = new HashSet<IApplicationMenuItem>(menus.SelectMany(menu => menu.Items), ReferenceEqualityComparer.Instance);
			foreach (var menu in menus)
				menu.Dispose();
			foreach (var item in _toolBarItems ?? Array.Empty<IApplicationMenuItem>())
				if (menuItems.Add(item))
					item.Dispose();

			_registeredMenuStrip?.Dispose();
			foreach (var Item in _menuStripItems)
				if (!Item.IsDisposed)
					Item.Dispose();
			ScreenMenuStrip?.Dispose();
			ScreenToolBar?.Dispose();
			_screenToolBarHeight?.Dispose();
		}
		base.Dispose(Disposing);
	}

	/// <summary>Allows a screen to update its presentation when displayed.</summary>
	protected virtual void OnShow() {
		if (_showCount++ == 0)
			NotifyShowScreenFirstTime();
	}

	/// <summary>Allows a screen to veto deactivation through its existing native hook.</summary>
	protected virtual void OnHide(ref bool cancelHide) {
	}

	/// <summary>Allows a screen to release resources before its native destruction notification.</summary>
	protected virtual void OnDestroyScreen() {
	}

	/// <summary>Adds a legacy native menu item before command membership activates.</summary>
	protected void RegisterMenuItem(ToolStripItem item) {
		EnsureCommandsNotActivated();
		_menuStripItems.Add(item);
	}

	// Runs the existing hide guard without changing screen ownership.
	bool IApplicationScreen.CanDeactivate() {
		var cancel = false;
		NotifyHideScreen(ref cancel);
		return !cancel;
	}

	// Runs this screen's existing display notification.
	void IApplicationScreen.OnActivated() {
		NotifyShow();
	}

	// Runs this screen's existing hide notification.
	void IApplicationScreen.OnDeactivated() {
		// The native hide notification already runs as part of its cancelable guard.
	}

	// Sets registration policy before the screen becomes hosted.
	internal void ConfigureActivationMode(ScreenActivationMode Mode) {
		ActivationMode = Mode;
	}

	// Notifies display listeners and the existing screen presentation hook.
	internal void NotifyShow() {
		OnShow();
		ScreenDisplayed?.Invoke(this, EventArgs.Empty);
	}

	// Notifies first-display listeners and the corresponding presentation hook.
	internal void NotifyShowScreenFirstTime() {
		OnShowFirstTime();
		ScreenDisplayedFirstTime?.Invoke(this, EventArgs.Empty);
	}

	// Runs the hide guard and notifies listeners when the screen may be hidden.
	internal void NotifyHideScreen(ref bool cancel) {
		OnHide(ref cancel);
		if (!cancel) {
			var cancelArgs = new HideScreenEventArgs();
			ScreenHidden?.Invoke(this, cancelArgs);
			cancel = cancelArgs.Cancel;
		}
	}

	// Runs destruction once and releases settings subscriptions before callbacks.
	internal void NotifyScreenDestroyed() {
		if (_destroyed)
			return;
		_destroyed = true;
		OnDestroyScreen();
		ScreenDestroyed?.Invoke(this, EventArgs.Empty);
	}

	// Command membership is configured before activation; native appearance and checked/enabled state remain mutable.
	private void EnsureCommandsNotActivated() {
		Guard.Ensure(_menus == null && _toolBarItems == null, "Command membership is read-only after activation.");
	}

	// Recurses through child application controls to apply the selected culture.
	private void SetLocalizedTextInApplicationControls(ControlCollection controls) {
		if (controls != null) {
			foreach (Control control in controls) {
				if (control is ApplicationControl) {
					((ApplicationControl)control).SetLocalizedText();
				}
				SetLocalizedTextInApplicationControls(
					control.Controls
				);
			}
		}
	}
}
