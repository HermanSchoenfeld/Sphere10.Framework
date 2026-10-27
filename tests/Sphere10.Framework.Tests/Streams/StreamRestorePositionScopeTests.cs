// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamRestorePositionScopeTests {

	[Test]
	public void LegacySeekPositionScopeRetainsItsZeroPositionExit() {
		using var stream = new MemoryStream(new byte[16]);
		stream.Position = 5;
		using (var scope = stream.EnterRestoreSeekPositionScope()) {
			Assert.That(stream.Position, Is.EqualTo(5));
			stream.Position = 9;
		}
		Assert.That(stream.Position, Is.Zero);
		Assert.That(stream.CanRead, Is.True);
	}

	[Test]
	public void NestedScopesRestoreTheirOwnPositionsWithoutClosingTheStream() {
		using var stream = new MemoryStream(new byte[16]);
		stream.Position = 3;
		using (stream.EnterRestorePositionSeek(8, SeekOrigin.Begin)) {
			Assert.That(stream.Position, Is.EqualTo(8));
			using (stream.EnterRestorePositionSeek(2, SeekOrigin.Current)) {
				Assert.That(stream.Position, Is.EqualTo(10));
				stream.Position = 12;
			}
			Assert.That(stream.Position, Is.EqualTo(8));
			stream.Position = 5;
		}
		Assert.That(stream.Position, Is.EqualTo(3));
		Assert.That(stream.CanRead, Is.True);
	}

	[Test]
	public void ThrowingInsideUsingRestoresTheOriginalPosition() {
		using var stream = new MemoryStream(new byte[16]);
		stream.Position = 3;
		Assert.That(() => {
			using var restore = stream.EnterRestorePositionSeek(8, SeekOrigin.Begin);
			stream.Position = 12;
			throw new InvalidOperationException("Operation failed");
		}, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Operation failed"));
		Assert.That(stream.Position, Is.EqualTo(3));
		Assert.That(stream.CanRead, Is.True);
	}

	[Test]
	public void GuardCaptureSeekAndSetterRestorationRetainTheirOrder() {
		using var stream = new TrackingStream();
		stream.Position = 4;
		stream.ClearOperations();
		var restore = stream.EnterRestorePositionSeek(2, SeekOrigin.Begin);
		Assert.That(stream.Operations, Is.EqualTo(new[] { "can seek", "get position", "seek 2 Begin" }));
		restore.Dispose();
		Assert.That(stream.Operations, Is.EqualTo(new[] { "can seek", "get position", "seek 2 Begin", "set position 4" }));
	}

	[Test]
	public void ScopeWithoutASeekRetainsTheExistingImmediateSeek() {
		using var stream = new TrackingStream();
		stream.Position = 4;
		stream.ClearOperations();
		var restore = stream.EnterRestorePositionScope();
		Assert.That(stream.Operations, Is.EqualTo(new[] { "get position", "can seek", "get position", "seek 4 Begin" }));
		stream.Position = 9;
		restore.Dispose();
		Assert.That(stream.Position, Is.EqualTo(4));
	}

	[Test]
	public void NonSeekableStreamIsRejectedBeforeCapturingOrSeeking() {
		using var stream = new TrackingStream { AllowSeek = false };
		Assert.That(() => stream.EnterRestorePositionSeek(1, SeekOrigin.Begin), Throws.TypeOf<ArgumentException>().With.Property("ParamName").EqualTo("stream"));
		Assert.That(stream.Operations, Is.EqualTo(new[] { "can seek" }));
	}

	[Test]
	public void FailedInitialSeekPropagatesWithoutAutomaticallyRestoring() {
		using var stream = new TrackingStream();
		stream.Position = 4;
		stream.ThrowAfterSeek = true;
		stream.ClearOperations();
		Assert.That(() => stream.EnterRestorePositionSeek(2, SeekOrigin.Begin), Throws.TypeOf<IOException>().With.Message.EqualTo("Seek failed after moving"));
		Assert.That(stream.Operations, Is.EqualTo(new[] { "can seek", "get position", "seek 2 Begin" }));
		Assert.That(stream.Position, Is.EqualTo(2));
	}

	[Test]
	public void ExplicitDisposalCallsTheFinalizerEachTime() {
		using var stream = new TrackingStream();
		stream.Position = 4;
		var restore = stream.EnterRestorePositionSeek(1, SeekOrigin.Begin);
		restore.Dispose();
		stream.Position = 9;
		stream.ClearOperations();
		restore.Dispose();
		Assert.That(stream.Operations, Is.EqualTo(new[] { "set position 4" }));
		Assert.That(stream.Position, Is.EqualTo(4));
	}

	[Test]
	public void RestorationRunsInlineOnTheCallingThread() {
		using var stream = new TrackingStream();
		stream.Position = 4;
		var restore = stream.EnterRestorePositionSeek(1, SeekOrigin.Begin);
		var callingThread = Environment.CurrentManagedThreadId;
		stream.PositionChanging = () => Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(callingThread));
		stream.ClearOperations();
		restore.Dispose();
		Assert.That(stream.Operations, Is.EqualTo(new[] { "set position 4" }));
		Assert.That(stream.Position, Is.EqualTo(4));
	}

	[Test]
	public void FailedRestorationCanBeRetried() {
		using var stream = new TrackingStream();
		stream.Position = 4;
		var restore = stream.EnterRestorePositionSeek(1, SeekOrigin.Begin);
		stream.FailNextPositionSet = true;
		Assert.That(() => restore.Dispose(), Throws.TypeOf<IOException>().With.Message.EqualTo("Restore rejected"));
		Assert.That(stream.Position, Is.EqualTo(1));
		restore.Dispose();
		Assert.That(stream.Position, Is.EqualTo(4));
	}

	[Test]
	public void ConstructionRequiresAStream() {
		Assert.That(() => new RestorePositionScope(null, 0), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("stream"));
	}

	[Test]
	public void EntryPointsRequireAStream([Values] bool seek) {
		Stream stream = null;
		Assert.That(() => {
			using var scope = seek ? stream.EnterRestorePositionSeek(0, SeekOrigin.Begin) : stream.EnterRestorePositionScope();
		}, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("stream"));
	}

	[Test]
	public void LegacySeekPositionScopeRequiresAStream() {
		Stream stream = null;
		Assert.That(() => stream.EnterRestoreSeekPositionScope(), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("stream"));
	}

	[Test]
	public void InterfaceConsumersRetainDirectRestoration() {
		using var stream = new MemoryStream(new byte[16]);
		stream.Position = 4;
		IDisposable restore = stream.EnterRestorePositionSeek(1, SeekOrigin.Begin);
		restore.Dispose();
		Assert.That(stream.Position, Is.EqualTo(4));
		stream.Position = 9;
		restore.Dispose();
		Assert.That(stream.Position, Is.EqualTo(4));
		Assert.That(stream.CanRead, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RestorePositionScopesDoNotAllocate(bool seek) {
		using var stream = new MemoryStream(new byte[16]);
		stream.Position = 4;
		for (var iteration = 0; iteration < 32; iteration++)
			using (seek ? stream.EnterRestorePositionSeek(8, SeekOrigin.Begin) : stream.EnterRestorePositionScope())
				stream.ReadByte();
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var iteration = 0; iteration < 1000; iteration++)
			using (seek ? stream.EnterRestorePositionSeek(8, SeekOrigin.Begin) : stream.EnterRestorePositionScope())
				stream.ReadByte();
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.WriteLine($"1000 restore-position scopes (seek: {seek}): {allocated:N0} allocated bytes.");
		Assert.That(stream.Position, Is.EqualTo(4));
		Assert.That(allocated, Is.Zero, "Concrete scope returns must avoid both heap allocation and interface boxing.");
	}

	private sealed class TrackingStream : MemoryStream {
		private readonly List<string> _operations = new();

		public TrackingStream()
			: base(new byte[16]) {
		}

		public bool AllowSeek { get; set; } = true;

		public bool ThrowAfterSeek { get; set; }

		public bool FailNextPositionSet { get; set; }

		public Action PositionChanging { get; set; }

		public string[] Operations => _operations.ToArray();

		public override bool CanSeek {
			get {
				_operations.Add("can seek");
				return AllowSeek;
			}
		}

		public override long Position {
			get {
				_operations.Add("get position");
				return base.Position;
			}
			set {
				_operations.Add($"set position {value}");
				if (FailNextPositionSet) {
					FailNextPositionSet = false;
					throw new IOException("Restore rejected");
				}
				PositionChanging?.Invoke();
				base.Position = value;
			}
		}

		public void ClearOperations() => _operations.Clear();

		public override long Seek(long offset, SeekOrigin origin) {
			_operations.Add($"seek {offset} {origin}");
			var position = base.Seek(offset, origin);
			if (ThrowAfterSeek)
				throw new IOException("Seek failed after moving");
			return position;
		}
	}
}
