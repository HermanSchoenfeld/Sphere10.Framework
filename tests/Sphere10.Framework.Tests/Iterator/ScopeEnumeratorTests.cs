// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScopeEnumeratorTests {

	[Test]
	public void DisposeWithTransfersTheConcreteScopeAndReleasesItBeforeTheInnerEnumerator() {
		var order = new List<string>();
		var inner = ((IEnumerable<int>)new[] { 42 }).GetEnumerator().OnDispose(() => order.Add("inner"));
		var scope = new SyncScope<List<string>>(order, static events => events.Add("scope"));
		using var enumerator = inner.DisposeWith(scope);

		Assert.That(order, Is.Empty);
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(enumerator.Current, Is.EqualTo(42));
		enumerator.Dispose();
		enumerator.Dispose();
		Assert.That(order, Is.EqualTo(new[] { "scope", "inner", "inner" }));
	}

	[Test]
	public void FailedScopeReleasePreservesTheInnerEnumeratorForRetry() {
		var releases = 0;
		var innerDisposals = 0;
		var expected = new InvalidOperationException("Release failed.");
		var inner = ((IEnumerable<int>)new[] { 42 }).GetEnumerator().OnDispose(() => innerDisposals++);
		var scope = new SyncScope(() => {
			if (++releases == 1)
				throw expected;
		});
		using var enumerator = inner.DisposeWith(scope);

		Assert.That(enumerator.Dispose, Throws.Exception.SameAs(expected));
		Assert.That(innerDisposals, Is.Zero);
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(enumerator.Current, Is.EqualTo(42));
		Assert.That(enumerator.Dispose, Throws.Nothing);
		enumerator.Dispose();
		Assert.That(releases, Is.EqualTo(2));
		Assert.That(innerDisposals, Is.EqualTo(2));
	}

	[Test]
	public void FailedInnerDisposalCanBeRetriedWithoutReleasingTheScopeAgain() {
		var releases = 0;
		var innerDisposals = 0;
		var expected = new InvalidOperationException("Inner disposal failed.");
		var inner = ((IEnumerable<int>)new[] { 42 }).GetEnumerator().OnDispose(() => {
			if (++innerDisposals == 1)
				throw expected;
		});
		var scope = new SyncScope(() => releases++);
		using var enumerator = inner.DisposeWith(scope);

		Assert.That(enumerator.Dispose, Throws.Exception.SameAs(expected));
		Assert.That(releases, Is.EqualTo(1));
		Assert.That(innerDisposals, Is.EqualTo(1));
		Assert.That(enumerator.Dispose, Throws.Nothing);
		Assert.That(releases, Is.EqualTo(1));
		Assert.That(innerDisposals, Is.EqualTo(2));
		enumerator.Dispose();
		Assert.That(releases, Is.EqualTo(1));
		Assert.That(innerDisposals, Is.EqualTo(3), "Repeated disposal must still reach the inner enumerator, as with the original OnDispose callback wrapper.");
	}

	[Test]
	public void ActionOnDisposeStillRunsItsCallbackBeforeTheInnerEnumeratorOnEveryCall() {
		var order = new List<string>();
		var inner = ((IEnumerable<int>)new[] { 42 }).GetEnumerator().OnDispose(() => order.Add("inner"));
		using var enumerator = inner.OnDispose(() => order.Add("callback"));

		enumerator.Dispose();
		enumerator.Dispose();
		enumerator.Dispose();
		Assert.That(order, Is.EqualTo(new[] { "callback", "inner", "callback", "inner", "callback", "inner" }));
	}

	[Test]
	public void DisposeWithRetriesTheSameMutableScopeBeforeDisposingTheInnerEnumerator() {
		var order = new List<string>();
		var expected = new InvalidOperationException("First release attempt failed.");
		var inner = ((IEnumerable<int>)new[] { 42 }).GetEnumerator().OnDispose(() => order.Add("inner"));
		var scope = new MutableRetryScope(order, expected);
		using var enumerator = inner.DisposeWith(scope);

		Assert.That(enumerator.Dispose, Throws.Exception.SameAs(expected));
		Assert.That(order, Is.EqualTo(new[] { "scope:1" }));
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(enumerator.Current, Is.EqualTo(42));
		Assert.That(enumerator.Dispose, Throws.Nothing);
		Assert.That(order, Is.EqualTo(new[] { "scope:1", "scope:2", "inner" }));
		enumerator.Dispose();
		Assert.That(order, Is.EqualTo(new[] { "scope:1", "scope:2", "inner", "inner" }));
	}

	[Test]
	public void DisposeWithRejectsAMissingEnumeratorWithoutTakingOwnershipOfTheScope() {
		IEnumerator<int> inner = null;
		var releases = 0;
		var scope = new SyncScope(() => releases++);

		Assert.That(() => inner.DisposeWith(scope), Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("enumerator"));
		Assert.That(releases, Is.Zero);
		scope.Dispose();
		Assert.That(releases, Is.EqualTo(1));
	}

	private struct MutableRetryScope : IDisposable {
		private readonly List<string> _order;
		private readonly Exception _failure;
		private int _attempts;

		public MutableRetryScope(List<string> order, Exception failure) {
			_order = order;
			_failure = failure;
			_attempts = 0;
		}

		public void Dispose() {
			_order.Add($"scope:{++_attempts}");
			if (_attempts == 1)
				throw _failure;
		}
	}
}
