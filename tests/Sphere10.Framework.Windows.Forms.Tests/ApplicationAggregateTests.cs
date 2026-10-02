// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationAggregateTests {
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
	public void NativeStartupRegistrationUsesOneLiveSharedAggregateAndInitializesBlocksOnce() {
		var services = new ServiceCollection();
		services.AddMainForm<ProbeMainForm>();
		using var later = new WinFormsApplicationBlock { Name = "Later", Position = 2 };
		using var first = new WinFormsApplicationBlock { Name = "First", Position = 1 };
		services.AddApplicationBlock(later);
		services.AddApplicationBlock(first);
		using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IWinFormsApplication>();
		var shared = provider.GetRequiredService<IApplication>();
		Assert.That(shared, Is.SameAs(application));
		Assert.That(application.BlockManager, Is.SameAs(provider.GetRequiredService<IBlockManager>()));
		application.Initialize();
		application.Initialize();
		Assert.That(shared.Blocks, Is.EqualTo(new[] { first, later }));
		shared.Blocks[0] = null;
		Assert.That(application.Blocks.First(), Is.SameAs(first));
		var notifications = 0;
		shared.Changed += () => notifications++;
		var screen = application.ScreenHost.ActivateScreen(later, typeof(DirtyScreen));
		Assert.That(shared.ActiveBlock, Is.SameAs(later));
		Assert.That(shared.ActiveScreen, Is.SameAs(screen));
		Assert.That(notifications, Is.GreaterThan(0));
		application.Dispose();
		var countAfterDisposal = notifications;
		application.ScreenHost.ActivateScreen(first, typeof(CleanScreen));
		Assert.That(notifications, Is.EqualTo(countAfterDisposal));
		Assert.That(((ProbeMainForm)provider.GetRequiredService<IMainForm>()).IsDisposed, Is.False);
	}

	[Test]
	public void HiddenRetainedScreensStillContributeUnsavedChanges() {
		using var form = new ProbeMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		using var application = new WinFormsApplication(form, new[] { block });
		application.Initialize();
		application.ScreenHost.ScreenMode = ScreenMode.SingleView;
		var dirty = (DirtyScreen)application.ScreenHost.ActivateScreen(block, typeof(DirtyScreen));
		dirty.HasUnsavedChanges = true;
		application.ScreenHost.ActivateScreen(block, typeof(CleanScreen));
		Assert.That(application.ScreenHost.OpenScreens, Does.Not.Contain(dirty));
		Assert.That(application.ScreenHost.Screens, Does.Contain(dirty));
		Assert.That(application.HasUnsavedChanges, Is.True);
		dirty.HasUnsavedChanges = false;
		Assert.That(application.HasUnsavedChanges, Is.False);
	}

	[Test]
	public void ApplicationCommandsRenderAndExecuteThroughNativeChromeWithoutTakingDefinitionOwnership() {
		using var form = new ProbeMainForm();
		using var block = new WinFormsApplicationBlock { Name = "Workspace" };
		using var application = new WinFormsApplication(form, new[] { block });
		application.Initialize();
		var executions = 0;
		using var menu = new WinFormsApplicationMenu("Application commands");
		var item = new TrackingAction("Run", () => executions++);
		menu.AddItem(item);
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(new[] { item });
		var header = form.MenuEntries.OfType<ToolStripMenuItem>().Single(entry => entry.Text == "Application commands");
		header.DropDownItems[0].PerformClick();
		form.ToolEntries.OfType<ToolStripButton>().Single(entry => entry.ToolTipText == "Run").PerformClick();
		Assert.That(executions, Is.EqualTo(2));
		Assert.That(item.Parent, Is.Null, "Global commands must not reparent caller-owned definitions");
		application.SetMenus(Array.Empty<IApplicationMenu>());
		application.SetToolBarItems(Array.Empty<IApplicationMenuItem>());
		Assert.That(form.MenuEntries.Any(entry => entry.Text == "Application commands"), Is.False);
		Assert.That(form.ToolEntries.Any(entry => entry.ToolTipText == "Run"), Is.False);
		Assert.That(item.DisposeCount, Is.Zero);
		Assert.That(form.RegisteredBlocks, Is.EqualTo(new[] { block }));
	}

	[Test]
	public void InvalidNativeCommandsLeaveCurrentMetadataAndChromeUnchanged() {
		using var form = new ProbeMainForm();
		using var application = new WinFormsApplication(form, Array.Empty<IWinFormsApplicationBlock>());
		using var item = new WinFormsActionMenuItem("Run", () => { });
		application.SetToolBarItems(new[] { item });
		using var unsupported = new WinFormsApplicationMenuItem();
		Assert.That(() => application.SetToolBarItems(new[] { unsupported }), Throws.ArgumentException);
		Assert.That(application.ToolBarItems, Is.EqualTo(new[] { item }));
		Assert.That(form.ToolEntries.Count(entry => entry.ToolTipText == "Run"), Is.EqualTo(1));
	}

	public sealed class ProbeMainForm : BlockMainForm {
		public ToolStripItem[] MenuEntries => MenuStrip.Items.Cast<ToolStripItem>().ToArray();
		public ToolStripItem[] ToolEntries => ToolStrip.Items.Cast<ToolStripItem>().ToArray();
	}

	public sealed class DirtyScreen : WinFormsApplicationScreen, IApplicationScreen {
		public DirtyScreen() => ActivationMode = ScreenActivationMode.SingleInstance;

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool HasUnsavedChanges { get; set; }
	}

	public sealed class CleanScreen : WinFormsApplicationScreen { }

	private sealed class TrackingAction : WinFormsActionMenuItem {
		public TrackingAction(string text, Action action) : base(text, action) { }
		public int DisposeCount { get; private set; }
		public override void Dispose() {
			DisposeCount++;
			base.Dispose();
		}
	}
}
