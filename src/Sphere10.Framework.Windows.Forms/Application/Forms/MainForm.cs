// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

public partial class MainForm : LiteMainForm, IUserInterfaceServices {
	private readonly List<ToolStripItem> _screenToolBarItems = new();
	private readonly List<ToolStripItem> _screenMenuItems = new();
	private ToolStripMenuItem? _screenMenu;
	private bool _ownsScreenMenu;
	private bool _screenToolBarVisible;

	public MainForm() {
		InitializeComponent();
		HelpToolStripMenuItem = new ToolStripMenuItem("Help");
		HelpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] {
			_userguideToolStripMenuItem, _contextHelpToolStripMenuItem, toolStripSeparator1,
			_requestAFeatureToolStripMenuItem, sendACommentToolStripMenuItem, reportABugToolStripMenuItem,
			toolStripSeparator4, _aboutToolStripMenuItem
		});
		MenuStrip.Items.Add(HelpToolStripMenuItem);
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
			foreach (var item in _screenMenuItems)
				_screenMenu.DropDownItems.Remove(item);
			_screenMenuItems.Clear();
			if (_ownsScreenMenu) {
				MenuStrip.Items.Remove(_screenMenu);
				_screenMenu.Dispose();
			}
			_screenMenu = null;
			_ownsScreenMenu = false;
		}
	}

	protected virtual void OnActiveScreenChanged(WinFormsApplicationScreen? Screen) {
		MergeScreenMenu(Screen);
		MergeScreenToolBar();
	}

	protected virtual void MergeScreenMenu(WinFormsApplicationScreen? Screen) {
		if (Screen == null)
			return;
		if (Screen.ShowInApplicationMenuStrip) {
			_screenMenu = FindApplicationMenuHeader(Screen.ApplicationMenuStripText);
			_ownsScreenMenu = _screenMenu == null;
			if (_ownsScreenMenu) {
				_screenMenu = new ToolStripMenuItem(Screen.ApplicationMenuStripText) { Tag = Screen };
				var helpIndex = MenuStrip.Items.IndexOf(HelpToolStripMenuItem);
				MenuStrip.Items.Insert(helpIndex < 0 ? MenuStrip.Items.Count : helpIndex, _screenMenu);
			}
			_screenMenuItems.AddRange(Screen.MenuItems);
			_screenMenu.DropDownItems.AddRange(_screenMenuItems.ToArray());
			KeepHelpMenuLast();
		}
	}

	protected static string GetMenuCaptionKey(string caption) => (caption ?? string.Empty).Replace("&", string.Empty).Trim().ToUpperInvariant();

	protected virtual ToolStripMenuItem FindApplicationMenuHeader(string caption) => MenuStrip.Items.OfType<ToolStripMenuItem>()
		.FirstOrDefault(item => GetMenuCaptionKey(item.Text) == GetMenuCaptionKey(caption));

	protected virtual void KeepHelpMenuLast() {
		if (HelpToolStripMenuItem == null || HelpToolStripMenuItem.IsDisposed || !MenuStrip.Items.Contains(HelpToolStripMenuItem))
			return;
		if (MenuStrip.Items.IndexOf(HelpToolStripMenuItem) == MenuStrip.Items.Count - 1)
			return;
		MenuStrip.Items.Remove(HelpToolStripMenuItem);
		MenuStrip.Items.Add(HelpToolStripMenuItem);
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
		if (!Args.Cancel && !ScreenExitPermissionsChecked)
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

	event EventHandlerEx IUserInterfaceServices.Changed {
		add => ScreenHost.Changed += value;
		remove => ScreenHost.Changed -= value;
	}

	IApplicationScreenHost IUserInterfaceServices.ScreenHost => ScreenHost;

	public virtual async Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		var nativeItem = Guard.ArgumentCast<IWinFormsApplicationMenuItem>(item, nameof(item));
		if (nativeItem is IWinFormsScreenMenuItem screenItem) {
			var block = screenItem.Parent?.Parent;
			Guard.Argument(block != null, nameof(item), "The screen command must belong to a block's menu.");
			if (await ScreenHost.ActivateScreenAsync(block, screenItem, cancellationToken) == null)
				return false;
		}
		if (nativeItem is IWinFormsLinkMenuItem link)
			link.OnSelect(this);
		return true;
	}

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

