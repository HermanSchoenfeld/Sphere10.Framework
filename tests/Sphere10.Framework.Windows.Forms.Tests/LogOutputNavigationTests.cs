// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class LogOutputNavigationTests {
	private ISettingsProvider _previousSettings;
	private string _settingsDirectory;

	[SetUp]
	public void UseIsolatedSettings() {
		_previousSettings = null;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => _previousSettings = UserSettings.Provider, out _);
		_settingsDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "LogNavigationSettings-" + Guid.NewGuid().ToString("N"))).FullName;
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_settingsDirectory));
	}

	[TearDown]
	public void RestoreSettings() {
		UserSettings.Provider = _previousSettings;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(_settingsDirectory);
	}
	[TestCase(false)]
	[TestCase(true)]
	public void DockedCaptureRetainsItsOriginatingMenuAfterItsSourceCloses(bool toolbarSelector) => RunWithMessageLoop(async owner => {
		using var form = CreateForm(owner);
		using var first = CreateBlock("Diagnostics");
		using var second = CreateBlock("Other Tools");
		form.RegisterBlock(first);
		form.RegisterBlock(second);
		var source = new WinFormsApplicationScreen { Block = first, Title = "Capture Source" };
		var selector = new LogOutputSelectorControl { Name = "captureOutput" };
		if (toolbarSelector) {
			var strip = new ToolStrip { Name = "captureCommands" };
			strip.Items.Add(new ToolStripControlHost(selector));
			source.Controls.Add(strip);
			source.ToolBar = strip;
		} else
			source.Controls.Add(selector);
		Assert.That(form.ShowScreen(source), Is.True);
		var other = form.ScreenHost.ActivateScreen(second, typeof(InventoryScreen));
		Assert.That(form.ShowScreen(source), Is.True);
		selector.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Screen, TimestampEnabled = false });
		var log = form.ScreenHost.OpenScreens.OfType<LogScreen>().Single();
		Assert.That(log.Block, Is.SameAs(first), "The log borrows its originating block, independently of the source screen's lifetime.");
		Assert.That(selector.EnqueueInfo("Retained Capture"), Is.True);
		await selector.FlushLoggingAsync();
		await WaitForAsync(() => Output(log).Text.Contains("Retained Capture"));
		form.ActiveBlock = second;
		AssertNavigation(form, second);
		Assert.That(form.ScreenHost.DockScreen(log), Is.True);
		AssertNavigation(form, first);
		Assert.That(form.ScreenHost.UndockScreen(log), Is.True);
		Assert.That(form.ShowScreen(other), Is.True);
		AssertNavigation(form, second);
		Assert.That(form.ShowScreen(log), Is.True);
		AssertNavigation(form, second);
		Assert.That(form.ScreenHost.DockScreen(log), Is.True);
		AssertNavigation(form, first);
		Assert.That(form.ShowScreen(source), Is.True);
		Assert.That(form.ShowScreen(log), Is.True);
		AssertNavigation(form, first);
		Assert.That(form.ScreenHost.CloseScreen(source), Is.True);
		Assert.That(source.IsDisposed, Is.True);
		Assert.That(log.IsDisposed, Is.False);
		Assert.That(Output(log).Text, Does.Contain("Retained Capture"));
		Assert.That(form.ShowScreen(other), Is.True);
		AssertNavigation(form, second);
		Assert.That(form.ShowScreen(log), Is.True);
		AssertNavigation(form, first);
		SaveScreenshot(form, toolbarSelector ? "log-output-docked-toolbar-navigation.png" : "log-output-docked-navigation.png");
	});

	[Test]
	public void ABlocklessAuxiliaryScreenKeepsTheSelectedNavigationPane() => RunWithMessageLoop(owner => {
		using var form = CreateForm(owner);
		using var block = CreateBlock("Available Tools");
		form.RegisterBlock(block);
		form.ScreenHost.ActivateScreen(block, typeof(InventoryScreen));
		using var log = new LogScreen();
		Assert.That(form.ScreenHost.ShowScreenUndocked(log), Is.True);
		Assert.That(log.FindForm().Text, Is.EqualTo("Log Output - Capture"));
		Assert.That(form.ScreenHost.DockScreen(log), Is.True);
		Assert.That(((WinFormsApplicationScreenHost)form.ScreenHost).TabControl.TabPages.Cast<TabPage>().Single(page => ReferenceEquals(page.Tag, log)).Text,
			Is.EqualTo("Log Output - Capture"));
		Assert.That(log.Block, Is.Null);
		AssertNavigation(form, block);
		return Task.CompletedTask;
	});

	[TestCase(false)]
	[TestCase(true)]
	public void ClosingALogReleasesItsBoundedTextAndAllOwnerReferences(bool disposeDirectly) => RunWithMessageLoop(async owner => {
		using var host = new WinFormsApplicationScreenHost { Dock = DockStyle.Fill, ScreenMode = ScreenMode.MultiView };
		owner.Controls.Add(host);
		using var selector = new LogOutputSelectorControl { ScreenHost = host };
		selector.StartLogging(new LogOutputConfiguration { Destination = LogOutputDestination.Screen, TimestampEnabled = false });
		var log = host.OpenScreens.OfType<LogScreen>().Single();
		var text = Output(log);
		var writer = typeof(LogScreen).GetField("_writer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(log);
		var pendingCharacters = writer.GetType().GetField("_pendingCharacters", BindingFlags.Instance | BindingFlags.NonPublic);
		var payload = new string('x', 120000);
		for (var batch = 0; batch < 10; batch++) {
			var marker = "Batch " + batch;
			var write = Task.Run(() => { log.Logger.Info(payload); log.Logger.Info(payload + marker); });
			Assert.That(write.Wait(TimeSpan.FromSeconds(2)), Is.True, "Worker writes must finish without waiting for the UI dispatcher.");
			Assert.That((int)pendingCharacters.GetValue(writer), Is.LessThanOrEqualTo(256 * 1024));
			await WaitForAsync(() => text.Text.Contains(marker));
			Assert.That(text.TextLength, Is.LessThanOrEqualTo(1024 * 1024));
		}
		Assert.That(text.TextLength, Is.EqualTo(1024 * 1024), "The displayed history is bounded after multiple completed batches.");
		if (disposeDirectly)
			log.Dispose();
		else
			Assert.That(host.CloseScreen(log), Is.True);
		Assert.Multiple(() => {
			Assert.That(log.IsDisposed, Is.True);
			Assert.That(text.IsHandleCreated, Is.False);
			Assert.That(text.Text, Is.Empty, "A retained disposed control must not retain its former display history.");
			Assert.That((int)pendingCharacters.GetValue(writer), Is.Zero);
			Assert.That(host.OpenScreens, Does.Not.Contain(log));
			Assert.That(host.Screens, Does.Not.Contain(log));
			Assert.That((ICollection)typeof(LogOutputSelectorControl).GetField("_logScreens", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(selector), Is.Empty);
		});
	});

	private static WinFormsApplicationBlock CreateBlock(string name) => new WinFormsApplicationBlockBuilder()
		.WithName(name)
		.ConfigureMenu(menu => menu
			.WithText(name)
			.AddScreenItem<InventoryScreen>("Inventory")
		)
		.Build();

	private static BlockMainForm CreateForm(Form owner) {
		var form = new BlockMainForm {
			ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700)
		};
		form.Show(owner);
		return form;
	}

	private static void AssertNavigation(BlockMainForm form, IWinFormsApplicationBlock block) {
		var split = form.Controls.OfType<SplitContainer>().Single();
		var bar = split.Panel1.Controls.OfType<ApplicationBar>().Single();
		Assert.Multiple(() => {
			Assert.That(form.ActiveBlock, Is.SameAs(block));
			Assert.That(bar.SelectedItem?.MenuControl, Is.SameAs(form.BlockBindings[block]));
			Assert.That(bar.ApplicationBarControl, Is.SameAs(form.BlockBindings[block]));
			Assert.That(bar.ApplicationBarControl.Visible, Is.True);
			Assert.That(form.BlockBindings[block].Controls.Count, Is.GreaterThan(0));
			Assert.That(split.Panel1Collapsed, Is.False);
		});
	}

	private static TextBox Output(LogScreen screen) => screen.Controls.Find("_outputTextBox", true).OfType<TextBox>().Single();

	private static async Task WaitForAsync(Func<bool> condition) {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		while (!condition())
			await Task.Delay(10, timeout.Token);
	}

	private static void SaveScreenshot(Form form, string filename) {
		var directory = Environment.GetEnvironmentVariable("SPHERE10_LOG_OUTPUT_SCREENSHOTS");
		if (string.IsNullOrWhiteSpace(directory))
			return;
		Directory.CreateDirectory(directory);
		using var bitmap = new Bitmap(form.Width, form.Height);
		form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
		bitmap.Save(Path.Combine(directory, filename), ImageFormat.Png);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 25000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Log navigation test timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
			completed = true;
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	public sealed class InventoryScreen : WinFormsApplicationScreen { }
}
