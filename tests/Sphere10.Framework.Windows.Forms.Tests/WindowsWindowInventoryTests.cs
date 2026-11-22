// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class WindowsWindowInventoryTests {
	[TestCase(false)]
	[TestCase(true)]
	public void CaptionFreeInventoryCompletesWhileTheOwningUiThreadDoesNotPump(bool includeProcessDetails) {
		using var owner = new CaptionProbeForm { Text = "Caption-free inventory fixture" };
		var handle = owner.Handle;
		owner.CaptionReads = 0;
		var inventory = Tools.Windows.Windowing.EnumerateAsync(includeProcessDetails, includeTitles: false, visibleOnly: false);
		try {
			// A UI owner joining this worker must not need to dispatch WM_GETTEXT to let it complete.
			Assert.That(WaitWithoutPumping(inventory), Is.True, "Caption-free inventory must complete without entering the owner's message loop.");
			var captured = inventory.GetAwaiter().GetResult().Single(window => window.Handle == handle);
			Assert.That(captured.Title, Is.Empty);
			Assert.That(captured.ProcessId, Is.EqualTo(Environment.ProcessId));
			Assert.That(captured.ProcessStartTimeUtc.HasValue, Is.EqualTo(includeProcessDetails));
			Assert.That(owner.CaptionReads, Is.Zero);
		} finally {
			ReleasePendingInventory(inventory);
		}
	}

	[Test]
	public void VisibleInventorySkipsHiddenAndMinimizedWindowsWhileDefaultInventoryRetainsThem() {
		using var visible = new CaptionProbeForm { Text = "Visible inventory fixture" };
		using var hidden = new CaptionProbeForm { Text = "Hidden inventory fixture" };
		using var minimized = new CaptionProbeForm { Text = "Minimized inventory fixture" };
		visible.Show();
		minimized.Show();
		minimized.WindowState = FormWindowState.Minimized;
		var hiddenHandle = hidden.Handle;
		Assert.That(WinAPI.USER32.IsIconic(minimized.Handle), Is.True);
		var filtered = Tools.Windows.Windowing.EnumerateAsync(includeProcessDetails: false, includeTitles: false, visibleOnly: true);
		try {
			Assert.That(WaitWithoutPumping(filtered), Is.True);
			var handles = filtered.GetAwaiter().GetResult().Select(window => window.Handle).ToArray();
			Assert.That(handles, Does.Contain(visible.Handle));
			Assert.That(handles, Does.Not.Contain(hiddenHandle));
			Assert.That(handles, Does.Not.Contain(minimized.Handle));
		} finally {
			ReleasePendingInventory(filtered);
		}
		var original = Tools.Windows.Windowing.EnumerateAsync(includeProcessDetails: false);
		ReleasePendingInventory(original);
		var windows = original.GetAwaiter().GetResult();
		Assert.That(windows.Single(window => window.Handle == hiddenHandle).Title, Is.EqualTo(hidden.Text));
		Assert.That(windows.Single(window => window.Handle == minimized.Handle).Title, Is.EqualTo(minimized.Text));
		Assert.That(hidden.CaptionReads, Is.GreaterThan(0), "The existing enumeration API still reads titles.");
	}

	private static bool WaitWithoutPumping(Task operation) {
		var elapsed = Stopwatch.StartNew();
		while (!operation.IsCompleted && elapsed.Elapsed < TimeSpan.FromSeconds(5))
			Thread.Sleep(1);
		return operation.IsCompleted;
	}

	private static void ReleasePendingInventory(Task operation) {
		var elapsed = Stopwatch.StartNew();
		while (!operation.IsCompleted && elapsed.Elapsed < TimeSpan.FromSeconds(5)) {
			System.Windows.Forms.Application.DoEvents();
			Thread.Sleep(1);
		}
		Assert.That(operation.IsCompleted, Is.True, "The inventory worker must be released before its owned fixture is disposed.");
	}

	#region Inner Types
	private sealed class CaptionProbeForm : Form {
		public CaptionProbeForm() {
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-20000, -20000);
		}

		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int CaptionReads { get; set; }

		protected override bool ShowWithoutActivation => true;

		protected override void WndProc(ref Message message) {
			const int getTextMessage = 0x000D;
			if (message.Msg == getTextMessage)
				CaptionReads++;
			base.WndProc(ref message);
		}
	}
	#endregion
}
