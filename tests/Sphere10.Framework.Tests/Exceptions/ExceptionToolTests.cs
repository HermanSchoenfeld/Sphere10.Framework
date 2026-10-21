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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ExceptionToolTests {
	[Test]
	public void ExistingCaptureRunsTheActionOnceAndClearsTheOutParameterOnSuccess() {
		var calls = 0;
		Exception captured = new InvalidOperationException("Previous error");
		Tools.Exceptions.ExecuteCapturingException(() => calls++, out captured);
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(captured, Is.Null);
	}

	[Test]
	public void TypedCaptureRetainsTheOriginalDerivedExceptionAndEvaluatesItsFilterOnce() {
		var expected = new FileNotFoundException("Owned missing file");
		var filterCalls = 0;
		Tools.Exceptions.ExecuteCapturingException<IOException>(() => throw expected, out var captured, error => {
			Assert.That(error, Is.SameAs(expected));
			filterCalls++;
			return true;
		});
		Assert.That(captured, Is.SameAs(expected));
		Assert.That(filterCalls, Is.EqualTo(1));
	}

	[Test]
	public void ATypeMismatchPropagatesTheOriginalExceptionWithoutCallingTheFilter() {
		var expected = new ArgumentException("Owned unmatched failure");
		var filterCalls = 0;
		Assert.That(() => Tools.Exceptions.ExecuteCapturingException<IOException>(() => throw expected, out _, _ => { filterCalls++; return true; }),
			Throws.Exception.SameAs(expected));
		Assert.That(filterCalls, Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void BroadSynchronousFilterControlsCaptureWithoutReplacingTheException(bool accepts) {
		var expected = new IOException("Owned filtered failure");
		if (accepts) {
			Tools.Exceptions.ExecuteCapturingException(() => throw expected, out var captured, error => ReferenceEquals(error, expected));
			Assert.That(captured, Is.SameAs(expected));
		} else {
			Assert.That(() => Tools.Exceptions.ExecuteCapturingException(() => throw expected, out _, _ => false), Throws.Exception.SameAs(expected));
		}
	}

	[Test]
	public void AThrowingSynchronousFilterPreservesTheOriginalException() {
		var expected = new IOException("Owned original failure");
		Assert.That(() => Tools.Exceptions.ExecuteCapturingException(() => throw expected, out _, _ => throw new InvalidOperationException("Owned filter failure")),
			Throws.Exception.SameAs(expected));
	}

	[Test]
	public void SynchronousCaptureCanExplicitlyExcludeCancellation() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var expected = new OperationCanceledException(cancellation.Token);
		Assert.That(() => Tools.Exceptions.ExecuteCapturingException(() => throw expected, out _, error => error is not OperationCanceledException),
			Throws.Exception.SameAs(expected));
		Tools.Exceptions.ExecuteCapturingException<OperationCanceledException>(() => throw expected, out var captured);
		Assert.That(captured, Is.SameAs(expected), "Typed capture remains available when the caller owns the cancellation policy.");
	}

	[Test]
	public async Task AsyncCaptureWaitsForSuccessfulWorkAndStartsItOnTheCaller() {
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var finishWork = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var callerThread = Environment.CurrentManagedThreadId;
		var startedThread = 0;
		var completed = false;
		var pending = Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			startedThread = Environment.CurrentManagedThreadId;
			await release.Task.ConfigureAwait(false);
			completed = true;
		});
		Assert.That(startedThread, Is.EqualTo(callerThread), "Exception capture must not silently dispatch UI-affine delegates onto a worker.");
		Assert.That(pending.IsCompleted, Is.False);
		release.SetResult();
		Assert.That(await pending, Is.Null);
		Assert.That(completed, Is.True);
	}

	[Test]
	public async Task AsyncCaptureHandlesAnExceptionThrownBeforeTheDelegateReturnsATask() {
		var expected = new IOException("Owned synchronous delegate failure");
		var captured = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => throw expected);
		Assert.That(captured, Is.SameAs(expected));
	}

	[Test]
	public async Task TypedAsyncCaptureAwaitsAndRetainsADelayedDerivedException() {
		var expected = new FileNotFoundException("Owned delayed missing file");
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var finishWork = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var pending = Tools.Exceptions.ExecuteCapturingExceptionAsync<IOException>(async () => {
			await release.Task.ConfigureAwait(false);
			throw expected;
		});
		Assert.That(pending.IsCompleted, Is.False);
		release.SetResult();
		Assert.That(await pending, Is.SameAs(expected));
	}

	[Test]
	public async Task AsyncTypeMismatchPropagatesTheOriginalExceptionWithoutCallingTheFilter() {
		var expected = new ArgumentException("Owned unmatched asynchronous failure");
		var filterCalls = 0;
		var pending = Tools.Exceptions.ExecuteCapturingExceptionAsync<IOException>(() => Task.FromException(expected), _ => { filterCalls++; return true; });
		await Assert.ThatAsync(async () => await pending, Throws.Exception.SameAs(expected));
		Assert.That(pending.IsFaulted, Is.True);
		Assert.That(filterCalls, Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncFilterControlsCaptureWithoutReplacingTheException(bool accepts) {
		var expected = new IOException("Owned filtered asynchronous failure");
		var filterCalls = 0;
		var pending = Tools.Exceptions.ExecuteCapturingExceptionAsync<IOException>(() => Task.FromException(expected), error => {
			Assert.That(error, Is.SameAs(expected));
			filterCalls++;
			return accepts;
		});
		if (accepts)
			Assert.That(await pending, Is.SameAs(expected));
		else
			await Assert.ThatAsync(async () => await pending, Throws.Exception.SameAs(expected));
		Assert.That(filterCalls, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncCancellationRemainsCanceledUnlessTheFilterExplicitlyCapturesIt(bool accepts) {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var pending = Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Task.FromCanceled(cancellation.Token), _ => accepts);
		if (accepts) {
			var captured = (OperationCanceledException)await pending;
			Assert.That(captured.CancellationToken, Is.EqualTo(cancellation.Token));
			Assert.That(pending.Status, Is.EqualTo(TaskStatus.RanToCompletion));
		} else {
			await Assert.ThatAsync(async () => await pending, Throws.InstanceOf<OperationCanceledException>()
				.With.Property(nameof(OperationCanceledException.CancellationToken)).EqualTo(cancellation.Token));
			Assert.That(pending.Status, Is.EqualTo(TaskStatus.Canceled));
		}
	}

	[Test]
	public async Task AThrowingAsyncFilterPreservesTheOriginalException() {
		var expected = new IOException("Owned original asynchronous failure");
		await Assert.ThatAsync(async () => await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Task.FromException(expected), _ => throw new InvalidOperationException("Owned filter failure")),
			Throws.Exception.SameAs(expected));
	}

	[Test]
	public void LegacyHandlerAndIgnoreHelpersContinueSwallowingHandlerFailures() {
		var expected = new IOException("Owned action failure");
		Exception handled = null;
		Assert.That(() => Tools.Exceptions.ExecuteWithExceptionHandler(() => throw expected, error => {
			handled = error;
			throw new InvalidOperationException("Owned handler failure");
		}), Throws.Nothing);
		Assert.That(handled, Is.SameAs(expected));
		Assert.That(() => Tools.Exceptions.ExecuteIgnoringException(() => throw expected), Throws.Nothing);
	}

	[Test]
	public void LegacyExecuteReturnsSuccessAndStillPropagatesItsHandlerFailure() {
		Assert.That(Tools.Exceptions.Execute(() => { }), Is.True);
		Assert.That(Tools.Exceptions.Execute(() => throw new IOException("Owned action failure")), Is.False);
		var expected = new InvalidOperationException("Owned unsuppressed handler failure");
		Assert.That(() => Tools.Exceptions.Execute(() => throw new IOException("Owned action failure"), _ => throw expected), Throws.Exception.SameAs(expected));
	}

	[Test]
	public void ExistingCollectionCaptureAppendsOnlyAnActualFailure() {
		var previous = new IOException("Previous failure");
		var expected = new ArgumentException("Owned appended failure");
		var errors = new List<Exception> { previous };
		Tools.Exceptions.ExecuteCapturingException(() => { }, errors);
		Tools.Exceptions.ExecuteCapturingException(() => throw expected, errors);
		Assert.That(errors, Is.EqualTo(new Exception[] { previous, expected }));
	}
}
