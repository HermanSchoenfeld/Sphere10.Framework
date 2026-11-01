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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.Components.BlockFramework;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Application UIs can be presented as a screen. It is a proxy ApplicationServiceProvider
/// routing to parent provider. 
/// 
/// NOTE: The ApplicationServiceProvider property, which defines the underlying provider all
/// such  calls are routed to, is guaranteed to be set post-construction.
/// </summary>
public class WinFormsApplicationScreen : ApplicationControl, IWinFormsApplicationScreen, IApplicationSettingsParticipant {
	public event EventHandler ScreenLoaded;
	public event EventHandler ScreenDisplayed;
	public event EventHandler ScreenDisplayedFirstTime;
	public event EventHandler<HideScreenEventArgs> ScreenHidden;
	public event EventHandler ScreenDestroyed;

	private int _showCount;
	private bool _destroyed;
	private ScreenActivationMode _activationMode = ScreenActivationMode.MultiInstance;
	private readonly List<ToolStripItem> _menuStripItems;
	private ToolStrip _toolBar;
	private ToolStripStableHeightBehavior _toolBarHeight;
	private WinFormsApplicationScreenSettings _settings;
	private ApplicationSettingsProfile _settingsProfile;

	public WinFormsApplicationScreen()
		: this(null) {
	}

	public WinFormsApplicationScreen(IWinFormsApplicationBlock applicationBlock) {
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
		get => _activationMode;
		protected set {
			Tools.UI.ValidateActivationMode(value);
			Guard.Ensure(Host == null, "A screen's activation mode cannot change while it belongs to a host");
			_activationMode = value;
		}
	}

	[Browsable(true), Category("Appearance"), Description("The title displayed in the screen tab and detached window")]
	public string Title {
		get => Text;
		set => Text = value;
	}

	/// <summary>A borrowed image supplied before hosting, with registered menu activator images used as the fallback.</summary>
	[Browsable(true), Category("Appearance"), DefaultValue(null), Description("The borrowed image used for this screen's tab. Set before the screen is first hosted.")]
	public Image ScreenImage { get; set; }

	/// <summary>Whether the registered screen can be reopened without session-specific launch state.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual bool CanRestoreAtStartup => true;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IWinFormsApplicationScreenHost Host { get; set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IWinFormsApplicationBlock Block { get; set; }

	[Browsable(false)]
	public IWinFormsApplicationBlock ApplicationBlock {
		get => Block;
		set => Block = value;
	}

	/// <summary>The registered presentation role. Empty screens occupy the workspace without a tab.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ScreenKind ScreenKind { get; internal set; }

	/// <summary>The owning host, for operations on this screen instance.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	protected IApplicationScreenHost ApplicationScreenHost => Host;

	/// <summary>
	/// The menu items associated with this screen.
	/// </summary>
	[Browsable(false)]
	public ToolStripItem[] MenuItems => _menuStripItems.ToArray();

	/// <summary>
	/// The toolbar associated with this screen.
	/// </summary>
	[Browsable(true), Category("Behavior"), Description("The toolbar associated with this screen.")]
	public ToolStrip ToolBar {
		get => _toolBar;
		set {
			if (ReferenceEquals(_toolBar, value))
				return;
			_toolBarHeight?.Dispose();
			_toolBar = value;
			_toolBarHeight = value == null ? null : new ToolStripStableHeightBehavior(value);
		}
	}

	/// <summary>Screen commands contributed to the shared application menu layers.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IWinFormsApplicationMenu[] Menus => Array.Empty<IWinFormsApplicationMenu>();

	/// <summary>Screen commands contributed to the shared application toolbar layers.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public virtual IWinFormsApplicationMenuItem[] ToolBarItems => Array.Empty<IWinFormsApplicationMenuItem>();

	public HelpType Type { get; }

	public string FileName { get; }

	public string Url { get; }

	public int? PageNumber { get; }

	public int? HelpTopicID { get; }

	public int? HelpTopicAlias { get; }

	/// <summary>The optional profile or legacy-section restore error, with declared defaults retained for normal screen use.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Exception LastSettingsLoadError => _settings?.LastLoadError ?? _settingsProfile?.LastLoadError;

	/// <summary>The profile's outstanding persistence failure, cleared after a successful accepted retry.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Exception LastSettingsSaveError => _settingsProfile?.LastSaveError;

	/// <summary>Returns this registration's restored preferences before the screen begins its first data work.</summary>
	protected TSettings GetSettings<TSettings>() {
		Guard.Ensure(_settings != null, "Declare typed settings on this screen registration before reading preferences.");
		return _settings.GetValues<TSettings>();
	}

	/// <summary>Binds a grid to one public property on the ordinary settings object.</summary>
	protected Task BindGridPreferencesAsync(VisualGrid grid, string propertyName, CancellationToken cancellationToken = default) {
		Guard.Ensure(_settings != null, "Declare settings before binding a screen-owned grid.");
		return _settings.BindGridAsync(grid, propertyName, null, cancellationToken);
	}

	/// <summary>Binds one named grid in a settings dictionary without repeated accessor delegates.</summary>
	protected Task BindGridPreferencesAsync(VisualGrid grid, string propertyName, string role, CancellationToken cancellationToken = default) {
		Guard.Ensure(_settings != null, "Declare settings before binding a screen-owned grid.");
		return _settings.BindGridAsync(grid, propertyName, role, cancellationToken);
	}

