// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ValuePageScopeTests {

	[Test]
	public void ConstructionRequiresItsSelectedResource([Values(0, 1, 2)] int resource) {
		Assert.That(() => {
			_ = resource switch {
				0 => new PageScope<int>((CachedItem<IMemoryPage<int>>)null, true),
				1 => new PageScope<int>((DynamicStreamPage<int>)null),
				_ => new PageScope<int>((Action)null)
			};
		}, Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo(resource == 0 ? "cachedItem" : resource == 1 ? "streamPage" : "close"));
	}

	[Test]
	public void NestedMemoryScopesRestoreThePreviousPinUntilTheOuterScopeCloses() {
		using var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		list.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
		var page = list.Pages[0];
		var outer = list.EnterOpenPageScope(page);
		try {
			var inner = list.EnterOpenPageScope(page);
			inner.Dispose();
			Assert.That(page.State, Is.EqualTo(PageState.Loaded));
			Assert.That(() => list.EnterOpenPageScope(list.Pages[1]), Throws.TypeOf<InvalidOperationException>());
			Assert.That(page.State, Is.EqualTo(PageState.Loaded));
		} finally {
			outer.Dispose();
		}
		using (list.EnterOpenPageScope(list.Pages[1]))
			Assert.That(page.State, Is.EqualTo(PageState.Unloaded));
	}

	[Test]
	public void InterfaceMemoryScopeOwnsTheSamePin() {
		using var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		list.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
		IPagedList<int> provider = list;
		PageScope<int> scope = provider.EnterOpenPageScope(list.Pages[0]);
		try {
			Assert.That(() => provider.EnterOpenPageScope(list.Pages[1]), Throws.TypeOf<InvalidOperationException>());
		} finally {
			scope.Dispose();
		}
		using (provider.EnterOpenPageScope(list.Pages[1]))
			Assert.That(list.Pages[0].State, Is.EqualTo(PageState.Unloaded));
	}

	[Test]
	public void EarlyEnumeratorDisposalReleasesItsRetainedPage() {
		using var list = new MemoryPagedList<int>(16, 16, sizeof(int));
		list.AddRange(new[] { 1, 2, 3, 4, 5, 6, 7, 8 });
		using var enumerator = list.GetEnumerator();
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(enumerator.Current, Is.EqualTo(1));
		Assert.That(() => list.EnterOpenPageScope(list.Pages[1]), Throws.TypeOf<InvalidOperationException>());
		enumerator.Dispose();
		using (list.EnterOpenPageScope(list.Pages[1]))
			Assert.That(list.Pages[0].State, Is.EqualTo(PageState.Unloaded));
	}

	[Test]
	public void EnumerationReleasesEachAcquisitionOnce([Values] StreamPagedListType type, [Values] bool empty, [Values] bool stopEarly) {
		using var stream = new MemoryStream();
		var list = new CallbackStreamPagedList(type, stream);
		if (!empty)
			list.AddRange(new[] { 7, 8 });
		var opened = list.Opened;
		var closed = list.Closed;
		var enumerator = list.GetEnumerator();
		if (stopEarly)
			Assert.That(enumerator.MoveNext(), Is.EqualTo(!empty));
		else
			while (enumerator.MoveNext()) {
			}
		enumerator.Dispose();
		enumerator.Dispose();
		Assert.That(list.Opened - opened, Is.EqualTo(empty ? 0 : 1));
		Assert.That(list.Closed - closed, Is.EqualTo(list.Opened - opened), "Every acquired page must close once, including completion followed by disposal.");
	}

	[Test]
	public void FailedEnumerationReleasesItsAcquiredPageOnce() {
		using var stream = new MemoryStream();
		var list = new CallbackStreamPagedList(StreamPagedListType.Static, stream);
		list.AddRange(new[] { 7, 8 });
		var opened = list.Opened;
		var closed = list.Closed;
		Assert.That(() => {
			foreach (var item in list)
				list.Update(0, item);
		}, Throws.TypeOf<InvalidOperationException>());
		Assert.That(list.Opened - opened, Is.EqualTo(2));
		Assert.That(list.Closed - closed, Is.EqualTo(2), "The mutation's scope and failed enumeration's scope must each close once.");
	}

	[Test]
	public void MemoryPinRestorePropagatesFailureAndAllowsAnotherAttempt() {
		var cachedItem = new FailingCachedItem();
		cachedItem.CanPurge = false;
		var scope = new PageScope<int>(cachedItem, true);
		cachedItem.FailRestore = true;
		Assert.That(() => scope.Dispose(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Injected pin restore failure"));
		Assert.That(cachedItem.CanPurge, Is.False);
		Assert.That(cachedItem.RestoreAttempts, Is.EqualTo(1));
		scope.Dispose();
		Assert.That(cachedItem.CanPurge, Is.True);
		Assert.That(cachedItem.RestoreAttempts, Is.EqualTo(2));
	}

	[Test]
	public void CustomCleanupPropagatesFailureAndAllowsAnotherAttempt() {
		var attempts = 0;
		var scope = new PageScope<int>(() => {
			attempts++;
			if (attempts == 1)
				throw new InvalidOperationException("Injected custom close failure");
		});
		Assert.That(() => scope.Dispose(), Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Injected custom close failure"));
		Assert.That(attempts, Is.EqualTo(1));
		scope.Dispose();
		Assert.That(attempts, Is.EqualTo(2));
	}

	[Test]
	public void StreamInterfaceDispatchesToTheTypedVirtualScopeOverride([Values] StreamPagedListType type) {
		using var stream = new MemoryStream();
		var list = new CallbackStreamPagedList(type, stream);
		list.AddRange(new[] { 7, 8 });
		var opened = list.Opened;
		var closed = list.Closed;
		IPagedList<int> provider = list;
		var scope = provider.EnterOpenPageScope(list.Pages[0]);
		Assert.That(list.Opened, Is.EqualTo(opened + 1));
		Assert.That(list.Pages[0].State, Is.EqualTo(PageState.Loaded));
		scope.Dispose();
		Assert.That(list.Closed, Is.EqualTo(closed + 1));
		Assert.That(list.Pages[0].State, Is.EqualTo(type == StreamPagedListType.Dynamic ? PageState.Unloaded : PageState.Loaded));
		Span<byte> destination = stackalloc byte[sizeof(int)];
		Assert.That(list.ReadItemBytes(0, 0, destination), Is.EqualTo(sizeof(int)));
		Assert.That(list.Opened, Is.EqualTo(opened + 2));
		Assert.That(list.Closed, Is.EqualTo(closed + 2));
	}




	private sealed class FailingCachedItem : CachedItem<IMemoryPage<int>> {
		public bool FailRestore { get; set; }

		public int RestoreAttempts { get; private set; }

		public Action Restoring { get; set; }

		public override CachedItemTraits Traits {
			get => base.Traits;
			internal set {
				if ((value & CachedItemTraits.CanPurge) != 0) {
					RestoreAttempts++;
					Restoring?.Invoke();
					if (FailRestore) {
						FailRestore = false;
						throw new InvalidOperationException("Injected pin restore failure");
					}
				}
				base.Traits = value;
			}
		}
	}

	private sealed class CallbackStreamPagedList : StreamPagedList<int> {
		public CallbackStreamPagedList(StreamPagedListType type, Stream stream)
			: base(type, PrimitiveSerializer<int>.Instance, stream, 16) {
		}

		public int Opened { get; private set; }

		public int Closed { get; private set; }

		public override PageScope<int> EnterOpenPageScope(IPage<int> page) {
			Opened++;
			var scope = base.EnterOpenPageScope(page);
			return new PageScope<int>(() => {
				scope.Dispose();
				Closed++;
			});
		}
	}
}
