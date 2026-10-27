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
public class ActionScopeTests {

	[Test]
	public void CleanupRunsAfterNormalAndExceptionalUsingBodies([Values] bool failBody) {
		var counter = new Counter();
		void Operation() {
			using var cleanup = new ActionScope<Counter>(counter, static state => state.Calls++);
			Assert.That(counter.Calls, Is.Zero);
			if (failBody)
				throw new InvalidOperationException("Body failed.");
		}
		if (failBody)
			Assert.That(Operation, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Body failed."));
		else
			Operation();
		Assert.That(counter.Calls, Is.EqualTo(1));
	}

	[Test]
	public void SimpleFinalizerRunsForEveryExplicitDisposal() {
		var calls = 0;
		var cleanup = new ActionScope(() => calls++);
		cleanup.Dispose();
		cleanup.Dispose();
		Assert.That(calls, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncDisposalReturnsWhileItsFinalizerIsBlocked(bool withItem) {
		using var started = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		using var returned = new ManualResetEventSlim();
		var calls = 0;
		var finalizerThread = 0;
		var callingThread = 0;
		var observed = 0;
		var original = 7;
		void Finalize() {
			finalizerThread = Environment.CurrentManagedThreadId;
			started.Set();
			release.Wait();
			calls++;
		}
		var simple = new ActionScope(Finalize);
		var generic = new ActionScope<int>(original, item => {
			observed = item;
			Finalize();
		});
		original = 9;
		var operation = ValueTask.CompletedTask;
		Exception invocationError = null;
		var caller = new Thread(() => {
			callingThread = Environment.CurrentManagedThreadId;
			try {
				operation = withItem ? generic.DisposeAsync() : simple.DisposeAsync();
			} catch (Exception error) {
				invocationError = error;
			} finally {
				returned.Set();
			}
		});
		caller.Start();
		try {
			Assert.That(started.Wait(TimeSpan.FromSeconds(10)), Is.True);
			Assert.That(returned.Wait(TimeSpan.FromSeconds(10)), Is.True, "DisposeAsync must return while the finalizer waits for release.");
			Assert.That(invocationError, Is.Null);
			Assert.That(operation.IsCompleted, Is.False);
			Assert.That(finalizerThread, Is.Not.EqualTo(callingThread));
			Assert.That(calls, Is.Zero);
			Assert.That(generic.Item, Is.EqualTo(7));
			Assert.That(original, Is.EqualTo(9));
			if (withItem)
				Assert.That(observed, Is.EqualTo(7));
		} finally {
			release.Set();
			Assert.That(caller.Join(TimeSpan.FromSeconds(10)), Is.True);
			await operation.AsTask().WaitAsync(TimeSpan.FromSeconds(10));
		}
		Assert.That(calls, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AsyncDisposalReturnsFinalizerFailuresThroughItsOperation(bool withItem) {
		var calls = 0;
		var observed = 0;
		void Finalize() {
			calls++;
			throw new InvalidOperationException("Finalizer failed.");
		}
		var simple = new ActionScope(Finalize);
		var generic = new ActionScope<int>(7, item => {
			observed = item;
			Finalize();
		});
		var operation = ValueTask.CompletedTask;
		Assert.That(() => {
			operation = withItem ? generic.DisposeAsync() : simple.DisposeAsync();
		}, Throws.Nothing);
		await Assert.ThatAsync(async () => await operation, Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Finalizer failed."));
		Assert.That(calls, Is.EqualTo(1));
		if (withItem)
			Assert.That(observed, Is.EqualTo(7));
	}



	[Test]
	public void GenericCleanupKeepsThePrefetchedValue() {
		var original = 7;
		var observed = 0;
		var cleanup = new ActionScope<int>(original, value => observed = value);
		original = 9;
		cleanup.Dispose();
		cleanup.Dispose();
		Assert.That(observed, Is.EqualTo(7));
		Assert.That(original, Is.EqualTo(9));
	}







	[Test]
	public void SimpleFailureDoesNotPreventLaterExplicitFinalization() {
		var calls = 0;
		var cleanup = new ActionScope(() => {
			if (++calls == 1)
				throw new InvalidOperationException("Cleanup failed.");
		});
		Assert.That(() => cleanup.Dispose(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Cleanup failed."));
		cleanup.Dispose();
		cleanup.Dispose();
		Assert.That(calls, Is.EqualTo(3));
	}

	[Test]
	public void GenericFailureDoesNotPreventLaterExplicitFinalization() {
		var counter = new Counter();
		var cleanup = new ActionScope<Counter>(counter, static state => {
			if (++state.Calls == 1)
				throw new InvalidOperationException("Cleanup failed.");
		});
		Assert.That(() => cleanup.Dispose(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Cleanup failed."));
		cleanup.Dispose();
		cleanup.Dispose();
		Assert.That(counter.Calls, Is.EqualTo(3));
	}

	[Test]
	public void SimpleFinalizerCanExplicitlyInvokeItself() {
		var calls = 0;
		var cleanup = default(ActionScope);
		cleanup = new ActionScope(() => {
			if (++calls == 1)
				cleanup.Dispose();
		});
		cleanup.Dispose();
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public void GenericFinalizerCanExplicitlyInvokeItself() {
		var counter = new Counter();
		var cleanup = default(ActionScope<Counter>);
		cleanup = new ActionScope<Counter>(counter, state => {
			if (++state.Calls == 1)
				cleanup.Dispose();
		});
		cleanup.Dispose();
		Assert.That(counter.Calls, Is.EqualTo(2));
	}

	[Test]
	public void CopiesEachInvokeTheStoredFinalizer() {
		var counter = new Counter();
		var original = new ActionScope<Counter>(counter, static state => state.Calls++);
		var copy = original;
		original.Dispose();
		copy.Dispose();
		Assert.That(counter.Calls, Is.EqualTo(2));
	}

	[Test]
	public void InterfaceCallsInvokeTheFinalizerEachTime() {
		var counter = new Counter();
		var original = new ActionScope<Counter>(counter, static state => state.Calls++);
		IDisposable boxed = original;
		boxed.Dispose();
		boxed.Dispose();
		Assert.That(counter.Calls, Is.EqualTo(2));
		original.Dispose();
		Assert.That(counter.Calls, Is.EqualTo(3));
	}

	[Test]
	public void FileCleanupRetainsDeferredDeletion() {
		var path = Tools.FileSystem.GetTempFileName();
		using (Tools.Scope.DeleteFileOnDispose(path)) {
			Assert.That(File.Exists(path), Is.True);
			File.WriteAllText(path, "temporary content");
		}
		Assert.That(File.Exists(path), Is.False);
	}

	[Test]
	public void DirectoryCleanupRetainsRecursiveDeferredDeletion() {
		var directory = Tools.FileSystem.GetTempEmptyDirectory();
		using (Tools.Scope.DeleteDirOnDispose(directory)) {
			var child = Path.Combine(directory, "child");
			Directory.CreateDirectory(child);
			File.WriteAllText(Path.Combine(child, "temporary.txt"), "temporary content");
			Assert.That(Directory.Exists(directory), Is.True);
		}
		Assert.That(Directory.Exists(directory), Is.False);
	}

	private sealed class Counter {
		public int Calls { get; set; }
	}
}
