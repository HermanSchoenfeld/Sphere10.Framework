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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ScreenWindowMenuTests {
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
	public async Task AsyncShowAcceptsNewConfiguredInstancesAndRetainsOwnershipValidation() {
		using var form = new ProbeForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		using var otherForm = new ProbeForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		var supplied = new ProbeScreen { Title = "Configured Inspector" };
		Assert.That(await form.ScreenHost.ShowScreenAsync(supplied), Is.True);
		Assert.That(supplied.Host, Is.SameAs(form.ScreenHost));
		Assert.That(form.ScreenHost.OpenScreens, Is.EqualTo(new[] { supplied }));
		Assert.That(form.ScreenHost.UndockScreen(supplied), Is.True);
		Assert.That(await form.ScreenHost.ShowScreenAsync(supplied), Is.True);
		Assert.That(form.ScreenHost.IsScreenUndocked(supplied), Is.True, "Async navigation must retain the exact detached instance.");
		Assert.That(() => otherForm.ScreenHost.ShowScreenAsync(supplied), Throws.ArgumentException, "A supplied instance must not cross host ownership.");
		using var disposed = new ProbeScreen();
		disposed.Dispose();
		Assert.That(() => form.ScreenHost.ShowScreenAsync(disposed), Throws.ArgumentException);
		Assert.That(await form.ScreenHost.CloseAllScreensAsync(), Is.True);
		Assert.That(supplied.IsDisposed, Is.True);
	}

	[Test]
	public async Task DirectDetachedLaunchKeepsTheCurrentMainTabAndNeverAsksItToHide() {
		using var form = new ProbeForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		var permanent = new PermanentScreen { Title = "Overview" };
		var current = new ProbeScreen { Title = "Processes" };
		form.ShowScreen(permanent);
		form.ShowScreen(current);
		current.CancelHide = true;
		var selectedTab = form.ScreenHost.TabControl.SelectedTab;
		var activeBlock = form.ScreenHost.ActiveBlock;
		var tabs = form.ScreenHost.TabControl.TabPages.Cast<TabPage>().ToArray();
		var selections = 0;
		form.ScreenHost.TabControl.SelectedIndexChanged += (_, _) => selections++;
		var hides = current.HideRequests;
		var inspector = new ProbeScreen { Title = "Inspect Current Process" };
		var displayed = 0;
		inspector.ScreenDisplayed += (_, _) => displayed++;
		IApplicationScreenHost host = form.ScreenHost;
		Assert.That(await host.ShowScreenUndockedAsync(inspector), Is.True);
		Assert.That(form.ScreenHost.ActiveScreen, Is.SameAs(current));
		Assert.That(form.ScreenHost.ActiveBlock, Is.SameAs(activeBlock));
		Assert.That(form.ScreenHost.TabControl.TabPages.Cast<TabPage>(), Is.EqualTo(tabs));
		Assert.That(form.ScreenHost.TabControl.SelectedTab, Is.SameAs(selectedTab));
		Assert.That(selections, Is.Zero, "Launching a detached inspector must never insert or select its main-frame tab.");
		Assert.That(current.HideRequests, Is.EqualTo(hides), "The main screen remains active even when its navigation guard would veto a change.");
		Assert.That(inspector.Host, Is.SameAs(form.ScreenHost));
		Assert.That(form.ScreenHost.IsScreenUndocked(inspector), Is.True);
		Assert.That(form.ScreenHost.OpenScreens, Is.EqualTo(new WinFormsApplicationScreen[] { permanent, current, inspector }));
		Assert.That(inspector.FindForm().StartPosition, Is.EqualTo(FormStartPosition.CenterParent));
		Assert.That(inspector.FindForm().Owner, Is.SameAs(form));
		Assert.That(displayed, Is.EqualTo(1));
		Assert.That(form.ScreenHost.ShowScreenUndocked(inspector), Is.True);
		Assert.That(displayed, Is.EqualTo(1), "Reopening an exact detached instance must only activate its existing window.");
		Assert.That(form.ScreenHost.DockScreen(inspector), Is.False, "Docking retains the ordinary active-screen navigation guard.");
		current.CancelHide = false;
		Assert.That(form.ScreenHost.DockScreen(inspector), Is.True);
		Assert.That(form.ScreenHost.TabControl.SelectedTab.Tag, Is.SameAs(inspector));
		Assert.That(await form.ScreenHost.CloseAllScreensAsync(), Is.True);
		Assert.That(form.ScreenHost.OpenScreens, Is.EqualTo(new[] { permanent }));
		Assert.That(permanent.IsDisposed, Is.False);
	}

	[Test]
	public async Task DirectDetachedLaunchRejectsUnsupportedPolicyOwnershipAndCanceledRequestsWithoutTakingOwnership() {
		using var single = new ProbeForm { ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		using var multiple = new ProbeForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		using var normal = new ProbeScreen();
		Assert.That(single.ScreenHost.ShowScreenUndocked(normal), Is.False);
		Assert.That(normal.Host, Is.Null);
		using var permanent = new PermanentScreen();
		Assert.That(multiple.ScreenHost.ShowScreenUndocked(permanent), Is.False);
		Assert.That(permanent.Host, Is.Null);
		using var emptyBlock = new WinFormsApplicationBlock {
			DefaultScreenDefinition = new ApplicationScreenDefinition(typeof(EmptyScreen), "Empty Workspace", ScreenActivationMode.MultiInstance, ScreenKind.Empty)
		};
		using var empty = new EmptyScreen { Block = emptyBlock };
		Assert.That(multiple.ScreenHost.ShowScreenUndocked(empty), Is.False);
		Assert.That(empty.Host, Is.Null);
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		IApplicationScreenHost host = multiple.ScreenHost;
		Assert.That(() => host.ShowScreenUndockedAsync(normal, canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(normal.Host, Is.Null);
		Assert.That(await host.ShowScreenUndockedAsync(normal), Is.True);
		Assert.That(() => single.ScreenHost.ShowScreenUndockedAsync(normal), Throws.ArgumentException);
		using var disposed = new ProbeScreen();
		disposed.Dispose();
		Assert.That(() => multiple.ScreenHost.ShowScreenUndocked(disposed), Throws.ArgumentException);
		var singleton = new SingletonScreen();
		Assert.That(multiple.ScreenHost.ShowScreenUndocked(singleton), Is.True);
		using var duplicate = new SingletonScreen();
		Assert.That(() => multiple.ScreenHost.ShowScreenUndocked(duplicate), Throws.ArgumentException);
		Assert.That(duplicate.Host, Is.Null);
	}

	[Test]
	public void ScreensMenuTracksExactInstancesAndDetachedWindowsAcrossRenameDockAndClose() {
		using var form = new ProbeForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		var first = new ProbeScreen { Title = "Inspector" };
		var second = new ProbeScreen { Title = "Inspector" };
		form.ShowScreen(first);
		form.ShowScreen(second);
		Assert.That(form.ScreensMenu.DropDownItems.OfType<ToolStripMenuItem>().Where(item => item.Tag is WinFormsApplicationScreen).Select(item => item.Tag), Is.EqualTo(new[] { first, second }));
		Assert.That(form.ScreensMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => ReferenceEquals(item.Tag, second)).Text, Does.Contain("(2)"));
		Assert.That(form.ScreenHost.UndockScreen(first), Is.True);
		var detached = form.ScreensMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => ReferenceEquals(item.Tag, first));
		Assert.That(detached.Text, Does.Contain("Undocked"));
		detached.PerformClick();
		Assert.That(form.ScreenHost.IsScreenUndocked(first), Is.True, "Menu navigation must bring the exact window forward without redocking or creating another screen.");
		Assert.That(form.ScreenHost.OpenScreens, Has.Length.EqualTo(2));
		first.Title = "Renamed inspector";
		Assert.That(form.ScreensMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => ReferenceEquals(item.Tag, first)).Text, Does.Contain("Renamed inspector"));
		Assert.That(form.ScreenHost.DockScreen(first), Is.True);
		Assert.That(form.ScreensMenu.DropDownItems.OfType<ToolStripMenuItem>().Single(item => ReferenceEquals(item.Tag, first)).Text, Does.Not.Contain("Undocked"));
		Assert.That(form.ScreenHost.CloseScreen(second), Is.True);
		Assert.That(form.ScreensMenu.DropDownItems.OfType<ToolStripMenuItem>().Where(item => item.Tag is WinFormsApplicationScreen).Select(item => item.Tag), Is.EqualTo(new[] { first }));
	}

	[Test]
	public async Task CloseAllChecksDetachedGuardsBeforeRemovingAnyInstanceAndLeavesPermanentScreens() {
		using var form = new ProbeForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		var permanent = new PermanentScreen { Title = "Dashboard" };
		var first = new ProbeScreen { Title = "First" };
		var second = new ProbeScreen { Title = "Second" };
		form.ShowScreen(permanent);
		form.ShowScreen(first);
		form.ShowScreen(second);
		form.ScreenHost.UndockScreen(second);
		second.CancelHide = true;
		Assert.That(await form.ScreenHost.CloseAllScreensAsync(), Is.False);
		Assert.That(first.IsDisposed || second.IsDisposed, Is.False, "A veto must leave the complete bulk-close set alive.");
		Assert.That(form.ScreenHost.OpenScreens, Has.Length.EqualTo(3));
		second.CancelHide = false;
		Assert.That(await form.ScreenHost.CloseAllScreensAsync(), Is.True);
		Assert.That(first.IsDisposed && second.IsDisposed, Is.True);
		Assert.That(permanent.IsDisposed, Is.False);
		Assert.That(form.ScreenHost.OpenScreens, Is.EqualTo(new[] { permanent }));
	}

	[Test]
	public async Task CloseAllOthersKeepsTheSuppliedDetachedInstanceAndRejectsForeignInstances() {
		using var form = new ProbeForm { ScreenMode = ScreenMode.MultiView, ShowInTaskbar = false, Location = new Point(-20000, -20000) };
		var keep = new ProbeScreen { Title = "Keep" };
		var other = new ProbeScreen { Title = "Other" };
		form.ShowScreen(keep);
		form.ShowScreen(other);
		form.ScreenHost.UndockScreen(keep);
		using var foreign = new ProbeScreen();
		Assert.That(() => form.ScreenHost.CloseAllOtherScreensAsync(foreign), Throws.ArgumentException);
		Assert.That(await form.ScreenHost.CloseAllOtherScreensAsync(keep), Is.True);
		Assert.That(other.IsDisposed, Is.True);
		Assert.That(keep.IsDisposed, Is.False);
		Assert.That(form.ScreenHost.IsScreenUndocked(keep), Is.True);
		Assert.That(form.ScreenHost.OpenScreens, Is.EqualTo(new[] { keep }));
	}

	private sealed class ProbeForm : MainForm {
		public ToolStripMenuItem ScreensMenu => ScreensToolStripMenuItem;
	}

	private class ProbeScreen : WinFormsApplicationScreen {
		[System.ComponentModel.Browsable(false), System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
		public bool CancelHide { get; set; }

		public int HideRequests { get; private set; }

		protected override void OnHide(ref bool cancelHide) {
			HideRequests++;
			cancelHide |= CancelHide;
		}
	}

	private sealed class PermanentScreen : ProbeScreen {
		public PermanentScreen() => ActivationMode = ScreenActivationMode.PermanentSingleton;
	}

	private sealed class SingletonScreen : ProbeScreen {
		public SingletonScreen() => ActivationMode = ScreenActivationMode.SingleInstance;
	}

	private sealed class EmptyScreen : ProbeScreen {
	}
}
