// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class FormWindowSettingsTests {
	[Test]
	public void BoundsFollowTheSameMonitorWhenItsDesktopOriginMoves() {
		var Settings = new FormWindowSettings {
			Bounds = new Rectangle(-1700, 120, 900, 600),
			WorkingArea = new Rectangle(-1920, 0, 1920, 1040)
		};
		var Bounds = Tools.WinForms.CalculateWindowBounds(Settings, new Rectangle(1920, 40, 1920, 1040), 96, Size.Empty, Size.Empty);
		Assert.That(Bounds, Is.EqualTo(new Rectangle(2140, 160, 900, 600)));
	}

	[Test]
	public void BoundsAndOffsetsScaleForDpiChanges() {
		var Settings = new FormWindowSettings { Bounds = new Rectangle(80, 60, 800, 500), WorkingArea = new Rectangle(0, 0, 1920, 1080), Dpi = 96 };
		var Bounds = Tools.WinForms.CalculateWindowBounds(Settings, new Rectangle(0, 0, 2560, 1440), 144, Size.Empty, Size.Empty);
		Assert.That(Bounds, Is.EqualTo(new Rectangle(120, 90, 1200, 750)));
	}

	[Test]
	public void BoundsFitSmallerWorkAreaAfterMonitorRemoval() {
		var Settings = new FormWindowSettings { Bounds = new Rectangle(2600, 400, 1800, 1000), WorkingArea = new Rectangle(1920, 0, 2560, 1400) };
		var WorkingArea = new Rectangle(0, 30, 1024, 738);
		var Bounds = Tools.WinForms.CalculateWindowBounds(Settings, WorkingArea, 96, Size.Empty, Size.Empty);
		Assert.That(Bounds, Is.EqualTo(WorkingArea));
	}

	[Test]
	public void BoundsRespectFormMinimumAndMaximumSizes() {
		var Settings = new FormWindowSettings { Bounds = new Rectangle(-10, -30, 20, 900), WorkingArea = new Rectangle(0, 0, 1920, 1040) };
		var Bounds = Tools.WinForms.CalculateWindowBounds(Settings, Settings.WorkingArea, 96, new Size(300, 200), new Size(700, 500));
		Assert.That(Bounds, Is.EqualTo(new Rectangle(0, 0, 300, 500)));
	}

	[Test]
	public void InvalidStoredDpiUsesTheLogicalDefault() {
		var Settings = new FormWindowSettings { Bounds = new Rectangle(20, 40, 500, 400), Dpi = 0 };
		var Bounds = Tools.WinForms.CalculateWindowBounds(Settings, new Rectangle(0, 0, 1600, 1000), 192, Size.Empty, Size.Empty);
		Assert.That(Bounds, Is.EqualTo(new Rectangle(40, 80, 1000, 800)));
	}

	[Test]
	public void ExtremeStoredCoordinatesAndSizesAreClampedWithoutOverflow() {
		var Settings = new FormWindowSettings { Bounds = new Rectangle(int.MaxValue, int.MinValue, int.MaxValue, int.MaxValue), Dpi = 1 };
		var WorkingArea = new Rectangle(-1920, 0, 1920, 1040);
		var Bounds = Tools.WinForms.CalculateWindowBounds(Settings, WorkingArea, 384, Size.Empty, Size.Empty);
		Assert.That(Bounds, Is.EqualTo(WorkingArea));
	}

	[Test]
	public void MissingPlacementPreservesApplicationDefaults() {
		using var Window = NewWindow();
		var Defaults = Window.Bounds;
		Assert.That(Tools.WinForms.RestoreWindowSettings(Window, new FormWindowSettings()), Is.False);
		Assert.That(Window.Bounds, Is.EqualTo(Defaults));
	}

	[TestCase(FormWindowState.Minimized)]
	[TestCase((FormWindowState)999)]
	public void RestoreNeverStartsMinimizedOrWithAnUnknownState(FormWindowState State) {
		using var Window = NewWindow();
		Window.Show();
		var Settings = new FormWindowSettings();
		Tools.WinForms.CaptureWindowSettings(Window, Settings);
		Settings.WindowState = State;
		Assert.That(Tools.WinForms.RestoreWindowSettings(Window, Settings), Is.True);
		Assert.That(Window.WindowState, Is.EqualTo(FormWindowState.Normal));
		Assert.That(Screen.FromControl(Window).WorkingArea.Contains(Window.Bounds), Is.True);
	}

	[Test]
	public void MissingMonitorRestoresIntoAnAvailableWorkArea() {
		using var Window = NewWindow();
		Window.Show();
		var Settings = new FormWindowSettings {
			MonitorDeviceName = "Disconnected monitor",
			Bounds = new Rectangle(50000, 50000, 1800, 1200),
			WorkingArea = new Rectangle(48000, 48000, 3840, 2160)
		};
		Assert.That(Tools.WinForms.RestoreWindowSettings(Window, Settings), Is.True);
		Assert.That(Screen.FromControl(Window).WorkingArea.Contains(Window.Bounds), Is.True);
	}

	[Test]
	public void MaximizedCapturePreservesNormalRestoreBounds() {
		using var Window = NewWindow();
		Window.Show();
		var NormalBounds = Window.Bounds;
		Window.WindowState = FormWindowState.Maximized;
		var Settings = new FormWindowSettings();
		Tools.WinForms.CaptureWindowSettings(Window, Settings);
		Assert.That(Settings.Bounds, Is.EqualTo(NormalBounds));
		Assert.That(Settings.WindowState, Is.EqualTo(FormWindowState.Maximized));
		Assert.That(Settings.MonitorDeviceName, Is.EqualTo(Screen.FromControl(Window).DeviceName));
	}

	[Test]
	public void ClosingPersistsAndAFreshProviderRestoresTheNextWindow() => WithUserSettings(Directory => {
		Rectangle ExpectedBounds;
		using (var Window = NewWindow()) {
			using var SettingsScope = Tools.WinForms.AutoPersistWindowSettings(Window);
			Window.Show();
			Window.Bounds = new Rectangle(Window.Left + 25, Window.Top + 30, 720, 480);
			ExpectedBounds = Window.Bounds;
			Window.Close();
		}
		UserSettings.Provider = new DirectoryFileSettingsProvider(Directory);
		var Saved = UserSettings.Get<FormWindowSettings>("MainForm");
		Assert.That(Saved.Bounds, Is.EqualTo(ExpectedBounds));
		Assert.That(Saved.WorkingArea, Is.EqualTo(Screen.FromRectangle(ExpectedBounds).WorkingArea));
		Assert.That(Saved.MonitorDeviceName, Is.Not.Empty);
		Assert.That(Saved.Dpi, Is.GreaterThan(0));
		using var Restored = NewWindow();
		using var RestoreScope = Tools.WinForms.AutoPersistWindowSettings(Restored);
		Restored.Show();
		Assert.That(Restored.Bounds, Is.EqualTo(ExpectedBounds));
		Restored.Close();
	});

	[Test]
	public void RejectedCloseDoesNotPersistButAcceptedCloseDoes() => WithUserSettings(Directory => {
		using var Window = NewWindow();
		using var SettingsScope = Tools.WinForms.AutoPersistWindowSettings(Window);
		var Veto = true;
		Window.FormClosing += (_, Args) => Args.Cancel = Veto;
		Window.Show();
		Window.Close();
		Assert.That(Window.IsDisposed, Is.False);
		Assert.That(new DirectoryFileSettingsProvider(Directory).ContainsSetting(typeof(FormWindowSettings), "MainForm"), Is.False);
		Veto = false;
		var ExpectedBounds = Window.Bounds;
		Window.Close();
		Assert.That(Window.IsDisposed, Is.True);
		ISettingsProvider FreshProvider = new DirectoryFileSettingsProvider(Directory);
		Assert.That(FreshProvider.Get<FormWindowSettings>("MainForm").Bounds, Is.EqualTo(ExpectedBounds));
	});

	[TestCase(FormWindowState.Normal)]
	[TestCase(FormWindowState.Maximized)]
	public void ClosingWhileMinimizedRetainsThePreviousVisibleState(FormWindowState PreviousState) => WithUserSettings(Directory => {
		using var Window = NewWindow();
		using var SettingsScope = Tools.WinForms.AutoPersistWindowSettings(Window);
		Window.Show();
		var NormalBounds = Window.Bounds;
		Window.WindowState = PreviousState;
		Window.WindowState = FormWindowState.Minimized;
		Window.Close();
		ISettingsProvider FreshProvider = new DirectoryFileSettingsProvider(Directory);
		var Saved = FreshProvider.Get<FormWindowSettings>("MainForm");
		Assert.That(Saved.Bounds, Is.EqualTo(NormalBounds));
		Assert.That(Saved.WindowState, Is.EqualTo(PreviousState));
	});

	[Test]
	public void MinimizedCaptureUsesTheLastVisibleMonitorInsteadOfStaleRestoreBounds() {
		using var Window = NewWindow();
		Window.Show();
		Window.WindowState = FormWindowState.Minimized;
		var LastVisiblePlacement = new FormWindowSettings {
			Bounds = new Rectangle(2040, 90, 975, 600),
			WorkingArea = new Rectangle(1920, 0, 2560, 1400),
			MonitorDeviceName = "Last visible monitor",
			Dpi = 144,
			WindowState = FormWindowState.Maximized
		};
		var Settings = new FormWindowSettings { ID = "MainForm" };
		Tools.WinForms.CaptureWindowSettings(Window, Settings, LastVisiblePlacement);
		Assert.That(Settings.Bounds, Is.EqualTo(LastVisiblePlacement.Bounds));
		Assert.That(Settings.Bounds, Is.Not.EqualTo(Window.RestoreBounds), "The stale restore bounds belong to the other monitor.");
		Assert.That(Settings.MonitorDeviceName, Is.EqualTo(LastVisiblePlacement.MonitorDeviceName));
		Assert.That(Settings.WorkingArea, Is.EqualTo(LastVisiblePlacement.WorkingArea));
		Assert.That(Settings.Dpi, Is.EqualTo(144));
		Assert.That(Settings.WindowState, Is.EqualTo(FormWindowState.Maximized));
		Assert.That(Settings.ID, Is.EqualTo("MainForm"));
	}

	[Test]
	public void DisposingThePersistenceScopeDetachesSaveHandlers() => WithUserSettings(Directory => {
		using var Window = NewWindow();
		var SettingsScope = Tools.WinForms.AutoPersistWindowSettings(Window);
		Window.Show();
		SettingsScope.Dispose();
		Window.Close();
		Assert.That(new DirectoryFileSettingsProvider(Directory).ContainsSetting(typeof(FormWindowSettings), "MainForm"), Is.False);
	});

	[Test]
	public void SidebarWidthRoundTripsThroughTheSettingsProvider() => WithUserSettings(Directory => {
		var Settings = UserSettings.Get<FormWindowSettings>("MainForm");
		Settings.Bounds = new Rectangle(100, 80, 1000, 650);
		Settings.NavigationPaneWidth = 390;
		Settings.Save();
		ISettingsProvider FreshProvider = new DirectoryFileSettingsProvider(Directory);
		Assert.That(FreshProvider.Get<FormWindowSettings>("MainForm").NavigationPaneWidth, Is.EqualTo(390));
	});

	[Test]
	public void ShutdownFinalizerWritesOnceAfterAcceptedCloseAndNeverDuringWindowChanges() => WithUserSettings(Directory => {
		var Provider = new CountingSettingsProvider(Directory);
		UserSettings.Provider = Provider;
		using var Window = NewWindow();
		var Finalizer = new MainFormSettingsFinalizer();
		using var SettingsScope = Finalizer.Attach(Window);
		Window.Show();
		for (var Change = 0; Change < 30; Change++)
			Window.Bounds = new Rectangle(Window.Left + 1, Window.Top + 1, Window.Width + 1, Window.Height + 1);
		Assert.That(Provider.SaveCount, Is.Zero, "Moving and resizing must not serialize user settings.");
		var ExpectedBounds = Window.Bounds;
		Window.Close();
		Assert.That(Provider.SaveCount, Is.Zero, "Accepted close only captures placement for the shutdown task.");
		Finalizer.Finalize();
		Finalizer.Finalize();
		Assert.That(Provider.SaveCount, Is.EqualTo(1), "The shutdown task must save the captured settings exactly once.");
		ISettingsProvider FreshProvider = new DirectoryFileSettingsProvider(Directory);
		Assert.That(FreshProvider.Get<FormWindowSettings>("MainForm").Bounds, Is.EqualTo(ExpectedBounds));
	});

	[Test]
	public void ShutdownFinalizerDoesNotSavePlacementFromAVetoedClose() => WithUserSettings(Directory => {
		var Provider = new CountingSettingsProvider(Directory);
		UserSettings.Provider = Provider;
		using var Window = NewWindow();
		var Finalizer = new MainFormSettingsFinalizer();
		using var SettingsScope = Finalizer.Attach(Window);
		Window.FormClosing += (_, Args) => Args.Cancel = true;
		Window.Show();
		Window.Close();
		Finalizer.Finalize();
		Assert.That(Window.IsDisposed, Is.False);
		Assert.That(Provider.SaveCount, Is.Zero);
	});

	private static Form NewWindow() {
		var WorkingArea = Screen.PrimaryScreen!.WorkingArea;
		return new Form {
			StartPosition = FormStartPosition.Manual,
			Bounds = new Rectangle(WorkingArea.Left + 40, WorkingArea.Top + 40, 650, 400),
			ShowInTaskbar = false,
			Opacity = 0
		};
	}

	private static void WithUserSettings(Action<string> Test) {
		ISettingsProvider? PreviousProvider = null;
		try {
			PreviousProvider = UserSettings.Provider;
		} catch (InvalidOperationException) {
			// Standalone tests can run before the application module initializes the user-settings facade.
		}
		using var RestoreProvider = Tools.Scope.ExecuteOnDispose(() => UserSettings.Provider = PreviousProvider!);
		var Directory = Tools.FileSystem.GetTempEmptyDirectory();
		Guard.Ensure(Path.GetFullPath(Directory).StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase), "Settings test directory must be temporary.");
		using var Cleanup = Tools.Scope.DeleteDirOnDispose(Directory);
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Directory));
		Test(Directory);
	}

	private sealed class CountingSettingsProvider : DirectoryFileSettingsProvider {
		public CountingSettingsProvider(string Directory)
			: base(Directory) {
		}

		public int SaveCount { get; private set; }

		public override void SaveSetting(SettingsObject Settings) {
			SaveCount++;
			base.SaveSetting(Settings);
		}
	}
}
