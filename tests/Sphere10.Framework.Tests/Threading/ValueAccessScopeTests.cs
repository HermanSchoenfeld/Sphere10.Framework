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
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ValueAccessScopeTests {
	[Test]
	public void OwnedScopeReleasesItsLock() {
		var critical = new HookedCriticalObject();
		var scope = critical.EnterAccessScope();
		Assert.That(critical.IsLocked, Is.True);
		scope.Dispose();
		Assert.That(critical.IsLocked, Is.False);
		Assert.That(critical.Closed, Is.EqualTo(1));
	}

	[Test]
	public void NestedScopesRetainTheOuterAcquisitionAndHookOrder() {
		var critical = new HookedCriticalObject();
		var outer = critical.EnterAccessScope();
		var inner = critical.EnterAccessScope();
		inner.Dispose();
		Assert.That(critical.IsLocked, Is.True);
		outer.Dispose();
		Assert.That(critical.IsLocked, Is.False);
		Assert.That(critical.Events, Is.EqualTo(new[] { "open:True", "open:True", "close:True", "close:False" }));
	}

	[Test]
	public void OpenHookFailureReleasesOnlyTheFailedAcquisition([Values] bool hasOuterScope) {
		var critical = new HookedCriticalObject();
		AccessScope? outer = hasOuterScope ? critical.EnterAccessScope() : null;
		critical.ThrowOnOpen = true;
		Assert.That(() => critical.EnterAccessScope(), Throws.TypeOf<TestScopeException>());
		Assert.That(critical.IsLocked, Is.EqualTo(hasOuterScope));
		Assert.That(critical.Closed, Is.Zero);
		outer?.Dispose();
		Assert.That(critical.IsLocked, Is.False);
	}

	[Test]
	public void ClosedHookRunsAfterTheLockIsReleased() {
		var critical = new HookedCriticalObject { ThrowOnClose = true };
		var scope = critical.EnterAccessScope();
		Assert.That(() => scope.Dispose(), Throws.TypeOf<TestScopeException>());
		Assert.That(critical.IsLocked, Is.False);
		Assert.That(critical.Closed, Is.EqualTo(1));
	}

	[Test]
	public void CallbackScopeRunsItsFinalizerDirectly() {
		var calls = 0;
		var scope = new SyncScope(() => calls++);
		scope.Dispose();
		scope.Dispose();
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test]
	public void WrongThreadDisposalCanBeRetriedOnTheAcquiringThread() {
		var critical = new CriticalObject();
		var scope = critical.EnterAccessScope();
		Exception failure = null;
		var thread = new Thread(() => {
			try {
				scope.Dispose();
			} catch (Exception error) {
				failure = error;
			}
		});
		thread.Start();
		Assert.That(thread.Join(TimeSpan.FromSeconds(10)), Is.True);
		Assert.That(failure, Is.TypeOf<SynchronizationLockException>());
		Assert.That(critical.IsLocked, Is.True);
		scope.Dispose();
		Assert.That(critical.IsLocked, Is.False);
	}

	[Test]
	public void ScopeReleasesItsCapturedLockWhenTheParentChanges() {
		var firstParent = new CriticalObject();
		var secondParent = new CriticalObject();
		var critical = new CriticalObject { ParentCriticalObject = firstParent };
		var scope = critical.EnterAccessScope();
		critical.ParentCriticalObject = secondParent;
		scope.Dispose();
		Assert.That(Monitor.IsEntered(firstParent.Lock), Is.False);
		Assert.That(Monitor.IsEntered(secondParent.Lock), Is.False);
	}

	[Test]
	public void InterfaceScopeRemainsIndependentlyOwned() {
		var critical = new HookedCriticalObject();
		ICriticalObject contract = critical;
		var scope = contract.EnterAccessScope();
		Assert.That(critical.IsLocked, Is.True);
		scope.Dispose();
		Assert.That(critical.Closed, Is.EqualTo(1));
		Assert.That(critical.IsLocked, Is.False);
	}




	[Test]
	public void CustomCleanupAndFailureSurviveEveryStorageForwarder([Values(0, 1, 2, 3, 4)] int layer) {
		using var memory = new MemoryStream();
		var provider = new CustomCriticalObject();
		using var concurrent = new ConcurrentStream(memory, provider);
		using var streams = new ClusteredStreams(concurrent, 64, autoLoad: true);
		using var objects = new ObjectStream<int>(streams);
		using var space = new ScopeObjectSpace(streams);
		var target = SelectLayer(layer, provider, concurrent, streams, objects, space);
		provider.ResetCounters();
		var scope = target.EnterAccessScope();
		Assert.That(provider.Entered, Is.EqualTo(1));
		Assert.That(provider.IsLocked, Is.True);
		provider.FailNextDispose = true;
		Assert.That(() => scope.Dispose(), Throws.TypeOf<TestScopeException>());
		Assert.That(provider.IsLocked, Is.False);
		Assert.That(provider.DisposeAttempts, Is.EqualTo(1));
		Assert.That(provider.Completed, Is.Zero);
		using (target.EnterAccessScope()) { }
		Assert.That(provider.DisposeAttempts, Is.EqualTo(2));
		Assert.That(provider.Completed, Is.EqualTo(1));
		Assert.That(provider.IsLocked, Is.False);
		provider.FailNextEnter = true;
		Assert.That(() => target.EnterAccessScope(), Throws.TypeOf<TestScopeException>());
		Assert.That(provider.IsLocked, Is.False);
	}

	[Test]
	public void CustomCleanupRunsOnTheAcquiringThread() {
		var provider = new CustomCriticalObject();
		ICriticalObject contract = provider;
		var scope = contract.EnterAccessScope();
		var acquiringThread = Environment.CurrentManagedThreadId;
		provider.DisposingCallback = () => Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(acquiringThread));
		scope.Dispose();
		Assert.That(provider.DisposeAttempts, Is.EqualTo(1));
		Assert.That(provider.Completed, Is.EqualTo(1));
		Assert.That(provider.IsLocked, Is.False);
	}

	[Test]
	public void StreamMappedPropertyRetainsCustomProviderAcquisitionAndCleanup() {
		using var memory = new MemoryStream(new byte[8]);
		var provider = new CustomCriticalObject();
		var property = StreamMappedProperty.At<long>(memory, 0, @lock: provider);
		provider.ResetCounters();
		property.Value = 42;
		Assert.That(property.Value, Is.EqualTo(42));
		Assert.That(provider.Entered, Is.EqualTo(2));
		Assert.That(provider.Completed, Is.EqualTo(2));
		Assert.That(provider.IsLocked, Is.False);
		provider.FailNextEnter = true;
		Assert.That(() => property.Value, Throws.TypeOf<TestScopeException>());
		Assert.That(provider.IsLocked, Is.False);
		property.ValueChanged += (_, _) => throw new TestScopeException();
		Assert.That(() => property.Value = 43, Throws.TypeOf<TestScopeException>());
		Assert.That(provider.IsLocked, Is.False);
		property.ClearListeners();
		Assert.That(property.Value, Is.EqualTo(43));
	}

	[Test]
	public void OpenedClusteredStreamKeepsItsCustomProviderAcquisitionUntilClose() {
		using var memory = new MemoryStream();
		var provider = new CustomCriticalObject();
		using var concurrent = new ConcurrentStream(memory, provider);
		using var streams = new ClusteredStreams(concurrent, 64, autoLoad: true);
		var outer = streams.EnterAccessScope();
		var opened = streams.Add();
		outer.Dispose();
		Assert.That(provider.IsLocked, Is.True);
		opened.Dispose();
		Assert.That(provider.IsLocked, Is.False);
		Assert.That(provider.Completed, Is.EqualTo(provider.Entered));
	}

	private static ICriticalObject SelectLayer(int layer, ICriticalObject critical, ConcurrentStream concurrent, ClusteredStreams streams, ObjectStream objects, ObjectSpace space)
		=> layer switch {
			0 => critical,
			1 => concurrent,
			2 => streams,
			3 => objects,
			4 => space,
			_ => throw new ArgumentOutOfRangeException(nameof(layer))
		};

	private sealed class ScopeObjectSpace : ObjectSpace {
		public ScopeObjectSpace(ClusteredStreams streams)
			: base(streams, new ObjectSpaceDefinition(), SerializerFactory.Default, ComparerFactory.Default) {
			// This fixture exercises the access protocol independently of schema loading.
			FlushOnDispose = false;
		}
	}

	private sealed class TestScopeException : Exception {
	}

	private sealed class HookedCriticalObject : CriticalObject {
		public List<string> Events { get; } = new();
		public bool ThrowOnOpen { get; set; }
		public bool ThrowOnClose { get; set; }
		public Action ClosedCallback { get; set; }
		public int Closed { get; private set; }

		protected override void OnAccessScopeOpen() {
			Events.Add($"open:{IsLocked}");
			if (ThrowOnOpen)
				throw new TestScopeException();
		}

		protected override void OnAccessScopeClosed() {
			Closed++;
			Events.Add($"close:{IsLocked}");
			ClosedCallback?.Invoke();
			if (ThrowOnClose)
				throw new TestScopeException();
		}
	}

	private sealed class CustomCriticalObject : CriticalObject {
		public int Entered { get; private set; }
		public int Completed { get; private set; }
		public int DisposeAttempts { get; private set; }
		public bool FailNextEnter { get; set; }
		public bool FailNextDispose { get; set; }
		public Action DisposingCallback { get; set; }

		protected override void OnAccessScopeOpen() {
			if (FailNextEnter) {
				FailNextEnter = false;
				throw new TestScopeException();
			}
			Entered++;
		}

		protected override void OnAccessScopeClosed() {
			DisposeAttempts++;
			if (FailNextDispose) {
				FailNextDispose = false;
				throw new TestScopeException();
			}
			DisposingCallback?.Invoke();
			Completed++;
		}

		public void ResetCounters() {
			Entered = 0;
			Completed = 0;
			DisposeAttempts = 0;
		}

	}
}
