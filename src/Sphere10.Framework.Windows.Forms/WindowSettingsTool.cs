// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

// ReSharper disable CheckNamespace
namespace Tools;

public static partial class WinForms {
	/// <summary>Restores on load and saves only when the window actually closes. Dispose the scope to detach the handlers.</summary>
	public static IDisposable AutoPersistWindowSettings(Form Window, string SettingsID = "MainForm")
		=> TrackWindowSettings(Window, SettingsID, Settings => Settings.Save());

	internal static IDisposable TrackWindowSettings(Form Window, string SettingsID, Action<FormWindowSettings> OnAcceptedClose) {
		Guard.ArgumentNotNull(Window, nameof(Window));
		Guard.ArgumentNotNullOrEmpty(SettingsID, nameof(SettingsID));
		Guard.ArgumentNotNull(OnAcceptedClose, nameof(OnAcceptedClose));
		var LastVisibleBounds = Rectangle.Empty;
		var LastRestoreBounds = Rectangle.Empty;
		var LastVisibleDpi = 96;
		var LastVisibleState = FormWindowState.Normal;
		FormWindowSettings? Settings = null;
		var Loaded = false;

		void Load(object? Sender, EventArgs Args) {
			if (Loaded || Tools.Runtime.IsDesignMode)
				return;
			Loaded = true;
			try {
				Settings = UserSettings.Get<FormWindowSettings>(SettingsID);
				RestoreWindowSettings(Window, Settings);
			} catch (Exception Error) {
				SystemLog.Exception(Error);
			}
			RememberPlacement(Sender, Args);
		}

		void RememberPlacement(object? Sender, EventArgs Args) {
			if (!Loaded || Window.IsDisposed || Window.WindowState == FormWindowState.Minimized)
				return;
			// Keep only in-memory coordinates and state; monitor lookup and settings serialization happen at the selected save boundary.
			LastVisibleState = Window.WindowState;
			LastVisibleBounds = Window.Bounds;
			LastRestoreBounds = LastVisibleState == FormWindowState.Normal ? LastVisibleBounds : Window.RestoreBounds;
			LastVisibleDpi = Window.DeviceDpi;
		}

		void CaptureOnClose(object? Sender, FormClosedEventArgs Args) {
			if (!Loaded || Tools.Runtime.IsDesignMode)
				return;
			try {
				// Replace unreadable preferences after a successful close, so the next launch can recover.
				Settings ??= (FormWindowSettings)UserSettings.Provider.NewSetting(typeof(FormWindowSettings), SettingsID);
				FormWindowSettings? LastVisiblePlacement = null;
				if (Window.WindowState == FormWindowState.Minimized && LastVisibleBounds.Width > 0 && LastVisibleBounds.Height > 0) {
					var LastVisibleMonitor = Screen.FromRectangle(LastVisibleBounds);
					var BoundsMonitor = Screen.FromRectangle(LastRestoreBounds);
					if (LastVisibleMonitor.DeviceName != BoundsMonitor.DeviceName)
						LastRestoreBounds.Offset(LastVisibleMonitor.WorkingArea.X - BoundsMonitor.WorkingArea.X, LastVisibleMonitor.WorkingArea.Y - BoundsMonitor.WorkingArea.Y);
					LastVisiblePlacement = new FormWindowSettings {
						Bounds = LastRestoreBounds,
						WorkingArea = LastVisibleMonitor.WorkingArea,
						MonitorDeviceName = LastVisibleMonitor.DeviceName,
						Dpi = LastVisibleDpi,
						WindowState = LastVisibleState
					};
				}
				CaptureWindowSettings(Window, Settings, LastVisiblePlacement);
				OnAcceptedClose(Settings);
			} catch (Exception Error) {
				SystemLog.Exception(Error);
			}
		}

		Window.Load += Load;
		Window.Resize += RememberPlacement;
		Window.Move += RememberPlacement;
		Window.FormClosed += CaptureOnClose;
		if (Window.Visible)
			Load(Window, EventArgs.Empty);
		return Tools.Scope.ExecuteOnDispose(() => {
			Window.Load -= Load;
			Window.Resize -= RememberPlacement;
			Window.Move -= RememberPlacement;
			Window.FormClosed -= CaptureOnClose;
		});
	}

	/// <summary>Captures normal restore bounds; a prior visible capture preserves the monitor and state when minimized.</summary>
	public static void CaptureWindowSettings(Form Window, FormWindowSettings Settings, FormWindowSettings? LastVisiblePlacement = null) {
		Guard.ArgumentNotNull(Window, nameof(Window));
		Guard.ArgumentNotNull(Settings, nameof(Settings));
		if (Window.WindowState == FormWindowState.Minimized && LastVisiblePlacement is { Bounds.Width: > 0, Bounds.Height: > 0 }) {
			// RestoreBounds can still name the previous monitor after moving a maximized window, so retain the last visible placement.
			Settings.Bounds = LastVisiblePlacement.Bounds;
			Settings.WorkingArea = LastVisiblePlacement.WorkingArea;
			Settings.MonitorDeviceName = LastVisiblePlacement.MonitorDeviceName;
			Settings.Dpi = LastVisiblePlacement.Dpi;
			Settings.NavigationPaneWidth = (Window as BlockMainForm)?.NavigationPaneWidth;
			Settings.WindowState = LastVisiblePlacement.WindowState == FormWindowState.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
			return;
		}
		var Bounds = Window.WindowState == FormWindowState.Normal ? Window.Bounds : Window.RestoreBounds;
		if (Bounds.Width <= 0 || Bounds.Height <= 0)
			return;
		var BoundsMonitor = Screen.FromRectangle(Bounds);
		var Monitor = Window.WindowState == FormWindowState.Minimized ? BoundsMonitor : Screen.FromControl(Window);
		if (Monitor.DeviceName != BoundsMonitor.DeviceName)
			Bounds.Offset(Monitor.WorkingArea.X - BoundsMonitor.WorkingArea.X, Monitor.WorkingArea.Y - BoundsMonitor.WorkingArea.Y);
		Settings.Bounds = Bounds;
		Settings.WorkingArea = Monitor.WorkingArea;
		Settings.MonitorDeviceName = Monitor.DeviceName;
		Settings.Dpi = Window.DeviceDpi;
		Settings.NavigationPaneWidth = (Window as BlockMainForm)?.NavigationPaneWidth;
		Settings.WindowState = Window.WindowState == FormWindowState.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
	}

