// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture, NonParallelizable, Apartment(ApartmentState.STA)]
public class WindowHighlightScopeTests {
	[TestCase(false)]
	[TestCase(true)]
	public void FrameIsHollowClickThroughAndDoesNotActivateAnotherWindowThenDisposesOnItsOwnerThread(bool asynchronous) => RunWithMessageLoop(async owner => {
		var foreground = WinAPI.USER32.GetForegroundWindow();
		var bounds = new Rectangle(-19000, -19000, 360, 220);
		using var highlight = new WindowHighlightScope(bounds, Color.Red, 4);
		var frame = FormsApplication.OpenForms.Cast<Form>().Single(form => form.Name == "WindowHighlightForm");
		var handle = frame.Handle;
		Assert.That(ReadNativePhysicalBounds(handle), Is.EqualTo(bounds));
		Assert.That(frame.BackColor, Is.EqualTo(Color.Red));
		Assert.That(frame.ShowInTaskbar, Is.False);
		Assert.That(frame.Region.IsVisible(new Point(frame.Width / 2, frame.Height / 2)), Is.False);
		Assert.That(frame.Region.IsVisible(new Point(1, 1)), Is.True);
		var styles = WinAPI.USER32.GetWindowLong(handle, -20).ToInt64();
		Assert.That(styles & WinAPI.USER32.WS_EX_TOPMOST, Is.Not.Zero, "The outline must remain natively topmost without Form.TopMost's managed focus path.");
		Assert.That(styles & WinAPI.USER32.WS_EX_NOACTIVATE, Is.Not.Zero);
		Assert.That(styles & WinAPI.USER32.WS_EX_LAYERED, Is.Not.Zero);
		Assert.That(styles & WinAPI.USER32.WS_EX_TRANSPARENT, Is.Not.Zero);
		Assert.That(WinAPI.USER32.GetForegroundWindow(), Is.Not.EqualTo(handle),
			$"The overlay must never become foreground. Before={foreground}; owner={owner.Handle}; frame={handle}.");
		Assert.That(frame.ContainsFocus, Is.False);
		highlight.Bounds = new Rectangle(bounds.X + 10, bounds.Y + 20, 400, 260);
		Assert.That(ReadNativePhysicalBounds(handle), Is.EqualTo(highlight.Bounds));
		if (asynchronous)
			await highlight.DisposeAsync();
		else
			highlight.Dispose();
		Assert.That(frame.IsDisposed, Is.True);
		Assert.That(WinAPI.USER32.IsWindow(handle), Is.False);
	});

