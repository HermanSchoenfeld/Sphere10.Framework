// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class EtwTraceConsumerTests {
	[Test]
	public void EventAbiMatchesWindowsSdkX64Definitions() {
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_HEADER>(), Is.EqualTo(80));
		Assert.That(Marshal.SizeOf<WinAPI.TDH.TRACE_EVENT_INFO>(), Is.EqualTo(112));
		Assert.That(Marshal.SizeOf<WinAPI.TDH.EVENT_PROPERTY_INFO>(), Is.EqualTo(24));
		if (IntPtr.Size == 8) {
			Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_RECORD>(), Is.EqualTo(112));
			Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE>(), Is.EqualTo(88));
			Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.TRACE_LOGFILE_HEADER>(), Is.EqualTo(280));
			Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.EVENT_TRACE_LOGFILEW>(), Is.EqualTo(448));
			Assert.That(Marshal.OffsetOf<WinAPI.ADVAPI32.EVENT_TRACE_LOGFILEW>(nameof(WinAPI.ADVAPI32.EVENT_TRACE_LOGFILEW.EventRecordCallback)).ToInt32(), Is.EqualTo(424));
			Assert.That(Marshal.OffsetOf<WinAPI.ADVAPI32.EVENT_RECORD>(nameof(WinAPI.ADVAPI32.EVENT_RECORD.UserData)).ToInt32(), Is.EqualTo(96));
		}
	}

	[Test]
	public async Task CancelledConsumersAndUpdatesDoNotQueryOrChangeNativeSessions() {
		var traces = new FixtureTraceUtil();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await traces.SetRealTimeAsync(traces.Expected, true, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await traces.ConsumeRealTimeAsync(traces.Expected, _ => Assert.Fail("No callback expected."), cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(traces.Queries, Is.Zero);
	}

	[TestCase("missing")]
	[TestCase("guid")]
	[TestCase("handle")]
	[TestCase("file")]
	public async Task ReplacedSessionsAreRejectedBeforeAnyNativeUpdateOrConsumer(string change) {
		var traces = new FixtureTraceUtil();
		traces.Current = change switch {
			"missing" => null,
			"guid" => traces.Expected with { Guid = Guid.NewGuid() },
			"handle" => traces.Expected with { SessionId = 2 },
			_ => traces.Expected with { LogFilePath = "foreign.etl" }
		};
		Assert.That(() => traces.SetRealTime(traces.Expected, true), Throws.InvalidOperationException);
		await Assert.ThatAsync(async () => await traces.ConsumeRealTimeAsync(traces.Expected, _ => Assert.Fail("No callback expected.")), Throws.InvalidOperationException);
	}

	[Test]
	public void FlatManifestDecodingPreservesDynamicPayloadAndCopiesBytes() {
		var schema = new[] {
			new EtwTracePropertyDecoder.Property("Length", 0, 6, 1, 0),
			new EtwTracePropertyDecoder.Property("Payload", 2, 14, 1, 0)
		};
		var raw = new byte[] { 3, 0, 11, 22, 33 };
		var decoded = EtwTracePropertyDecoder.Decode(schema, raw, 8);
		Assert.That(decoded.Select(property => property.Name), Is.EqualTo(new[] { "Length", "Payload" }));
		Assert.That(decoded[1].Data, Is.EqualTo(new byte[] { 11, 22, 33 }));
		raw[2] = 99;
		Assert.That(decoded[1].Data[0], Is.EqualTo(11));
	}

	[Test]
	public void FlatManifestDecodingAcceptsZeroLengthDynamicBinary() {
		var schema = new[] {
			new EtwTracePropertyDecoder.Property("Length", 0, 6, 1, 0),
			new EtwTracePropertyDecoder.Property("Payload", 2, 14, 1, 0)
		};
		Assert.That(EtwTracePropertyDecoder.Decode(schema, new byte[] { 0, 0 }, 8).Last().Data, Is.Empty);
	}

	[Test]
	public void ExplicitZeroLengthStringsDoNotConsumeTheFollowingNumericProperty() {
		var fixedSchema = new[] {
			new EtwTracePropertyDecoder.Property("Empty", 16, 1, 1, 0),
			new EtwTracePropertyDecoder.Property("Following", 0, 6, 1, 0)
		};
		var fixedResult = EtwTracePropertyDecoder.Decode(fixedSchema, new byte[] { 42, 0 }, 8);
		Assert.That(fixedResult[0].Data, Is.Empty);
		Assert.That(fixedResult[1].Data, Is.EqualTo(new byte[] { 42, 0 }));
		var referencedSchema = new[] {
			new EtwTracePropertyDecoder.Property("Length", 0, 6, 1, 0),
			new EtwTracePropertyDecoder.Property("Empty", 2, 2, 1, 0),
			new EtwTracePropertyDecoder.Property("Following", 0, 6, 1, 0)
		};
		var referencedResult = EtwTracePropertyDecoder.Decode(referencedSchema, new byte[] { 0, 0, 42, 0 }, 8);
		Assert.That(referencedResult[1].Data, Is.Empty);
		Assert.That(referencedResult[2].Data, Is.EqualTo(new byte[] { 42, 0 }));
	}

	[Test]
	public void UnsupportedOrTruncatedSchemasDoNotInventProperties() {
		Assert.That(EtwTracePropertyDecoder.Decode(new[] { new EtwTracePropertyDecoder.Property("Structure", 1, 8, 1, 0) }, new byte[4], 8), Is.Empty);
		Assert.That(EtwTracePropertyDecoder.Decode(new[] { new EtwTracePropertyDecoder.Property("TooLong", 0, 10, 1, 0) }, new byte[4], 8), Is.Empty);
		Assert.That(EtwTracePropertyDecoder.Decode(new[] { new EtwTracePropertyDecoder.Property("ForwardReference", 2, 14, 1, 0) }, new byte[4], 8), Is.Empty);
	}

	private sealed class FixtureTraceUtil : EtwTraceUtil {
		public EtwTraceSessionInfo Expected { get; } = new("OwnedFixture", Guid.NewGuid(), 1, "owned.etl", WinAPI.ADVAPI32.EVENT_TRACE_REAL_TIME_MODE, 0);
		public EtwTraceSessionInfo Current { get; set; }
		public int Queries { get; private set; }
		public FixtureTraceUtil() { Current = Expected; }
		public override EtwTraceSessionInfo[] GetSessions() {
			Queries++;
			return Current == null ? Array.Empty<EtwTraceSessionInfo>() : new[] { Current };
		}
	}
}
