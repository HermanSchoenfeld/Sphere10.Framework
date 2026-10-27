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
public class ActionDictionaryScopeTests {


	[Test]
	public void NullCallbacksKeepOrdinaryDictionaryAndEnumerationBehavior() {
		var dictionary = new ActionDictionaryDecorator<int, int, Dictionary<int, int>>(new Dictionary<int, int>(), null, null);
		dictionary.Add(1, 7);
		Assert.That(dictionary[1], Is.EqualTo(7));
		var enumerator = dictionary.GetEnumerator();
		Assert.That(enumerator.MoveNext(), Is.True);
		Assert.That(enumerator.Current, Is.EqualTo(new KeyValuePair<int, int>(1, 7)));
		enumerator.Dispose();
		Assert.That(() => enumerator.Dispose(), Throws.Nothing);
	}

	[Test]
	public void FailedEntryDoesNotRunTheOperationOrExitCallback() {
		var source = new CallbackDictionary();
		var dictionary = new ActionDictionaryDecorator<int, int, CallbackDictionary>(source, () => {
			source.Events.Add("enter");
			throw new CallbackException();
		}, () => source.Events.Add("exit"));
		Assert.That(() => dictionary.Count, Throws.TypeOf<CallbackException>());
		Assert.That(source.Events, Is.EqualTo(new[] { "enter" }));
	}

	[Test]
	public void FailedDictionaryOperationStillRunsExitAfterTheOperation() {
		var source = new CallbackDictionary();
		var dictionary = Decorate(source);
		Assert.That(() => dictionary[2], Throws.TypeOf<KeyNotFoundException>());
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "get", "exit" }));
	}

	[Test]
	public void EnumeratorRetainsItsScopeUntilExplicitDisposal([Values(0, 1, 2)] int visitedItems) {
		var source = new CallbackDictionary();
		var dictionary = Decorate(source);
		var enumerator = dictionary.GetEnumerator();
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator" }));
		if (visitedItems > 0) {
			Assert.That(enumerator.MoveNext(), Is.True);
			Assert.That(enumerator.Current, Is.EqualTo(new KeyValuePair<int, int>(1, 7)));
		}
		if (visitedItems > 1)
			Assert.That(enumerator.MoveNext(), Is.False);
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator" }), "Completing traversal does not dispose an IEnumerator.");
		enumerator.Dispose();
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit", "dispose" }));
		enumerator.Dispose();
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit", "dispose", "dispose" }));
	}

	[Test]
	public void FailedEnumeratorConstructionRunsItsExitCallback() {
		var source = new CallbackDictionary { FailEnumeration = true };
		var dictionary = Decorate(source);
		Assert.That(() => dictionary.GetEnumerator(), Throws.TypeOf<CallbackException>());
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit" }));
	}

	[Test]
	public void FailedExitCanBeRetriedBeforeDisposingTheInnerEnumerator() {
		var source = new CallbackDictionary();
		var attempts = 0;
		var dictionary = new ActionDictionaryDecorator<int, int, CallbackDictionary>(source, () => source.Events.Add("enter"), () => {
			source.Events.Add("exit");
			if (++attempts == 1)
				throw new CallbackException();
		});
		var enumerator = dictionary.GetEnumerator();
		Assert.That(() => enumerator.Dispose(), Throws.TypeOf<CallbackException>());
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit" }));
		enumerator.Dispose();
		enumerator.Dispose();
		Assert.That(attempts, Is.EqualTo(2));
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit", "exit", "dispose", "dispose" }));
	}

	[Test]
	public void InnerDisposalFailurePropagatesWithoutRepeatingOwnerFinalization() {
		var expected = new CallbackException();
		var source = new CallbackDictionary { FirstDisposeFailure = expected };
		var dictionary = Decorate(source);
		var enumerator = dictionary.GetEnumerator();
		Assert.That(enumerator.Dispose, Throws.Exception.SameAs(expected));
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit", "dispose" }));
		Assert.That(source.CompletedDisposals, Is.Zero);
		Assert.That(enumerator.Dispose, Throws.Nothing);
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit", "dispose", "dispose" }));
		Assert.That(source.CompletedDisposals, Is.EqualTo(1), "Retry must complete the inner cleanup without repeating the successful owner exit.");
		enumerator.Dispose();
		Assert.That(source.Events, Is.EqualTo(new[] { "enter", "enumerator", "exit", "dispose", "dispose", "dispose" }));
		Assert.That(source.CompletedDisposals, Is.EqualTo(2));
	}

	private static ActionDictionaryDecorator<int, int, CallbackDictionary> Decorate(CallbackDictionary source)
		=> new(source, () => source.Events.Add("enter"), () => source.Events.Add("exit"));



	private sealed class CallbackException : Exception {
	}

	private sealed class CallbackDictionary : DictionaryDecorator<int, int> {
		public CallbackDictionary()
			: base(new Dictionary<int, int> { [1] = 7 }) {
		}

		public List<string> Events { get; } = new();
		public bool FailEnumeration { get; set; }
		public Exception FirstDisposeFailure { get; set; }
		public int CompletedDisposals { get; private set; }

		public override int Count {
			get {
				Events.Add("count");
				return base.Count;
			}
		}

		public override int this[int key] {
			get {
				Events.Add("get");
				return base[key];
			}
			set => base[key] = value;
		}

		public override IEnumerator<KeyValuePair<int, int>> GetEnumerator() {
			Events.Add("enumerator");
			if (FailEnumeration)
				throw new CallbackException();
			return new CallbackEnumerator(base.GetEnumerator(), this);
		}

		private sealed class CallbackEnumerator(IEnumerator<KeyValuePair<int, int>> enumerator, CallbackDictionary owner)
			: EnumeratorDecorator<KeyValuePair<int, int>>(enumerator) {
			private int _attempts;

			public override void Dispose() {
				owner.Events.Add("dispose");
				if (++_attempts == 1 && owner.FirstDisposeFailure is not null)
					throw owner.FirstDisposeFailure;
				base.Dispose();
				owner.CompletedDisposals++;
			}
		}
	}
}