	[Test, Combinatorial]
	public void PhysicalBoundsAndOutlineIgnoreCallerAndTargetAwarenessWithoutChangingTheirContexts(
		[Values(DpiAwarenessContext.Unaware, DpiAwarenessContext.SystemAware, DpiAwarenessContext.PerMonitorAwareV2)] DpiAwarenessContext callerContext,
		[Values(DpiAwarenessContext.Unaware, DpiAwarenessContext.SystemAware, DpiAwarenessContext.PerMonitorAwareV2)] DpiAwarenessContext targetContext
	) => RunWithMessageLoop(_ => {
		// Create only an owned offscreen target; the application's process-wide awareness and desktop stay untouched.
		var originalContext = WinAPI.USER32.GetThreadDpiAwarenessContext();
		Form target = null;
		using var releaseTarget = Tools.Scope.ExecuteOnDispose(() => target?.Dispose());
		using (new ThreadDpiAwarenessScope(targetContext)) {
			target = new Form {
				AutoScaleMode = AutoScaleMode.None, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual,
				Bounds = new Rectangle(-18000, -18000, 360, 220)
			};
			Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetWindowDpiAwarenessContext(target.Handle),
				new IntPtr((int)targetContext)), Is.True, "Create the owned native target in the intended awareness context.");
		}
		var nativeTargetContext = WinAPI.USER32.GetWindowDpiAwarenessContext(target.Handle);
		var targetBounds = ReadNativePhysicalBounds(target.Handle);
		Assert.That(targetBounds.Left, Is.LessThan(0));
		Assert.That(targetBounds.Top, Is.LessThan(0));
		Assert.That(WindowsWindow.TryGetInfo(target.Handle, out var snapshot), Is.True);
		using (new ThreadDpiAwarenessScope(callerContext)) {
			var caller = WinAPI.USER32.GetThreadDpiAwarenessContext();
			Assert.That(WindowsWindow.GetPhysicalBounds(snapshot), Is.EqualTo(targetBounds));
			Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), caller), Is.True,
				"Reading physical geometry must restore the caller's exact DPI context.");
			using var highlight = new WindowHighlightScope(targetBounds, Color.Red, 4);
			var frame = FormsApplication.OpenForms.Cast<Form>().Single(form => form.Name == "WindowHighlightForm");
			var handle = frame.Handle;
			Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetWindowDpiAwarenessContext(handle),
				new IntPtr((int)DpiAwarenessContext.PerMonitorAwareV2)), Is.True, "The overlay's own HWND must use physical desktop pixels.");
			Assert.That(ReadNativePhysicalBounds(handle), Is.EqualTo(targetBounds));
			Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), caller), Is.True,
				"Showing the outline must not change the host UI thread's awareness.");
			TestContext.Progress.WriteLine($"Caller={callerContext}; target={targetContext}; target DPI={WinAPI.USER32.GetDpiForWindow(target.Handle)}; "
				+ $"physical overlay monitor DPI={WinAPI.USER32.GetDpiForWindow(handle)}; bounds={targetBounds}. No display scale was changed.");

			// A later update can run from a different caller context; it must still use the supplied physical pixels.
			var resized = new Rectangle(targetBounds.X - 30, targetBounds.Y - 20, targetBounds.Width + 70, targetBounds.Height + 50);
			var updateContext = callerContext == DpiAwarenessContext.Unaware ? DpiAwarenessContext.PerMonitorAwareV2 : DpiAwarenessContext.Unaware;
			using (new ThreadDpiAwarenessScope(updateContext)) {
				var updater = WinAPI.USER32.GetThreadDpiAwarenessContext();
				highlight.Bounds = resized;
				Assert.That(highlight.Bounds, Is.EqualTo(resized));
				Assert.That(ReadNativePhysicalBounds(handle), Is.EqualTo(resized));
				Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), updater), Is.True,
					"Bounds setters and getters must restore their own caller context.");
			}
			highlight.Dispose();
			Assert.That(WinAPI.USER32.IsWindow(handle), Is.False);
			Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), caller), Is.True,
				"Disposing the overlay must restore its caller's DPI context.");
		}
		Assert.That(ReadNativePhysicalBounds(target.Handle), Is.EqualTo(targetBounds), "Highlighting must never move or resize the target.");
		Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetWindowDpiAwarenessContext(target.Handle), nativeTargetContext), Is.True,
			"The target's original HWND awareness must remain unchanged.");
		Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), originalContext), Is.True);
		return Task.CompletedTask;
	});

	[Test]
	public void DpiChangeSuggestedRectangleDoesNotRescaleSuppliedPhysicalBounds() => RunWithMessageLoop(_ => {
		var bounds = new Rectangle(-19000, -19000, 360, 220);
		using var highlight = new WindowHighlightScope(bounds, Color.Red, 4);
		var frame = FormsApplication.OpenForms.Cast<Form>().Single(form => form.Name == "WindowHighlightForm");
		var handle = frame.Handle;
		var nativeDpi = WinAPI.USER32.GetDpiForWindow(handle);
		var changedDpi = nativeDpi == 144 ? 192 : 144;
		using var suggested = new UnmanagedMemoryScope(Marshal.SizeOf<WinAPI.RECT>());
		Marshal.StructureToPtr(new WinAPI.RECT { Left = -18500, Top = -18500, Right = -17800, Bottom = -18000 }, suggested.Item, false);
		var context = WinAPI.USER32.GetThreadDpiAwarenessContext();
		// Deliver only to the owned frame; this checks WM_DPICHANGED handling without changing any monitor or user setting.
		WinAPI.USER32.SendMessage(new HandleRef(frame, handle), 0x02E0, new IntPtr(changedDpi | changedDpi << 16), suggested.Item);
		Assert.That(ReadNativePhysicalBounds(handle), Is.EqualTo(bounds), "Windows' DPI suggestion must not scale an already physical target rectangle twice.");
		Assert.That(highlight.Bounds, Is.EqualTo(bounds));
		Assert.That(frame.Region.IsVisible(new Point(frame.Width / 2, frame.Height / 2)), Is.False);
		Assert.That(WinAPI.USER32.AreDpiAwarenessContextsEqual(WinAPI.USER32.GetThreadDpiAwarenessContext(), context), Is.True);
		return Task.CompletedTask;
	});

	[Test]
	public void ActualFrameReceivesNoActivationOrFocusOnFirstShowResizeAndReshow() => RunWithMessageLoop(async owner => {
		var type = typeof(WindowHighlightScope).Assembly.GetType("Sphere10.Framework.Windows.Forms.WindowHighlightForm", throwOnError: true);
		using var frame = (Form)Sphere10.Framework.TypeActivator.ActivateWithCompatibleArgs(type, Array.Empty<object>());
		var activated = 0;
		var focused = 0;
		frame.Activated += (_, _) => activated++;
		frame.GotFocus += (_, _) => focused++;
		frame.Bounds = new Rectangle(-19000, -19000, 360, 220);
		using var messages = new WindowActivationSpy(frame.Handle);
		frame.Show();
		await owner.InvokeAsync(() => { });
		frame.Bounds = new Rectangle(-19010, -19010, 400, 260);
		frame.Hide();
		frame.Show();
		await owner.InvokeAsync(() => { });
		Assert.That(activated, Is.Zero, "Observe the frame's own activation events from before its first HWND/show.");
		Assert.That(focused, Is.Zero);
		Assert.That(messages.ActivationCount, Is.Zero, "The frame must not receive active WM_ACTIVATE messages during show/resize/reshow.");
		Assert.That(messages.FocusCount, Is.Zero, "The frame must not receive WM_SETFOCUS.");
		Assert.That(WinAPI.USER32.GetForegroundWindow(), Is.Not.EqualTo(frame.Handle));
		Assert.That(WinAPI.USER32.SendMessage(new HandleRef(frame, frame.Handle), (uint)WinAPI.USER32.WM_MOUSEACTIVATE, IntPtr.Zero, IntPtr.Zero).ToInt64(),
			Is.EqualTo(WinAPI.USER32.MA_NOACTIVATE));
		Assert.That(WinAPI.USER32.SendMessage(new HandleRef(frame, frame.Handle), (uint)WinAPI.USER32.WM_NCHITTEST, IntPtr.Zero, IntPtr.Zero).ToInt64(),
			Is.EqualTo(WinAPI.USER32.HTTRANSPARENT));
	});

	[TestCase("Process")]
	[TestCase("Thread")]
	[TestCase("Class")]
	public void BoundsRequireTheCapturedNativeWindowIdentity(string mismatch) => RunWithMessageLoop(_ => {
		using var target = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(-18000, -18000, 300, 200) };
		Assert.That(WindowsWindow.TryGetInfo(target.Handle, out var snapshot), Is.True);
		Assert.That(WindowsWindow.GetBounds(snapshot), Is.EqualTo(target.Bounds));
		var stale = mismatch switch {
			"Process" => snapshot with { ProcessStartTimeUtc = snapshot.ProcessStartTimeUtc.Value.AddSeconds(-1) },
			"Thread" => snapshot with { ThreadId = snapshot.ThreadId + 1 },
			_ => snapshot with { ClassName = snapshot.ClassName + "-replaced" }
		};
		Assert.That(() => WindowsWindow.GetBounds(stale), Throws.InvalidOperationException);
		Assert.That(() => WindowsWindow.GetPhysicalBounds(stale), Throws.InvalidOperationException);
		Assert.That(() => WindowMessageSender.Send(stale, 0, UIntPtr.Zero, IntPtr.Zero, WinAPI.USER32.SendMessageTimeoutFlags.AbortIfHung, 500),
			Throws.InvalidOperationException);
		Assert.That(WindowMessageSender.Send(snapshot, 0, UIntPtr.Zero, IntPtr.Zero, WinAPI.USER32.SendMessageTimeoutFlags.AbortIfHung, 500).Succeeded, Is.True,
			"WM_NULL may be sent only to the exact owned live window; the liveness check must not mistake an undefined live exit time for termination.");
		Assert.That(WindowsWindow.GetBounds(snapshot), Is.EqualTo(target.Bounds), "Geometry inspection must leave the owned target untouched.");
		return Task.CompletedTask;
	});

	[Test]
	public void ClosedAndUnverifiedWindowsCannotSupplyHighlightBounds() => RunWithMessageLoop(_ => {
		using var target = new Form { ShowInTaskbar = false };
		Assert.That(WindowsWindow.TryGetInfo(target.Handle, out var snapshot), Is.True);
		Assert.That(() => WindowsWindow.GetBounds(snapshot with { ProcessStartTimeUtc = null }), Throws.ArgumentException);
		Assert.That(() => WindowsWindow.GetPhysicalBounds(snapshot with { ProcessStartTimeUtc = null }), Throws.ArgumentException);
		target.Dispose();
		Assert.That(() => WindowsWindow.GetBounds(snapshot), Throws.InvalidOperationException);
		Assert.That(() => WindowsWindow.GetPhysicalBounds(snapshot), Throws.InvalidOperationException);
		return Task.CompletedTask;
	});

	private static Rectangle ReadNativePhysicalBounds(IntPtr handle) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		if (!WinAPI.USER32.GetWindowRect(handle, out var bounds))
			throw new Win32Exception(Marshal.GetLastPInvokeError());
		return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => { failure = new AssertionException("Owned highlight fixture timed out."); owner.Close(); };
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				// Return from the owner's initial Show/activation before measuring another window's behavior.
				await Task.Yield();
				await test(owner);
				completed = true;
			}
			catch (Exception error) { failure = error; }
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}

	private sealed class WindowActivationSpy : NativeWindow, IDisposable {
		public WindowActivationSpy(IntPtr handle) => AssignHandle(handle);

		public int ActivationCount { get; private set; }
		public int FocusCount { get; private set; }

		public void Dispose() => ReleaseHandle();

		protected override void WndProc(ref Message message) {
			if (message.Msg == 0x0006 && (message.WParam.ToInt64() & 0xFFFF) != 0)
				ActivationCount++;
			if (message.Msg == 0x0007)
				FocusCount++;
			base.WndProc(ref message);
		}
	}
}
