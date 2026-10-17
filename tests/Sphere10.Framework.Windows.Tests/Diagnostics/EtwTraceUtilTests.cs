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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class EtwTraceUtilTests {
	[Test]
	public void EtwPropertyLayoutUsesNativePointerWidthsAndDllFacade() {
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.WNODE_HEADER>(), Is.EqualTo(48));
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>(), Is.EqualTo(IntPtr.Size == 8 ? 120 : 116));
		Assert.That(Marshal.OffsetOf<WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES>(nameof(WinAPI.ADVAPI32.EVENT_TRACE_PROPERTIES.LoggerThreadId)).ToInt32(), Is.EqualTo(104));
	}

	[Test]
	public async Task PreCanceledEnumerationDoesNotEnterNativeQuery() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(() => new EtwTraceUtil().GetSessionsAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task NativeSessionEnumerationIsReadOnlyAndReturnsDetachedNamesOrRawAccessFailure() {
		try {
			var sessions = await new EtwTraceUtil().GetSessionsAsync();
			Assert.That(sessions.Select(session => session.Name), Is.All.Not.Null);
			Assert.That(sessions.Select(session => session.LogFilePath), Is.All.Not.Null);
		} catch (Win32Exception error) {
			Assert.That(error.NativeErrorCode, Is.EqualTo(5), "Only a real access restriction is an acceptable unavailable read-only inventory.");
		}
	}
}
