// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Tools;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class MainFormDpiNoticeTests {
	[Test]
	public void MovingSystemAwareWindowBetweenMonitorsDoesNotNotify() {
		LiteMainFormProbe form;
		using (Tools.Scope.EnterThreadDpiAwarenessScope(DpiAwarenessContext.SystemAware)) {
			form = new LiteMainFormProbe();
		}
		using (form) {
			var handle = form.Handle;
			using (Tools.Scope.EnterThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2)) {
				foreach (var screen in Screen.AllScreens) {
					var bounds = Tools.Windows.Display.GetBounds(screen.DeviceName);
					Assert.That(WinAPI.USER32.SetWindowPos(handle, IntPtr.Zero, bounds.X + 50, bounds.Y + 50, 0, 0,
						WinAPI.USER32.SetWindowPosFlags.SWP_NOSIZE | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE), Is.True);
				}
			}
			Assert.That(form.Notices, Is.Zero);
		}
	}

	[TestCase(false, DpiAwarenessContext.SystemAware, false, 2)]
	[TestCase(true, DpiAwarenessContext.SystemAware, false, 2)]
	[TestCase(false, DpiAwarenessContext.PerMonitorAwareV2, false, 0)]
	[TestCase(false, DpiAwarenessContext.Unaware, false, 0)]
	[TestCase(false, DpiAwarenessContext.SystemAware, true, 0)]
	public void DpiEventPreservesBaseHandlingAndReportsEachCompletedSystemAwareChange(bool fullMainForm, DpiAwarenessContext awareness, bool exiting, int expectedNotices) {
		LiteMainForm form;
		using (Tools.Scope.EnterThreadDpiAwarenessScope(awareness)) {
			form = fullMainForm ? new MainFormProbe() : new LiteMainFormProbe();
		}
		using (form) {
			form.StartPosition = FormStartPosition.Manual;
			form.Location = new Point(100, 100);
			form.ApplicationExiting = exiting;
			var handle = form.Handle;
			Assert.That(Screen.FromHandle(handle).Primary, Is.True);
			var events = new List<int>();
			form.DpiChanged += (_, e) => events.Add(e.DeviceDpiNew);
			var dpi = form.DeviceDpi;
			SendDpiChange(form, dpi);
			Assert.That(NoticeCount(form), Is.Zero, "An unchanged DPI must not notify.");
			SendDpiChange(form, dpi + 24);
			SendDpiChange(form, dpi + 48);
			Assert.That(events, Does.Contain(dpi + 24).And.Contain(dpi + 48), "Keep the inherited DPI event pipeline, including any native monitor correction.");
			Assert.That(NoticeCount(form), Is.EqualTo(expectedNotices));
		}
	}

	[Test]
	public void OnlyOverlappingNoticesAreSuppressed() => AsyncModalTests.RunWithMessageLoop(async owner => {
		LiteMainFormProbe form;
		using (Tools.Scope.EnterThreadDpiAwarenessScope(DpiAwarenessContext.SystemAware)) {
			form = new LiteMainFormProbe();
		}
		using (form) {
			form.StartPosition = FormStartPosition.Manual;
			form.Location = new Point(100, 100);
			_ = form.Handle;
			var dpi = form.DeviceDpi;
			var dismissed = new TaskCompletionSource();
			form.Notice = dismissed.Task;
			SendDpiChange(form, dpi + 24);
			SendDpiChange(form, dpi + 48);
			Assert.That(form.Notices, Is.EqualTo(1), "The first notice is still open.");
			dismissed.SetResult();
			await owner.InvokeAsyncEx(_ => Task.CompletedTask);
			SendDpiChange(form, dpi + 72);
			Assert.That(form.Notices, Is.EqualTo(2), "A later change must notify again after dismissal.");
		}
	});

	private static int NoticeCount(LiteMainForm form)
		=> form is MainFormProbe main ? main.Notices : ((LiteMainFormProbe)form).Notices;

	// Exercise the exposed event with a valid native payload; this does not claim OS delivery in SystemAware mode.
	private static void SendDpiChange(Form form, int dpi) {
		Assert.That(WinAPI.USER32.GetWindowRect(form.Handle, out var bounds), Is.True);
		var suggestedBounds = Marshal.AllocHGlobal(Marshal.SizeOf(bounds));
		using (Tools.Scope.ExecuteOnDispose(() => Marshal.FreeHGlobal(suggestedBounds))) {
			Marshal.StructureToPtr(bounds, suggestedBounds, false);
			WinAPI.USER32.SendMessage(form.Handle, 0x02E0, new IntPtr(dpi | (dpi << 16)), suggestedBounds);
		}
	}

	#region Inner Types

	private sealed class LiteMainFormProbe : LiteMainForm {
		public Task Notice = Task.CompletedTask;
		public int Notices { get; private set; }
		public override Task ReportInfoAsync(string title, string msg, CancellationToken cancellationToken = default) {
			Notices++;
			return Notice;
		}
	}

	private sealed class MainFormProbe : MainForm {
		public int Notices { get; private set; }
		public override Task ReportInfoAsync(string title, string msg, CancellationToken cancellationToken = default) {
			Notices++;
			return Task.CompletedTask;
		}
	}

	#endregion
}
