// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.ComponentModel;
using System.Globalization;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms.Components.BlockFramework;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Application UIs can be presented as a screen. It is a proxy ApplicationServiceProvider
/// routing to parent provider. 
/// 
/// NOTE: The ApplicationServiceProvider property, which defines the underlying provider all
/// such  calls are routed to, is guaranteed to be set post-construction.
/// </summary>
public class ApplicationScreen : ApplicationControl, IHelpableObject {
	public event EventHandler ScreenLoaded;
	public event EventHandler ScreenDisplayed;
	public event EventHandler ScreenDisplayedFirstTime;
	public event EventHandler<HideScreenEventArgs> ScreenHidden;
	public event EventHandler ScreenDestroyed;

	private int _showCount;
	private bool _destroyed;
	private ScreenActivationMode _activationMode;
	private readonly List<ToolStripItem> _menuStripItems;

	public ApplicationScreen()
		: this(null) {
	}

	public ApplicationScreen(IApplicationBlock applicationBlock) {
		ApplicationBlock = applicationBlock;
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

	/// <summary>The type's constructor default or explicit builder declaration. Every instance of a hosted type uses the same mode.</summary>
	[Browsable(true), Category("Behavior"), DefaultValue(ScreenActivationMode.SingleInstance), Description("The instance policy declared by this screen type")]
	public ScreenActivationMode ActivationMode {
		get => _activationMode;
		protected set {
			Guard.Argument(value == ScreenActivationMode.SingleInstance || value == ScreenActivationMode.MultiInstance, nameof(value), "Unknown activation mode");
			Guard.Ensure(ScreenHost == null, "A screen's activation mode cannot change while it belongs to a host");
			_activationMode = value;
		}
	}

	[Browsable(true), Category("Appearance"), Description("The title displayed in the screen tab and detached window")]
	public string Title {
		get => Text;
		set => Text = value;
	}

	[Browsable(false)] public IApplicationBlock ApplicationBlock { get; set; }

	internal IApplicationScreenHost? ScreenHost { get; set; }

	/// <summary>
	/// The menu items associated with this screen.
	/// </summary>
	[Browsable(false)]
	public ToolStripItem[] MenuItems => _menuStripItems.ToArray();

	/// <summary>
	/// The toolbar associated with this screen.
	/// </summary>
	[Browsable(true), Category("Behavior"), Description("The toolbar associated with this screen.")]
	public ToolStrip ToolBar { get; set; }

	public HelpType Type { get; }

	public string FileName { get; }

	public string Url { get; }

	public int? PageNumber { get; }

	public int? HelpTopicID { get; }

	public int? HelpTopicAlias { get; }

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
			NotifyScreenDestroyed();
			foreach (var Item in _menuStripItems)
				Item.Dispose();
			ToolBar?.Dispose();
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