	/// <summary>Restores onto the saved monitor or its nearest replacement, keeping the window inside the current work area.</summary>
	public static bool RestoreWindowSettings(Form Window, FormWindowSettings Settings) {
		Guard.ArgumentNotNull(Window, nameof(Window));
		Guard.ArgumentNotNull(Settings, nameof(Settings));
		if (Settings.Bounds.Width <= 0 || Settings.Bounds.Height <= 0)
			return false;
		var Monitor = Screen.AllScreens.FirstOrDefault(Item => string.Equals(Item.DeviceName, Settings.MonitorDeviceName, StringComparison.OrdinalIgnoreCase))
			?? Screen.FromRectangle(Settings.Bounds);
		Window.StartPosition = FormStartPosition.Manual;
		Window.WindowState = FormWindowState.Normal;

		// Move onto the target monitor first so WinForms updates DeviceDpi before we scale the saved bounds.
		Window.Bounds = new Rectangle(Monitor.WorkingArea.Location,
			new Size(Math.Min(Window.Width, Monitor.WorkingArea.Width), Math.Min(Window.Height, Monitor.WorkingArea.Height)));
		Window.Bounds = CalculateWindowBounds(Settings, Monitor.WorkingArea, Window.DeviceDpi, Window.MinimumSize, Window.MaximumSize);
		Window.WindowState = Settings.WindowState == FormWindowState.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
		if (Window is BlockMainForm BlockForm && Settings.NavigationPaneWidth is > 0) {
			var Scale = Window.DeviceDpi / (double)(Settings.Dpi > 0 ? Settings.Dpi : 96);
			BlockForm.NavigationPaneWidth = (int)Tools.Values.ClipValue(Math.Round(Settings.NavigationPaneWidth.Value * Scale), 1, int.MaxValue);
		}
		return true;
	}

	/// <summary>Adapts saved bounds to a monitor's current DPI, work area and the form's size limits.</summary>
	public static Rectangle CalculateWindowBounds(FormWindowSettings Settings, Rectangle WorkingArea, int Dpi, Size MinimumSize, Size MaximumSize) {
		Guard.ArgumentNotNull(Settings, nameof(Settings));
		Guard.ArgumentGT(WorkingArea.Width, 0, nameof(WorkingArea));
		Guard.ArgumentGT(WorkingArea.Height, 0, nameof(WorkingArea));
		Guard.ArgumentGT(Dpi, 0, nameof(Dpi));
		var Scale = Dpi / (double)(Settings.Dpi > 0 ? Settings.Dpi : 96);
		var MaximumWidth = MaximumSize.Width > 0 ? Math.Min(MaximumSize.Width, WorkingArea.Width) : WorkingArea.Width;
		var MaximumHeight = MaximumSize.Height > 0 ? Math.Min(MaximumSize.Height, WorkingArea.Height) : WorkingArea.Height;
		var MinimumWidth = Tools.Values.ClipValue(MinimumSize.Width, 1, MaximumWidth);
		var MinimumHeight = Tools.Values.ClipValue(MinimumSize.Height, 1, MaximumHeight);
		var Width = (int)Tools.Values.ClipValue(Math.Round(Settings.Bounds.Width * Scale), MinimumWidth, MaximumWidth);
		var Height = (int)Tools.Values.ClipValue(Math.Round(Settings.Bounds.Height * Scale), MinimumHeight, MaximumHeight);
		var SavedWorkingArea = Settings.WorkingArea;
		var HasWorkingArea = SavedWorkingArea.Width > 0 && SavedWorkingArea.Height > 0;
		var OffsetX = HasWorkingArea ? (double)Settings.Bounds.X - SavedWorkingArea.X : (double)Settings.Bounds.X - WorkingArea.X;
		var OffsetY = HasWorkingArea ? (double)Settings.Bounds.Y - SavedWorkingArea.Y : (double)Settings.Bounds.Y - WorkingArea.Y;
		var X = (int)Tools.Values.ClipValue(WorkingArea.X + Math.Round(OffsetX * Scale), WorkingArea.Left, (double)WorkingArea.Right - Width);
		var Y = (int)Tools.Values.ClipValue(WorkingArea.Y + Math.Round(OffsetY * Scale), WorkingArea.Top, (double)WorkingArea.Bottom - Height);
		return new Rectangle(X, Y, Width, Height);
	}
}