	/// <summary>Binds remembered output to one public property on the ordinary settings object.</summary>
	protected Task BindLogOutputPreferencesAsync(LogOutputSelectorControl selector, string propertyName, CancellationToken cancellationToken = default) {
		Guard.Ensure(_settings != null, "Declare settings before binding screen-owned logging.");
		return _settings.BindLogOutputAsync(selector, propertyName, cancellationToken);
	}

	/// <summary>Binds one declared grid section and imports its legacy record once without writing during display.</summary>
	protected Task BindGridPreferencesAsync<TSettings>(VisualGrid grid, string sectionPath, Func<TSettings, VisualGridSettings> get,
		Action<TSettings, VisualGridSettings> set, CancellationToken cancellationToken = default) {
		Guard.Ensure(_settings != null, "Declare typed settings before binding a screen-owned grid.");
		return _settings.BindGridAsync(grid, sectionPath, get, set, cancellationToken);
	}

	/// <summary>Binds remembered logger values to the screen profile without starting output during restoration.</summary>
	protected Task BindLogOutputPreferencesAsync<TSettings>(LogOutputSelectorControl selector, string sectionPath,
		Func<TSettings, LogOutputSelectorPreferences> get, Action<TSettings, LogOutputSelectorPreferences> set, CancellationToken cancellationToken = default) {
		Guard.Ensure(_settings != null, "Declare typed settings before binding screen-owned logging preferences.");
		return _settings.BindLogOutputAsync(selector, sectionPath, get, set, cancellationToken);
	}

	/// <summary>Marks deliberate edits to the detached typed values for accepted close or an explicit awaited save.</summary>
	protected void MarkSettingsChanged() {
		Guard.Ensure(_settings != null, "Declare typed settings before changing preferences.");
		_settings.MarkChanged();
	}

	/// <summary>Captures accepted native preferences before close or application shutdown; disposal never saves.</summary>
	public virtual async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		if (_settings != null)
			await _settings.SaveAsync(cancellationToken);
		foreach (var grid in GetChildGrids(this))
			await grid.FlushPreferencesAsync();
	}

	// Scalar options can update their existing live owner after successful Options Apply.
	protected virtual void OnSettingsChanged() {
	}

	// The canonical native host attaches profiles before the control can raise Load.
	internal void AttachSettings(ApplicationSettingsProfile profile, ISettingsProvider provider) {
		_settingsProfile = profile;
		if (!profile.HasOptions)
			return;
		Guard.Ensure(_settings == null, "A hosted screen cannot change its settings profile.");
		_settings = new WinFormsApplicationScreenSettings(this, _settingsProfile, provider, (_, _) => NotifySettingsChanged());
	}

	// The frame can select this exact page without inferring a registration from its type or caption.
	internal ApplicationSettingsProfile SettingsProfile => _settingsProfile;

	// Deliver scalar preference updates through the screen's existing UI owner.
	internal void NotifySettingsChanged() => OnSettingsChanged();

	// Standalone child grids retain their existing persistence owner and ordered flush contract.
	private static IEnumerable<VisualGrid> GetChildGrids(Control owner) {
		foreach (Control child in owner.Controls) {
			if (child is VisualGrid grid)
				yield return grid;
			foreach (var nested in GetChildGrids(child))
				yield return nested;
		}
	}

	public override void SetLocalizedText(CultureInfo culture = null) {
		base.SetLocalizedText(culture);
		SetLocalizedTextInApplicationControls(this.Controls);
	}

	protected virtual void OnShowFirstTime() {
	}

	protected override void OnLoad(EventArgs Args) {
		base.OnLoad(Args);
		ScreenLoaded?.Invoke(this, EventArgs.Empty);
	}

	protected override void Dispose(bool Disposing) {
		if (Disposing && !_destroyed) {
			_settings?.Dispose();
			NotifyScreenDestroyed();
			foreach (var Item in _menuStripItems)
				Item.Dispose();
			ToolBar?.Dispose();
			_toolBarHeight?.Dispose();
		}
		base.Dispose(Disposing);
	}

	protected virtual void OnShow() {
		if (_showCount++ == 0)
			NotifyShowScreenFirstTime();
	}

	protected virtual void OnHide(ref bool cancelHide) {
	}

	protected virtual void OnDestroyScreen() {
	}

	protected void RegisterMenuItem(ToolStripItem item) {
		_menuStripItems.Add(item);
	}

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

	bool IWinFormsApplicationScreen.CanDeactivate() {
		var cancel = false;
		NotifyHideScreen(ref cancel);
		return !cancel;
	}

	void IWinFormsApplicationScreen.OnActivated() => NotifyShow();

	void IWinFormsApplicationScreen.OnDeactivated() {
		// The native hide notification already runs as part of its cancelable guard.
	}

	internal void ConfigureActivationMode(ScreenActivationMode Mode) => ActivationMode = Mode;

	internal void NotifyShow() {
		OnShow();
		ScreenDisplayed?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyShowScreenFirstTime() {
		OnShowFirstTime();
		ScreenDisplayedFirstTime?.Invoke(this, EventArgs.Empty);
	}

	internal void NotifyHideScreen(ref bool cancel) {
		OnHide(ref cancel);
		if (!cancel) {
			var cancelArgs = new HideScreenEventArgs();
			ScreenHidden?.Invoke(this, cancelArgs);
			cancel = cancelArgs.Cancel;
		}
	}

	internal void NotifyScreenDestroyed() {
		if (_destroyed)
			return;
		_destroyed = true;
		OnDestroyScreen();
		ScreenDestroyed?.Invoke(this, EventArgs.Empty);
	}
}

