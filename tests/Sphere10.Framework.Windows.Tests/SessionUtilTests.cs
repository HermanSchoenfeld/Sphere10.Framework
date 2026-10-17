// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SessionUtilTests {
	[Test]
	[Apartment(ApartmentState.STA)]
	public async Task CurrentSessionQueriesRunOnAWorkerEvenWhenTheirValuesAreImmediatelyAvailable() {
		var caller = Environment.CurrentManagedThreadId;
		var sessions = new RecordingCurrentSession();
		Assert.That(await sessions.GetCurrentSessionIdAsync(), Is.EqualTo(42));
		Assert.That(sessions.ReadThreadId, Is.Not.EqualTo(caller));
		Assert.That(await sessions.IsCurrentSessionRemoteAsync(), Is.True);
		Assert.That(sessions.ReadThreadId, Is.Not.EqualTo(caller));
	}

	[Test]
	public async Task CanceledCurrentSessionQueriesNeverReadNativeState() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var sessions = new RecordingCurrentSession();
		await Assert.ThatAsync(async () => await sessions.GetCurrentSessionIdAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await sessions.IsCurrentSessionRemoteAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(sessions.ReadThreadId, Is.Zero);
	}

	[Test]
	public void WtsInfoLayoutRetainsSdkOffsetsAndTheCorrectUtf16Arrays() {
		Assert.That(Marshal.SizeOf<WinAPI.WTSAPI32.WTSINFO>(), Is.EqualTo(216));
		Assert.That(Marshal.OffsetOf<WinAPI.WTSAPI32.WTSINFO>(nameof(WinAPI.WTSAPI32.WTSINFO.LogonTime)).ToInt32(), Is.EqualTo(200));
	}

	[Test]
	public void ReusedSessionNumberOrUnavailableLogonTimeCannotMatchACapturedIdentity() {
		var first = new WindowsSessionInfo { SessionId = 7, ServerName = "Host", UserName = "User", Domain = "Domain", LogonTimeUtc = DateTime.UtcNow };
		Assert.That(first.MatchesIdentity(first with { ServerName = "host", UserName = "user" }), Is.True);
		Assert.That(first.MatchesIdentity(first with { LogonTimeUtc = first.LogonTimeUtc.Value.AddSeconds(1) }), Is.False);
		Assert.That(first.MatchesIdentity(first with { UserName = "Other" }), Is.False);
		Assert.That(first.MatchesIdentity(first with { LogonTimeUtc = null }), Is.False);
	}

	[Test]
	public void ListenerDetailsWithZeroSessionIdRemainPartialAndCannotTargetSessionZero() {
		var returned = new WinAPI.WTSAPI32.WTSINFO { SessionId = 0, State = WinAPI.WTSAPI32.WTS_CONNECTSTATE_CLASS.Listen,
			UserName = "Incorrect Provider User", LogonTime = DateTime.UtcNow.ToFileTimeUtc() };
		var row = SessionUtil.ProjectSessionInformation(65536, Environment.MachineName, returned);
		Assert.That(row.SessionId, Is.EqualTo(65536));
		Assert.That(row.LogonTimeUtc, Is.Null);
		Assert.That(row.UserName, Is.Null);
		Assert.That(row.ReadErrorCode, Is.EqualTo(13));
		Assert.That(row.IsVerified, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ManagementRejectsUnverifiedSessionBeforeAnyNativeAction(bool logoff) {
		var sessions = new SessionUtil();
		var unverified = new WindowsSessionInfo { SessionId = uint.MaxValue, ServerName = Environment.MachineName };
		Assert.That(() => { if (logoff) sessions.Logoff(unverified); else sessions.Disconnect(unverified); }, Throws.InstanceOf<InvalidOperationException>());
	}

	[Test]
	public async Task CanceledInventoryDoesNotOpenOrQueryAWindowsServer() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await new SessionUtil().GetSessionsAsync("not-an-existing-server", cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task ReadOnlyLocalInventoryReturnsDetachedRowsAndTheCurrentSession() {
		var utility = new SessionUtil();
		var rows = await utility.GetSessionsAsync();
		Assert.That(rows, Is.Not.Empty);
		Assert.That(rows, Has.Some.Property(nameof(WindowsSessionInfo.SessionId)).EqualTo(utility.CurrentSessionId));
		Assert.That(Array.TrueForAll(rows, row => row.ServerName == Environment.MachineName), Is.True);
	}

	[Test]
	public void EmptyMemoryAndBorrowedLocalServerScopesDisposeIdempotentlyAndRejectFurtherAccess() {
		var memory = new WtsMemoryScope(IntPtr.Zero);
		memory.Dispose();
		memory.Dispose();
		Assert.That(() => memory.Pointer, Throws.InstanceOf<ObjectDisposedException>());
		var server = new WtsServerScope();
		Assert.That(server.Handle, Is.EqualTo(IntPtr.Zero));
		server.Dispose();
		server.Dispose();
		Assert.That(() => server.Handle, Throws.InstanceOf<ObjectDisposedException>());
	}

	private sealed class RecordingCurrentSession : SessionUtil {
		public int ReadThreadId { get; private set; }
		public override uint CurrentSessionId { get { ReadThreadId = Environment.CurrentManagedThreadId; return 42; } }
		public override bool IsCurrentSessionRemote { get { ReadThreadId = Environment.CurrentManagedThreadId; return true; } }
	}
}
