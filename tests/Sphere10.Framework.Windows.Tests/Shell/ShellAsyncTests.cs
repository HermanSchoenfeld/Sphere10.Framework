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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[Apartment(ApartmentState.STA)]
public class ShellAsyncTests {
	[Test]
	public void NativeMessageLayoutMatchesTheCurrentPointerWidth() {
		Assert.That(Marshal.SizeOf<WinAPI.USER32.MSG>(), Is.EqualTo(IntPtr.Size == 8 ? 48 : 32));
		Assert.That(Marshal.OffsetOf<WinAPI.USER32.MSG>(nameof(WinAPI.USER32.MSG.WParam)).ToInt32(), Is.EqualTo(IntPtr.Size == 8 ? 16 : 8));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task WorkerRetainsAndPumpsItsVisibleSheetAfterDispatchButDoesNotRetainHiddenInfrastructure(bool visible) {
		var shell = new OwnedSheetShell(visible);
		using var cleanup = Tools.Scope.ExecuteOnDispose(shell.CloseAndJoin);
		await shell.ShowPropertiesAsync("Owned Native Fixture");
		if (visible) {
			Assert.That(shell.Worker.IsAlive, Is.True, "Returning from Shell dispatch must not terminate the sheet's STA.");
			Assert.That(WinAPI.USER32.IsWindowVisible(shell.Window), Is.True);
			Assert.That(WinAPI.USER32.SendMessageTimeoutW(shell.Window, WinAPI.USER32.WM_NULL, UIntPtr.Zero, IntPtr.Zero,
				WinAPI.USER32.SendMessageTimeoutFlags.AbortIfHung, 2000, out _), Is.Not.EqualTo(IntPtr.Zero),
				"The retained STA must dispatch native messages after the caller's Task has completed.");
			shell.CloseAndJoin();
		} else {
			Assert.That(await Task.Run(() => shell.Worker.Join(TimeSpan.FromSeconds(3))), Is.True,
				"A hidden COM-style helper window must not keep the background worker alive.");
		}
		Assert.That(shell.Worker.IsAlive, Is.False);
		var destruction = Stopwatch.StartNew();
		while (shell.OriginalWindowExists && destruction.Elapsed < TimeSpan.FromSeconds(3))
			await Task.Delay(10);
		Assert.That(shell.OriginalWindowExists, Is.False,
			$"The original native fixture window must be destroyed after its worker exits: {shell.WindowTitle} (HWND {shell.Window}, native thread {shell.NativeThreadId}).");
	}

	[TestCase("Launch")]
	[TestCase("Open Item")]
	[TestCase("Open Directory")]
	[TestCase("Highlight")]
	[TestCase("Properties")]
	[TestCase("Empty")]
	[TestCase("Invoke Verb")]
	public async Task NativeOperationExecutesOnAnotherStaAndItsCompletionIsAwaited(string operation) {
		var caller = Environment.CurrentManagedThreadId;
		var shell = new RecordingShell();
		await InvokeAsync(shell, operation, CancellationToken.None);
		Assert.That(shell.Calls, Is.EqualTo(1));
		Assert.That(shell.OperationThreadId, Is.Not.EqualTo(caller));
		Assert.That(shell.OperationApartment, Is.EqualTo(ApartmentState.STA));
		Assert.That(shell.ComInitializationResult, Is.EqualTo(1), "The worker must initialize native COM before dispatching the Shell operation (S_FALSE means already initialized).");
	}

	[TestCase("Launch")]
	[TestCase("Open Item")]
	[TestCase("Open Directory")]
	[TestCase("Highlight")]
	[TestCase("Properties")]
	[TestCase("Empty")]
	[TestCase("Invoke Verb")]
	[TestCase("System Tool")]
	[TestCase("URI")]
	public async Task CancellationBeforeAdmissionDoesNotInvokeTheNativeOperation(string operation) {
		var shell = new RecordingShell();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await InvokeAsync(shell, operation, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(shell.Calls, Is.Zero);
	}

	[Test]
	public async Task CancellationAfterLaunchRetainsTheCallerOwnedObservationInsteadOfLosingItsHandle() {
		using var cancellation = new CancellationTokenSource();
		using var observation = Process.GetCurrentProcess();
		var handle = observation.SafeHandle;
		var shell = new RecordingShell { Observation = observation, BeforeReturn = cancellation.Cancel };
		var result = await shell.LaunchAsync("fixture.exe", cancellationToken: cancellation.Token);
		Assert.That(result, Is.SameAs(observation));
		Assert.That(cancellation.IsCancellationRequested, Is.True);
		Assert.That(handle.IsClosed, Is.False, "Once launch is admitted, the observation must reach its caller for disposal.");
	}

	[Test]
	public async Task NativeFailureIsPropagatedFromTheWorker() {
		var shell = new RecordingShell { BeforeReturn = () => throw new InvalidOperationException("Owned Shell failure") };
		await Assert.ThatAsync(async () => await shell.OpenShellItemAsync("fixture"), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Owned Shell failure"));
	}

	private static Task InvokeAsync(ShellUtility shell, string operation, CancellationToken token) => operation switch {
		"Launch" => shell.LaunchAsync("fixture.exe", cancellationToken: token),
		"Open Item" => shell.OpenShellItemAsync("fixture", cancellationToken: token),
		"Open Directory" => shell.OpenDirectoryAsync("fixture", token),
		"Highlight" => shell.HighlightFileInExplorerAsync("fixture", token),
		"Properties" => shell.ShowPropertiesAsync("fixture", cancellationToken: token),
		"Empty" => shell.EmptyRecycleBinAsync(cancellationToken: token),
		"Invoke Verb" => shell.InvokeShellItemVerbAsync(new WindowsShellItemInfo(), "Fixture Verb", token),
		"System Tool" => shell.OpenSystemToolAsync("fixture.exe", Array.Empty<string>(), token),
		"URI" => shell.OpenUriAsync(new Uri("fixture:operation"), token),
		_ => throw new ArgumentOutOfRangeException(nameof(operation))
	};

	private sealed class OwnedSheetShell : ShellUtility {
		private static int _nextWindowMarker;
		private readonly bool _visible;
		private readonly IntPtr _windowMarker = new(Interlocked.Increment(ref _nextWindowMarker));

		public OwnedSheetShell(bool visible) => _visible = visible;

		public Thread Worker { get; private set; }
		public uint NativeThreadId { get; private set; }
		public IntPtr Window { get; private set; }
		public string WindowTitle { get; } = "Owned Shell Sheet Fixture " + Guid.NewGuid().ToString("N");
		public bool OriginalWindowExists => Window != IntPtr.Zero && WinAPI.USER32.IsWindow(Window)
			&& WinAPI.USER32.GetWindowThreadProcessId(Window, out _) == NativeThreadId
			&& WinAPI.USER32.GetWindowLong(Window, -21) == _windowMarker;

		public override void ShowProperties(string path, IntPtr ownerWindow = default) {
			Worker = Thread.CurrentThread;
			NativeThreadId = WinAPI.KERNEL32.GetCurrentThreadId();
			Window = WinAPI.USER32.CreateWindowExW((uint)(WinAPI.USER32.WS_EX_NOACTIVATE | WinAPI.USER32.WS_EX_TOOLWINDOW), "STATIC", WindowTitle,
				0x80000000u | (_visible ? 0x10000000u : 0u), -20000, -20000, 320, 180, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
			if (Window == IntPtr.Zero)
				throw new Win32Exception(Marshal.GetLastPInvokeError());
			Marshal.SetLastPInvokeError(0);
			if (WinAPI.USER32.SetWindowLongPtr(new HandleRef(this, Window), -21, _windowMarker) == IntPtr.Zero && Marshal.GetLastPInvokeError() != 0)
				throw new Win32Exception(Marshal.GetLastPInvokeError());
		}

		public void CloseAndJoin() {
			if (Worker?.IsAlive == true && OriginalWindowExists)
				WinAPI.USER32.PostMessageW(Window, WinAPI.USER32.WM_CLOSE, UIntPtr.Zero, IntPtr.Zero);
			if (Worker != null && !Worker.Join(TimeSpan.FromSeconds(5)))
				throw new AssertionException("The owned Shell fixture window did not close and release its STA worker.");
		}
	}

	private sealed class RecordingShell : ShellUtility {
		public int Calls { get; private set; }
		public int OperationThreadId { get; private set; }
		public ApartmentState OperationApartment { get; private set; }
		public int ComInitializationResult { get; private set; }
		public Action BeforeReturn { get; init; }
		public Process Observation { get; init; }

		public override Process Launch(string target, string arguments = null, string workingDirectory = null, string verb = null,
			IntPtr ownerWindow = default, ProcessWindowStyle windowStyle = ProcessWindowStyle.Normal) {
			Record();
			return Observation;
		}

		public override void OpenShellItem(string parsingName, IntPtr ownerWindow = default) => Record();
		public override void OpenDirectory(string path) => Record();
		public override void HighlightFileInExplorer(string filePath) => Record();
		public override void ShowProperties(string path, IntPtr ownerWindow = default) => Record();
		public override void EmptyRecycleBin(string driveRoot = null, IntPtr ownerWindow = default, bool showConfirmation = true,
			bool showProgress = true, bool playSound = false) => Record();
		public override void InvokeShellItemVerb(WindowsShellItemInfo expected, string verbName) => Record();

		private void Record() {
			ComInitializationResult = WinAPI.OLE32.CoInitializeEx(IntPtr.Zero, WinAPI.OLE32.COINIT_APARTMENTTHREADED | WinAPI.OLE32.COINIT_DISABLE_OLE1DDE);
			Marshal.ThrowExceptionForHR(ComInitializationResult);
			using var balance = Tools.Scope.ExecuteOnDispose(WinAPI.OLE32.CoUninitialize);
			Calls++;
			OperationThreadId = Environment.CurrentManagedThreadId;
			OperationApartment = Thread.CurrentThread.GetApartmentState();
			BeforeReturn?.Invoke();
		}
	}
}
