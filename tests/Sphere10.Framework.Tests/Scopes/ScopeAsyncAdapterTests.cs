// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScopeAsyncAdapterTests {
	[Test]
	public async Task UnconstructedResourceScopesDoNotSilentlyDispose([Values(0, 1, 2)] int resource) {
		IScope scope = resource switch {
			0 => default(Hashers.HasherScope),
			1 => default(RestorePositionScope),
			_ => default(PageScope<int>)
		};
		Assert.That(scope.Dispose, Throws.TypeOf<NullReferenceException>());
		await Assert.ThatAsync(async () => await scope.DisposeAsync(), Throws.TypeOf<NullReferenceException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncFinalizationReturnsWhileSynchronousCleanupIsBlocked(bool restorePosition) {
		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		Action finalize = () => {
			entered.Set();
			if (!release.Wait(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("Finalizer was not released.");
		};
		using var stream = new FinalizationStream(finalize);
		IAsyncDisposable scope = restorePosition ? new RestorePositionScope(stream, 7) : new PageScope<int>(finalize);
		var operation = ValueTask.CompletedTask;
		Exception callFailure = null;
		var caller = new Thread(() => {
			try {
				operation = scope.DisposeAsync();
			} catch (Exception error) {
				callFailure = error;
			} finally {
				returned.Set();
			}
		});
		caller.Start();
		try {
			Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromSeconds(5)), Is.True, "DisposeAsync must return before synchronous cleanup completes.");
			Assert.That(callFailure, Is.Null);
			Assert.That(operation.IsCompleted, Is.False);
		} finally {
			release.Set();
			Assert.That(caller.Join(TimeSpan.FromSeconds(5)), Is.True);
			await operation;
		}
		if (restorePosition)
			Assert.That(stream.Position, Is.EqualTo(7));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncFinalizationReturnsCleanupFailureThroughItsOperation(bool restorePosition) {
		Action finalize = () => throw new IOException("Finalization failed.");
		using var stream = new FinalizationStream(finalize);
		IAsyncDisposable scope = restorePosition ? new RestorePositionScope(stream, 7) : new PageScope<int>(finalize);
		var operation = ValueTask.CompletedTask;
		Assert.That(() => { operation = scope.DisposeAsync(); }, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.TypeOf<IOException>().With.Message.EqualTo("Finalization failed."));
	}

	private sealed class FinalizationStream : MemoryStream {
		private readonly Action _finalize;

		public FinalizationStream(Action finalize) : base(new byte[16]) {
			_finalize = finalize;
		}

		public override long Position {
			get => base.Position;
			set {
				_finalize();
				base.Position = value;
			}
		}
	}
}
