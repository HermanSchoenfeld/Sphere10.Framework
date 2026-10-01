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
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public partial class MainForm : LiteMainForm {
	private readonly List<ToolStripItem> _screenToolBarItems = new();
	private ToolStripMenuItem? _screenMenu;
	private bool _screenToolBarVisible;

	public MainForm() {
		InitializeComponent();
		ScreenHost = new WinFormsApplicationScreenHost { Dock = DockStyle.Fill };
		Controls.Add(ScreenHost);
		ScreenHost.BringToFront();
		ScreenHost.ActiveScreenChanging += OnActiveScreenChanging;
		ScreenHost.ActiveScreenChanged += OnActiveScreenChanged;
	}

	[DefaultValue(ScreenMode.SingleView), Category("Behavior"), Description("Display one screen or multiple screens in detachable tabs")]
	public ScreenMode ScreenMode {
		get => ScreenHost.ScreenMode;
		set => ScreenHost.ScreenMode = value;
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public WinFormsApplicationScreenHost ScreenHost { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public WinFormsApplicationScreen? ActiveScreen {
		get => ScreenHost.ActiveScreen;
		set {
			if (value != null)
				ScreenHost.ShowScreen(value);
			else if (ActiveScreen != null)
				ScreenHost.CloseScreen(ActiveScreen);
		}
	}

	public bool ShowScreen(WinFormsApplicationScreen Screen) => ScreenHost.ShowScreen(Screen);

	protected virtual void OnActiveScreenChanging(WinFormsApplicationScreen? Screen) {
		RestoreScreenToolBar();
		if (_screenMenu != null) {
			_screenMenu.DropDownItems.Clear();
			MenuStrip.Items.Remove(_screenMenu);
			_screenMenu.Dispose();
			_screenMenu = null;
		}
	}

	protected virtual void OnActiveScreenChanged(WinFormsApplicationScreen? Screen) {
		if (Screen == null)
			return;
		if (Screen.ShowInApplicationMenuStrip) {
			_screenMenu = new ToolStripMenuItem(Screen.ApplicationMenuStripText) { Tag = Screen };
			_screenMenu.DropDownItems.AddRange(Screen.MenuItems);
			var HelpIndex = HelpToolStripMenuItem == null ? -1 : MenuStrip.Items.IndexOf(HelpToolStripMenuItem);
			MenuStrip.Items.Insert(HelpIndex < 0 ? MenuStrip.Items.Count : HelpIndex, _screenMenu);
		}
		MergeScreenToolBar();
	}

	protected void RestoreScreenToolBar() {
		if (ActiveScreen?.ToolBar != null && !ActiveScreen.ToolBar.IsDisposed) {
			ActiveScreen.ToolBar.Items.AddRange(_screenToolBarItems.ToArray());
			if (_screenToolBarItems.Count > 0)
				ActiveScreen.ToolBar.Visible = _screenToolBarVisible;
		}
		_screenToolBarItems.Clear();
	}

	protected void MergeScreenToolBar() {
		if (ActiveScreen?.ToolBar == null || _screenToolBarItems.Count > 0)
			return;
		_screenToolBarVisible = ActiveScreen.ToolBar.Visible;
		foreach (ToolStripItem Item in ActiveScreen.ToolBar.Items)
			_screenToolBarItems.Add(Item);
		ActiveScreen.ToolBar.Visible = false;
		ToolStrip.Items.AddRange(_screenToolBarItems.ToArray());
	}

	protected override void OnApplicationExiting(CancelEventArgs Args) {
		base.OnApplicationExiting(Args);
		if (!Args.Cancel)
			Args.Cancel = !ScreenHost.CanCloseScreens(ScreenHost.Screens);
	}

	#region Form Properties

	[Browsable(false)] protected ToolStripMenuItem PurchaseFullVersionToolStripMenuItem { get; private set; }

	[Browsable(false)] protected ToolStripMenuItem HelpToolStripMenuItem { get; private set; }

	[Browsable(false)] protected ToolStrip ToolStrip => _toolStrip;

	[Browsable(false)] protected MenuStrip MenuStrip => _menuStrip;

	[Browsable(false)] protected StatusStrip StatusStrip => _statusStrip;

	#endregion

	#region Form Methods

	protected override void OnFirstActivated() {
		base.OnFirstActivated();
		if (!Tools.Runtime.IsDesignMode && !ApplicationExiting) {
			try {
				var licenseProvider = Sphere10Framework.Instance.ServiceProvider.GetService<IProductLicenseProvider>();
				// Show/Hide register menu item based on what's happened with the user nag screen
				if (licenseProvider.TryGetLicense(out var license) && license.License.Item.FeatureLevel == ProductLicenseFeatureLevelDTO.Free) {
					if (PurchaseFullVersionToolStripMenuItem != null)
						PurchaseFullVersionToolStripMenuItem.Visible = true;
				} else {
					if (PurchaseFullVersionToolStripMenuItem != null)
						PurchaseFullVersionToolStripMenuItem.Visible = false;
				}
			} catch (ProductLicenseTamperedException error) {
				ReportError(error);
				Exit(true);
			}
		}
	}

	#endregion

	#region IUserInterfaceServices Overrides

	public override string Status {
		get => _statusLabel.Text;
		set { ExecuteInUIFriendlyContext(() => _statusLabel.Text = value); }
	}

	#endregion

	#region Event Handlers

	protected virtual async void RequestAFeature_Click(object sender, EventArgs e) {
		try {
			await ShowRequestFeatureDialog();
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual async void SendComment_Click(object sender, EventArgs e) {
		try {
			await ShowSendCommentDialog();
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual async void ReportABug_Click(object sender, EventArgs e) {
		try {
			await ShowSubmitBugReportDialog();
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual async void About_Click(object sender, EventArgs e) {
		try {
			await ShowAboutBox();
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual void ContextHelp_Click(object sender, EventArgs e) {
		try {
			var helpServices = Sphere10Framework.Instance.ServiceProvider.GetService<IHelpServices>();
			helpServices.ShowHelp();
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual void UserGuide_Click(object sender, EventArgs e) {
		try {
			var helpServices = Sphere10Framework.Instance.ServiceProvider.GetService<IHelpServices>();
			helpServices.ShowHelp();
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual async void PurchaseFullVersion_Click(object sender, EventArgs e) {
		try {
			var productLicenseEnforcer = Sphere10Framework.Instance.ServiceProvider.GetService<IProductLicenseEnforcer>();
			productLicenseEnforcer.CalculateRights(out var nagMessage);
			await ShowNagScreen(nagMessage);
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual void Exit_Click(object sender, EventArgs e) {
		try {
			Exit(false);
		} catch (Exception error) {
			ReportError(error);
		}
	}

	protected virtual void MainForm_HelpRequested(object sender, HelpEventArgs hlpevent) {
		try {
			var helpServices = Sphere10Framework.Instance.ServiceProvider.GetService<IHelpServices>();
			helpServices.ShowHelp();
		} catch (Exception error) {
			ReportError(error);
		}
	}

	#endregion

}

