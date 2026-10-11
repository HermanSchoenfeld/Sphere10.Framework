// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationHostRenderingTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void ScreenTransitionPublishesOneFinalSelectionAndRebuildsChromeOnce() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		form.RegisterBlock(block);
		form.Show();
		form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		System.Windows.Forms.Application.DoEvents();
		form.ResetCounts();
		var selections = 0;
		var changes = 0;
		form.ScreenHost.ScreenSelected += _ => selections++;
		form.ScreenHost.Changed += () => changes++;
		var created = form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		Assert.That(form.ActiveScreen, Is.SameAs(created));
		Assert.That(form.ActiveChangingCount, Is.EqualTo(1));
		Assert.That(form.ActiveChangedCount, Is.EqualTo(1));
		Assert.That(form.CommandUpdateCount, Is.EqualTo(1));
		Assert.That(form.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(form.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(changes, Is.EqualTo(1));
		Assert.That(selections, Is.EqualTo(1));
	}

	[Test]
	public void FailedRenderingEntryLeavesTheNativeTransitionReadyForRetry() {
		using var form = new CountingMainForm { ScreenMode = ScreenMode.MultiView };
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		using var failing = new FailingLayoutPanel();
		form.Controls.Add(failing);
		form.RegisterBlock(block);
		form.Show();
		var previous = form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		System.Windows.Forms.Application.DoEvents();
		form.ResetCounts();
		var changes = 0;
		var selections = 0;
		form.ScreenHost.Changed += () => changes++;
		form.ScreenHost.ScreenSelected += _ => selections++;
		failing.FailOnNextEnumeration();
		var failure = Assert.Throws<InvalidOperationException>(() => form.ScreenHost.ActivateScreen(block, typeof(CountingScreen)));
		Assert.That(failure, Is.SameAs(failing.Failure));
		Assert.That(form.ActiveScreen, Is.SameAs(previous));
		Assert.That(form.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(form.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(changes, Is.Zero);
		Assert.That(selections, Is.Zero);
		form.ResetCounts();
		var created = form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		Assert.That(form.ActiveScreen, Is.SameAs(created));
		Assert.That(form.ActiveChangedCount, Is.EqualTo(1));
		Assert.That(form.CommandUpdateCount, Is.EqualTo(1));
		Assert.That(form.DisableRedrawCount, Is.EqualTo(1));
		Assert.That(form.EnableRedrawCount, Is.EqualTo(1));
		Assert.That(changes, Is.EqualTo(1));
		Assert.That(selections, Is.EqualTo(1));
	}

	[Test]
	public async Task ApplicationCommandsSurviveBlockAndScreenCaptionCollisions() {
		using var form = new CountingMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Workspace", ShowInMenuStrip = true };
		await using var application = new WinFormsApplication(form, new[] { block });
		using var globalMenu = new WinFormsApplicationMenu("&File");
		using var globalHelpMenu = new WinFormsApplicationMenu("&Help");
		var globalRuns = 0;
		var blockRuns = 0;
		var globalHelpRuns = 0;
		var blockHelpRuns = 0;
		var global = new WinFormsActionMenuItem("Save", () => globalRuns++);
		var contextual = new WinFormsActionMenuItem("Save", () => blockRuns++);
		var globalHelp = new WinFormsActionMenuItem("Inspect", () => globalHelpRuns++);
		var contextualHelp = new WinFormsActionMenuItem("Inspect", () => blockHelpRuns++);
		globalMenu.AddItem(global);
		globalHelpMenu.AddItem(globalHelp);
		var blockMenu = new WinFormsApplicationMenu("File") { ShowInMenuStrip = true };
		blockMenu.AddItem(contextual);
		block.AddMenu(blockMenu);
		var blockHelpMenu = new WinFormsApplicationMenu("Help") { ShowInMenuStrip = true };
		blockHelpMenu.AddItem(contextualHelp);
		block.AddMenu(blockHelpMenu);
		application.SetMenus(new[] { globalMenu, globalHelpMenu });
		application.SetToolBarItems(new[] { global });
		await application.InitializeAsync();
		var legacyFile = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		var legacyExit = legacyFile.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "E&xit");
		var legacyHelp = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "Help");
		var legacyContextHelp = legacyHelp.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "Context Help");
		var screen = (CommandScreen)form.ScreenHost.ActivateScreen(block, typeof(CommandScreen));
		var file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		var help = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "Help");
		Assert.That(file, Is.SameAs(legacyFile));
		Assert.That(help, Is.SameAs(legacyHelp));
		Assert.That(form.MenuEntries.Last(), Is.SameAs(legacyHelp));
		Assert.That(file.DropDownItems.OfType<ToolStripMenuItem>().Select(item => item.Text), Is.EqualTo(new[] { "Save", "Save", "E&xit" }));
		file.DropDownItems[0].PerformClick();
		file.DropDownItems[1].PerformClick();
		help.DropDownItems[0].PerformClick();
		help.DropDownItems[1].PerformClick();
		legacyExit.PerformClick();
		legacyContextHelp.PerformClick();
		Assert.That(globalRuns, Is.EqualTo(1));
		Assert.That(blockRuns, Is.Zero, "The screen replaces the block's contextual Save command.");
		Assert.That(screen.ExecuteCount, Is.EqualTo(1));
		Assert.That(globalHelpRuns, Is.EqualTo(1));
		Assert.That(blockHelpRuns, Is.Zero);
		Assert.That(screen.HelpExecuteCount, Is.EqualTo(1));
		Assert.That(form.ExitCount, Is.EqualTo(1));
		Assert.That(form.HelpCount, Is.EqualTo(1));
		Assert.That(form.ToolEntries.Count(item => item.ToolTipText == "Save"), Is.EqualTo(2));
		Assert.That(global.Parent, Is.SameAs(globalMenu));
		Assert.That(screen.Command.Parent, Is.SameAs(screen.Menus[0]), "Menu composition preserves the command's declaring menu.");
		var previousGenerated = file.DropDownItems[1];
		form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		help = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "Help");
		file.DropDownItems[0].PerformClick();
		file.DropDownItems[1].PerformClick();
		help.DropDownItems[0].PerformClick();
		help.DropDownItems[1].PerformClick();
		legacyExit.PerformClick();
		legacyContextHelp.PerformClick();
		Assert.That(globalRuns, Is.EqualTo(2));
		Assert.That(blockRuns, Is.EqualTo(1));
		Assert.That(globalHelpRuns, Is.EqualTo(2));
		Assert.That(blockHelpRuns, Is.EqualTo(1));
		Assert.That(form.ExitCount, Is.EqualTo(2));
		Assert.That(form.HelpCount, Is.EqualTo(2));
		Assert.That(legacyExit.IsDisposed, Is.False);
		Assert.That(legacyContextHelp.IsDisposed, Is.False);
		Assert.That(previousGenerated.IsDisposed, Is.True);
		Assert.That(form.MenuEntries.Last(), Is.SameAs(legacyHelp));
	}

	[TestCase("&File")]
	[TestCase("&Actions")]
	public async Task NativeScreenMenusReuseExistingHeadersWithoutTakingCommandOwnership(string caption) {
		using var form = new CountingMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		await using var application = new WinFormsApplication(form, new[] { block });
		using var globalMenu = new WinFormsApplicationMenu(caption);
		globalMenu.AddItem(new WinFormsActionMenuItem("Keep", () => { }));
		application.SetMenus(new[] { globalMenu });
		await application.InitializeAsync();
		var file = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text == "File");
		var legacyExit = file.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "E&xit");
		var screen = new NativeMenuScreen { ApplicationBlock = block, ApplicationMenuStripText = caption };
		form.ScreenHost.ShowScreen(screen);
		var header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text.Replace("&", string.Empty) == caption.Replace("&", string.Empty));
		Assert.That(header.DropDownItems.Contains(screen.Command), Is.True);
		Assert.That(screen.Command.IsDisposed, Is.False);
		screen.Command.PerformClick();
		Assert.That(screen.ExecuteCount, Is.EqualTo(1));
		form.ScreenHost.ActivateScreen(block, typeof(CountingScreen));
		header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text.Replace("&", string.Empty) == caption.Replace("&", string.Empty));
		Assert.That(header.DropDownItems.Contains(screen.Command), Is.False);
		Assert.That(screen.Command.IsDisposed, Is.False);
		Assert.That(legacyExit.IsDisposed, Is.False);
		Assert.That(form.ScreenHost.ShowScreen(screen), Is.True);
		header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(item => item.Text.Replace("&", string.Empty) == caption.Replace("&", string.Empty));
		Assert.That(header.DropDownItems.Contains(screen.Command), Is.True);
		screen.Command.PerformClick();
		Assert.That(screen.ExecuteCount, Is.EqualTo(2));
	}

	[Test]
	public void SharedModeSetterAndSpecializedContractsUseTheNativeGuardedTransition() {
		using var host = new WinFormsApplicationScreenHost { ScreenMode = ScreenMode.MultiView };
		var first = new GuardedScreen();
		var active = new CountingScreen();
		host.ShowScreen(first);
		host.ShowScreen(active);
		first.CancelHide = true;
		IApplicationScreenHost common = host;
		Assert.That(() => common.ScreenMode = ScreenMode.SingleView, Throws.InvalidOperationException);
		Assert.That(common.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { first, active }));
		first.CancelHide = false;
		common.ScreenMode = ScreenMode.SingleView;
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { active }));
		Assert.That(first.IsDisposed, Is.True);
		ISingleApplicationScreenHost single = host;
		Assert.That(single.CanCloseActiveScreenAsync().Result, Is.True);
		Assert.That(single.CloseActiveScreenAsync().Result, Is.True);
		Assert.That(common.ActiveScreen, Is.Null);
		Assert.That(active.IsDisposed, Is.True);
		Assert.That(host.Screens, Is.Empty, "Closing the active native screen removes its control and hosting metadata together.");
		common.ScreenMode = ScreenMode.MultiView;
		IMultiApplicationScreenHost multi = host;
		Assert.That(multi.OpenScreens, Is.Empty);
	}

	[Test]
	public void SingleScreenClosePreservesTheNativeControlWhenItsHideGuardCancels() {
		using var host = new WinFormsApplicationScreenHost();
		var screen = new GuardedScreen { CancelHide = true };
		host.ShowScreen(screen);
		ISingleApplicationScreenHost single = host;
		Assert.That(single.CanCloseActiveScreenAsync().Result, Is.False);
		Assert.That(single.CloseActiveScreenAsync().Result, Is.False);
		Assert.That(single.ActiveScreen, Is.SameAs(screen));
		Assert.That(screen.IsDisposed, Is.False);
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { screen }));
		screen.CancelHide = false;
		Assert.That(single.CloseActiveScreenAsync().Result, Is.True);
		Assert.That(screen.IsDisposed, Is.True);
	}

	private sealed class CountingMainForm : BlockMainForm {
		public int CommandUpdateCount { get; private set; }
		public int ActiveChangingCount { get; private set; }
		public int ActiveChangedCount { get; private set; }
		public int DisableRedrawCount { get; private set; }
		public int EnableRedrawCount { get; private set; }
		public int ExitCount { get; private set; }
		public int HelpCount { get; private set; }
		public ToolStripItem[] MenuEntries => MenuStrip.Items.Cast<ToolStripItem>().ToArray();
		public ToolStripItem[] ToolEntries => ToolStrip.Items.Cast<ToolStripItem>().ToArray();

		public void ResetCounts() => CommandUpdateCount = ActiveChangingCount = ActiveChangedCount = DisableRedrawCount = EnableRedrawCount = 0;

		protected override void OnLoad(EventArgs args) { }

		protected override void OnFirstActivated() { }

		protected override void Exit_Click(object sender, EventArgs args) => ExitCount++;

		protected override void ContextHelp_Click(object sender, EventArgs args) => HelpCount++;

		protected override void UpdateApplicationCommands() {
			CommandUpdateCount++;
			base.UpdateApplicationCommands();
		}

		protected override void OnActiveScreenChanging(WinFormsApplicationScreen screen) {
			ActiveChangingCount++;
			base.OnActiveScreenChanging(screen);
		}

		protected override void OnActiveScreenChanged(WinFormsApplicationScreen screen) {
			ActiveChangedCount++;
			base.OnActiveScreenChanged(screen);
		}

		protected override void WndProc(ref Message message) {
			if (message.Msg == 0x000B) {
				if (message.WParam == IntPtr.Zero)
					DisableRedrawCount++;
				else
					EnableRedrawCount++;
			}
			base.WndProc(ref message);
		}
	}

	public class CountingScreen : WinFormsApplicationScreen { }

	private sealed class FailingLayoutPanel : Panel {
		private bool _failEnumeration;

		public InvalidOperationException Failure { get; } = new("Controlled control failure.");

		public void FailOnNextEnumeration() => _failEnumeration = true;

		protected override ControlCollection CreateControlsInstance() => new FailingControlCollection(this);

		private sealed class FailingControlCollection : ControlCollection {
			private readonly FailingLayoutPanel _owner;

			public FailingControlCollection(FailingLayoutPanel owner)
				: base(owner) => _owner = owner;

			public override IEnumerator GetEnumerator() {
				if (_owner._failEnumeration) {
					_owner._failEnumeration = false;
					throw _owner.Failure;
				}
				return base.GetEnumerator();
			}
		}
	}

	public sealed class NativeMenuScreen : CountingScreen {
		public NativeMenuScreen() {
			ActivationMode = ScreenActivationMode.SingleInstance;
			ApplicationMenuStripText = "&File";
			ShowInApplicationMenuStrip = true;
			Command = new ToolStripMenuItem("Inspect route", null, (_, _) => ExecuteCount++);
			RegisterMenuItem(Command);
		}

		public ToolStripMenuItem Command { get; }
		public int ExecuteCount { get; private set; }
	}

	public sealed class GuardedScreen : CountingScreen {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool CancelHide { get; set; }

		protected override void OnHide(ref bool cancelHide) => cancelHide = CancelHide;
	}

	public sealed class CommandScreen : WinFormsApplicationScreen {
		private readonly WinFormsApplicationMenu _menu;
		private readonly WinFormsApplicationMenu _helpMenu;

		public CommandScreen() {
			Command = new WinFormsActionMenuItem("Save", () => ExecuteCount++);
			_menu = new WinFormsApplicationMenu("File");
			_menu.AddItem(Command);
			_helpMenu = new WinFormsApplicationMenu("H&elp");
			_helpMenu.AddItem(new WinFormsActionMenuItem("Inspect", () => HelpExecuteCount++));
		}

		public int ExecuteCount { get; private set; }
		public int HelpExecuteCount { get; private set; }
		public WinFormsActionMenuItem Command { get; }
		public override IWinFormsApplicationMenu[] Menus => new[] { _menu, _helpMenu };
		public override IWinFormsApplicationMenuItem[] ToolBarItems => new[] { Command };

		protected override void Dispose(bool disposing) {
			if (disposing) {
				_menu.Dispose();
				_helpMenu.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
